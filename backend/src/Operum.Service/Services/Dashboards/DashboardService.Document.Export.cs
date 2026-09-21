using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.Models;

namespace Operum.Service.Services.Dashboards
{
    // Reading a board back out as the document that would rebuild it: keys and names in place
    // of ids, so an exported board imports unchanged.
    public partial class DashboardService
    {
        private sealed record DocumentExportContext(
            Dashboard Dashboard,
            DocumentLookups Lookups,
            IReadOnlyDictionary<string, Query> QueriesById,
            IReadOnlyDictionary<string, string> PresetNameById,
            IReadOnlyDictionary<string, string> KeyByItemId,
            IReadOnlyDictionary<string, string> ClauseKeyBySlotId);

        private async Task<DashboardDocumentDto> BuildDocument(Dashboard dashboard)
        {
            var lookups = await LoadDocumentLookups();

            var views = await db.DashboardViews
                .Where(v => v.DashboardId == dashboard.Id)
                .OrderBy(v => v.Order)
                .Include(v => v.DashboardViewQueries.OrderBy(q => q.Order)).ThenInclude(q => q.Query)
                .ToListAsync();

            var filterConfigs = dashboard.Items
                .Where(i => i.Type == DashboardWidgetTypes.Filter)
                .OrderBy(i => i.Order)
                .Select(i => (Item: i, Config: TryParseFilterConfig(i.Config)))
                .Where(x => x.Config != null)
                .ToList();

            var slotQueryIds = filterConfigs
                .SelectMany(x => x.Config!.Slots)
                .Select(s => s.QueryId)
                .Distinct()
                .ToList();

            var queries = await db.Queries.Where(q => slotQueryIds.Contains(q.Id)).ToDictionaryAsync(q => q.Id);

            var clauseKeys = new Dictionary<string, string>();
            foreach (var (item, config) in filterConfigs)
                for (var index = 0; index < config!.Slots.Count; index++)
                    clauseKeys[config.Slots[index].SlotId] = ClauseKey(item.Key!, index);

            var context = new DocumentExportContext(
                dashboard,
                lookups,
                queries,
                views.ToDictionary(v => v.Id, v => v.Name),
                dashboard.Items.ToDictionary(i => i.Id, i => i.Key!),
                clauseKeys);

            return new DashboardDocumentDto
            {
                SchemaVersion = DashboardDocumentDto.CurrentSchemaVersion,
                Board = new DashboardDocumentBoardDto
                {
                    Name = dashboard.Name,
                    Color = new Optional<string>(dashboard.Color),
                    Icon = new Optional<string>(dashboard.Icon),
                    Presets = views.Select(v => new DashboardDocumentPresetDto
                    {
                        Name = v.Name,
                        Clauses = v.DashboardViewQueries
                            .OrderBy(q => q.Order)
                            .Select(q => new DashboardDocumentClauseDto
                            {
                                Kind = q.Query.Kind,
                                DataType = q.Query.DataType,
                                Operator = q.Query.Operator,
                                Value = q.Query.Value,
                                Descending = q.Query.Descending
                            })
                            .ToList()
                    }).ToList()
                },
                Items = dashboard.Items.OrderBy(i => i.Order).Select(i => BuildDocumentItem(i, context)).ToList()
            };
        }

        private DashboardDocumentItemDto BuildDocumentItem(DashboardItem item, DocumentExportContext context)
        {
            var parentKey = item.ParentItemId == null ? null : context.KeyByItemId.GetValueOrDefault(item.ParentItemId);
            var parent = item.ParentItemId == null ? null : context.Dashboard.Items.FirstOrDefault(i => i.Id == item.ParentItemId);
            var tabName = item.ParentTabId == null || parent == null
                ? null
                : TryParseTabsContainerConfig(parent.Config)?.Tabs.FirstOrDefault(t => t.Id == item.ParentTabId)?.Name;

            var documentItem = new DashboardDocumentItemDto
            {
                Key = item.Key!,
                Type = item.Type,
                Name = ResolveItemName(item),
                Parent = new Optional<string>(parentKey),
                Tab = new Optional<string>(tabName),
                Layout = new DashboardDocumentLayoutDto
                {
                    X = item.X,
                    Y = item.Y,
                    W = item.W,
                    H = item.H,
                    DisplayMode = DashboardDocumentDisplayModes.From(item.DisplayMode)
                },
                MobileLayout = new DashboardDocumentLayoutDto
                {
                    X = item.MobileX,
                    Y = item.MobileY,
                    W = item.MobileW,
                    H = item.MobileH,
                    DisplayMode = DashboardDocumentDisplayModes.From(item.MobileDisplayMode)
                },
                Color = new Optional<string>(item.Color),
                ShowTrend = item.ShowTrend,
                YAxisFromZero = item.YAxisFromZero,
                Wiring = BuildWiring(item, context)
            };

            if (item.Type is DashboardWidgetTypes.Header or DashboardWidgetTypes.Note or DashboardWidgetTypes.Container)
                documentItem.Text = new Optional<string>(TryParseTextConfig(item.Config)?.Text ?? string.Empty);

            if (item.Type == DashboardWidgetTypes.TabsContainer)
            {
                var config = TryParseTabsContainerConfig(item.Config);
                documentItem.Text = new Optional<string>(config?.Title ?? string.Empty);
                documentItem.Tabs = new Optional<List<string>>((config?.Tabs ?? []).Select(t => t.Name).ToList());
            }

            if (item.Type == DashboardWidgetTypes.Entries)
            {
                var trackerId = item.EntriesWidget?.TrackerId;
                documentItem.Columns = new Optional<List<string>>(
                    (TryParseEntriesConfig(item.Config)?.ColumnFieldIds ?? [])
                        .Select(id => trackerId == null ? null : context.Lookups.FieldName(trackerId, id))
                        .OfType<string>()
                        .ToList());
            }

            return documentItem;
        }

        private DashboardDocumentWiringDto? BuildWiring(DashboardItem item, DocumentExportContext context)
        {
            var wiring = new DashboardDocumentWiringDto();

            switch (item.Type)
            {
                case DashboardWidgetTypes.Analytic:
                    wiring.Widget = WidgetEcho(item);

                    var sources = item.Sources
                        .OrderBy(s => s.Order)
                        .Select(s =>
                        {
                            var trackerId = s.WidgetSource?.TrackerId;
                            var viewName = s.ViewId == null || trackerId == null ? null : context.Lookups.ViewName(trackerId, s.ViewId);

                            return new DashboardDocumentSourceDto
                            {
                                TrackerName = s.WidgetSource?.Tracker?.Name,
                                Label = new Optional<string>(s.Label),
                                // A view the user can no longer see is left out, so an unchanged import doesn't clear it.
                                View = s.ViewId == null || viewName != null ? new Optional<string>(viewName) : default,
                                Fields = SourceFieldEntries(s.WidgetSource, context.Lookups)
                            };
                        })
                        .ToList();

                    wiring.Sources = sources.Count > 0 ? sources : null;

                    var goalTargets = ParseGoalConditionalTargets(item.GoalConditionalTargets)
                        .Select(row => new GoalConditionalTargetDto
                        {
                            Target = row.Target,
                            Conditions = row.Conditions
                                .Where(kv => context.ClauseKeyBySlotId.ContainsKey(kv.Key))
                                .ToDictionary(kv => context.ClauseKeyBySlotId[kv.Key], kv => kv.Value)
                        })
                        .Where(row => row.Conditions.Count > 0)
                        .ToList();

                    wiring.GoalConditionalTargets = goalTargets.Count > 0 ? goalTargets : null;
                    break;

                case DashboardWidgetTypes.QuickAdd:
                    var quickAddTracker = TryParseQuickAddConfig(item.Config)?.TrackerId;
                    wiring.TrackerName = quickAddTracker == null ? null : context.Lookups.TrackerName(quickAddTracker);
                    break;

                case DashboardWidgetTypes.Entries:
                    wiring.TrackerName = item.EntriesWidget?.Tracker?.Name;
                    break;

                case DashboardWidgetTypes.Filter:
                    wiring.Filter = BuildFilterEcho(item, context);
                    break;
            }

            return wiring.Widget == null && wiring.Sources == null && wiring.GoalConditionalTargets == null
                && wiring.TrackerName == null && wiring.Filter == null
                    ? null
                    : wiring;
        }

        private DashboardDocumentFilterDto? BuildFilterEcho(DashboardItem item, DocumentExportContext context)
        {
            var config = TryParseFilterConfig(item.Config);
            if (config == null)
                return null;

            var links = config.Links
                .Where(l => LinkResolves(context.Dashboard, l) && context.KeyByItemId.ContainsKey(l.ItemId))
                .Select(l =>
                {
                    var follower = context.Dashboard.Items.First(i => i.Id == l.ItemId);

                    return new DashboardDocumentFilterLinkDto
                    {
                        Item = context.KeyByItemId[l.ItemId],
                        TrackerName = ResolveItemTrackerIds(follower).Count > 1 ? context.Lookups.TrackerName(l.TrackerId) : null,
                        Fields = l.FieldByQuery
                            .Where(kv => context.ClauseKeyBySlotId.ContainsKey(kv.Key) && context.Lookups.FieldName(l.TrackerId, kv.Value) != null)
                            .ToDictionary(kv => context.ClauseKeyBySlotId[kv.Key], kv => context.Lookups.FieldName(l.TrackerId, kv.Value)!)
                    };
                })
                .ToList();

            return new DashboardDocumentFilterDto
            {
                Clauses = config.Slots
                    .Where(s => context.QueriesById.ContainsKey(s.QueryId))
                    .Select(s => new DashboardDocumentClauseDto
                    {
                        Key = context.ClauseKeyBySlotId[s.SlotId],
                        DataType = context.QueriesById[s.QueryId].DataType,
                        Operator = context.QueriesById[s.QueryId].Operator,
                        Value = config.ValueBySlot.GetValueOrDefault(s.SlotId)
                    })
                    .ToList(),
                Presets = config.PresetIds
                    .Where(context.PresetNameById.ContainsKey)
                    .Select(id => context.PresetNameById[id])
                    .ToList(),
                Links = links
            };
        }

        private static DashboardDocumentWidgetDto? WidgetEcho(DashboardItem item)
        {
            if (item.Widget == null)
                return null;

            return new DashboardDocumentWidgetDto
            {
                ResultType = item.Widget.ResultType,
                Code = item.Widget.Code,
                Grouping = string.IsNullOrEmpty(item.Widget.Grouping) ? null : item.Widget.Grouping,
                MatchedValuesOnly = item.Widget.MatchedValuesOnly,
                GoalTarget = item.Widget.GoalTarget,
                GoalDirection = item.Widget.GoalDirection
            };
        }

        private static List<string> SourceFieldEntries(WidgetSource? source, DocumentLookups lookups) =>
            (source?.Fields ?? [])
                .Where(f => f.Field != null)
                .Select(f => $"{f.Purpose}: {f.Field.Name}")
                .ToList();

        private static TextWidgetConfigDto? TryParseTextConfig(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                return JsonSerializer.Deserialize<TextWidgetConfigDto>(config, ConfigJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Property order and map key order carry no meaning, so a reformatted or reordered
        // read-only block still compares equal; array order does, and is left alone.
        private static string Canonical(object? value)
        {
            var node = JsonSerializer.SerializeToNode(value, ConfigJsonOptions);
            return SortNode(node)?.ToJsonString() ?? "null";
        }

        private static JsonNode? SortNode(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    var sorted = new JsonObject();
                    foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                        sorted[property.Key] = SortNode(property.Value?.DeepClone());
                    return sorted;
                case JsonArray array:
                    var copy = new JsonArray();
                    foreach (var element in array)
                        copy.Add(SortNode(element?.DeepClone()));
                    return copy;
                default:
                    return node?.DeepClone();
            }
        }
    }
}
