using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Model.DTOs.Queries;
using Operum.Model.DTOs.Widgets.Requests;
using Operum.Model.Models;
using Operum.Service.Domain.Queries;

namespace Operum.Service.Services.Dashboards
{
    public partial class DashboardService
    {
        // ----- Names to ids -----

        private sealed record TrackerRef(string Id, string Name);

        private sealed record ChildRef(string Id, string Name);

        // The trackers the user can read, with their fields and views, loaded once so a
        // document that names them resolves without a query per reference. Names are not unique
        // in the data, so one that matches more than once is an error asking for a rename.
        private sealed class DocumentLookups
        {
            public required List<TrackerRef> Trackers { get; init; }
            public required Dictionary<string, List<ChildRef>> FieldsByTracker { get; init; }
            public required Dictionary<string, List<ChildRef>> ViewsByTracker { get; init; }

            public string? TrackerName(string id) => Trackers.FirstOrDefault(t => t.Id == id)?.Name;

            public string? FieldName(string trackerId, string fieldId) =>
                FieldsByTracker.GetValueOrDefault(trackerId)?.FirstOrDefault(f => f.Id == fieldId)?.Name;

            public string? ViewName(string trackerId, string viewId) =>
                ViewsByTracker.GetValueOrDefault(trackerId)?.FirstOrDefault(v => v.Id == viewId)?.Name;

            public string? ResolveTracker(string path, string? name, List<string> errors)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"{path}: trackerName is required.");
                    return null;
                }

                var matches = Trackers.Where(t => string.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 1)
                    return matches[0].Id;

                errors.Add(matches.Count == 0
                    ? $"{path}.trackerName: no tracker named \"{name}\"."
                    : $"{path}.trackerName: {matches.Count} trackers are named \"{name}\". Rename one so they can be told apart.");
                return null;
            }

            // Where two fields share a name, the one already in use wins over a rename request.
            public string? ResolveField(string path, string trackerId, string name, List<string> errors, IEnumerable<string>? preferredIds = null)
            {
                var trimmed = name.Trim();
                var matches = (FieldsByTracker.GetValueOrDefault(trackerId) ?? [])
                    .Where(f => string.Equals(f.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count == 1)
                    return matches[0].Id;

                var preferred = matches.FirstOrDefault(f => preferredIds?.Contains(f.Id) == true);
                if (matches.Count > 1 && preferred != null)
                    return preferred.Id;

                errors.Add(matches.Count == 0
                    ? $"{path}: no field \"{name}\" on {TrackerName(trackerId) ?? "that tracker"}."
                    : $"{path}: {matches.Count} fields on {TrackerName(trackerId)} are named \"{name}\". Rename one so they can be told apart.");
                return null;
            }

            public string? ResolveView(string path, string trackerId, string name, List<string> errors)
            {
                var matches = (ViewsByTracker.GetValueOrDefault(trackerId) ?? [])
                    .Where(v => string.Equals(v.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count == 1)
                    return matches[0].Id;

                errors.Add(matches.Count == 0
                    ? $"{path}: no view named \"{name}\" on {TrackerName(trackerId) ?? "that tracker"}."
                    : $"{path}: {matches.Count} views on {TrackerName(trackerId)} are named \"{name}\". Rename one so they can be told apart.");
                return null;
            }
        }

        private async Task<DocumentLookups> LoadDocumentLookups()
        {
            var user = currentUserService.GetCurrentUser();

            var trackers = await db.Trackers
                .Where(t => t.OwnerId == user.Id || t.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id))
                .Select(t => new TrackerRef(t.Id, t.Name))
                .ToListAsync();

            var trackerIds = trackers.Select(t => t.Id).ToList();

            var fields = await db.Fields
                .Where(f => trackerIds.Contains(f.TrackerId))
                .Select(f => new { f.TrackerId, f.Id, f.Name })
                .ToListAsync();

            var views = await db.Views
                .Where(v => trackerIds.Contains(v.TrackerId))
                .Select(v => new { v.TrackerId, v.Id, v.Name })
                .ToListAsync();

            return new DocumentLookups
            {
                Trackers = trackers,
                FieldsByTracker = fields.GroupBy(f => f.TrackerId).ToDictionary(g => g.Key, g => g.Select(f => new ChildRef(f.Id, f.Name)).ToList()),
                ViewsByTracker = views.GroupBy(v => v.TrackerId).ToDictionary(g => g.Key, g => g.Select(v => new ChildRef(v.Id, v.Name)).ToList())
            };
        }

        // "Purpose: Field name". The purpose has no colon, so the first one splits the two.
        private static bool TrySplitSourceField(string entry, out string purpose, out string reference)
        {
            var colon = entry.IndexOf(':');
            purpose = colon > 0 ? entry[..colon].Trim() : string.Empty;
            reference = colon > 0 ? entry[(colon + 1)..].Trim() : string.Empty;
            return purpose.Length > 0 && reference.Length > 0;
        }

        private static string Options(IEnumerable<string> values) => string.Join(", ", values.Order());

        // ----- A new item: what it is wired to -----

        private async Task ResolveNewItem(DocumentPlan plan, ItemPlan itemPlan)
        {
            var wiring = itemPlan.Doc.Wiring;
            var path = $"{itemPlan.Path}.wiring";

            switch (itemPlan.Item.Type)
            {
                case DashboardWidgetTypes.Analytic:
                    await ResolveNewAnalytic(plan, itemPlan);
                    break;

                case DashboardWidgetTypes.QuickAdd:
                    itemPlan.TrackerId = plan.Lookups.ResolveTracker(path, wiring?.TrackerName, plan.Errors);
                    break;

                case DashboardWidgetTypes.Entries:
                    await ResolveNewEntries(plan, itemPlan);
                    break;

                case DashboardWidgetTypes.Filter:
                    if (wiring?.Filter == null || wiring.Filter.Clauses.Count == 0)
                        plan.Errors.Add($"{path}.filter: a filter widget needs at least one clause.");
                    break;
            }
        }

        private async Task ResolveNewEntries(DocumentPlan plan, ItemPlan itemPlan)
        {
            var wiring = itemPlan.Doc.Wiring;
            var path = $"{itemPlan.Path}.wiring";

            if (string.IsNullOrWhiteSpace(wiring?.Library))
            {
                itemPlan.TrackerId = plan.Lookups.ResolveTracker(path, wiring?.TrackerName, plan.Errors);
                return;
            }

            var user = currentUserService.GetCurrentUser();
            var name = wiring.Library.Trim();
            var lowered = name.ToLower();
            var matches = await db.EntriesWidgets
                .Where(w => w.OwnerId == user.Id && w.Name.ToLower() == lowered)
                .ToListAsync();

            if (matches.Count != 1)
            {
                plan.Errors.Add(matches.Count == 0
                    ? $"{path}.library: no entries widget named \"{name}\" in your Widget Library."
                    : $"{path}.library: {matches.Count} entries widgets are named \"{name}\". Rename one in the Widget Library.");
                return;
            }

            var library = matches[0];
            if (wiring.TrackerName != null
                && plan.Lookups.ResolveTracker(path, wiring.TrackerName, plan.Errors) is { } named
                && named != library.TrackerId)
                plan.Errors.Add($"{path}: that entries widget reads a different tracker.");

            itemPlan.LibraryEntries = library;
            itemPlan.TrackerId = library.TrackerId;
        }

        private async Task ResolveNewAnalytic(DocumentPlan plan, ItemPlan itemPlan)
        {
            var wiring = itemPlan.Doc.Wiring;
            var path = $"{itemPlan.Path}.wiring";

            if (string.IsNullOrWhiteSpace(wiring?.Library) && wiring?.Widget == null)
            {
                plan.Errors.Add($"{path}: an analytic widget needs library (a Widget Library widget, by name) or widget (a new definition).");
                return;
            }

            if (!string.IsNullOrWhiteSpace(wiring.Library) && wiring.Widget != null)
            {
                plan.Errors.Add($"{path}: give library or widget, not both.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(wiring.Library))
                await ResolveLibraryReference(plan, itemPlan, wiring);
            else
                ResolveInlineDefinition(plan, itemPlan, wiring);
        }

        private async Task ResolveLibraryReference(DocumentPlan plan, ItemPlan itemPlan, DashboardDocumentWiringDto wiring)
        {
            var path = $"{itemPlan.Path}.wiring";
            var user = currentUserService.GetCurrentUser();
            var name = wiring.Library!.Trim();
            var lowered = name.ToLower();

            var matches = await db.Widgets
                .Include(w => w.Sources).ThenInclude(s => s.Tracker)
                .Include(w => w.Sources).ThenInclude(s => s.Fields).ThenInclude(f => f.Field)
                .Where(w => w.OwnerId == user.Id && w.Name.ToLower() == lowered)
                .ToListAsync();

            if (matches.Count != 1)
            {
                plan.Errors.Add(matches.Count == 0
                    ? $"{path}.library: no widget named \"{name}\" in your Widget Library."
                    : $"{path}.library: {matches.Count} Library widgets are named \"{name}\". Rename one in the Widget Library.");
                return;
            }

            var widget = matches[0];
            itemPlan.LibraryWidget = widget;
            var ordered = widget.Sources.OrderBy(s => s.Order).ToList();

            if (wiring.Sources != null && wiring.Sources.Count != ordered.Count)
            {
                plan.Errors.Add($"{path}.sources: this widget has {ordered.Count} sources, and they cannot be changed here.");
                return;
            }

            for (var index = 0; index < ordered.Count; index++)
            {
                var given = wiring.Sources?[index];
                var sourcePath = $"{path}.sources[{index}]";

                if (given == null)
                {
                    itemPlan.SourceInputs.Add((null, null));
                    continue;
                }

                if (given.TrackerName != null && !string.Equals(given.TrackerName, ordered[index].Tracker?.Name, StringComparison.OrdinalIgnoreCase))
                    plan.Errors.Add($"{sourcePath}.trackerName does not match the widget's source.");
                if (given.Fields != null && !given.Fields.SequenceEqual(SourceFieldEntries(ordered[index], plan.Lookups)))
                    plan.Errors.Add($"{sourcePath}.fields does not match the widget's source.");

                var placement = ResolveSourcePlacement(plan, sourcePath, 0, given, ordered[index].TrackerId, null);
                itemPlan.SourceInputs.Add((placement?.Label, placement?.ViewId));
            }
        }

        private void ResolveInlineDefinition(DocumentPlan plan, ItemPlan itemPlan, DashboardDocumentWiringDto wiring)
        {
            var path = $"{itemPlan.Path}.wiring";
            var errors = plan.Errors;
            var widget = wiring.Widget!;
            var widgetPath = $"{path}.widget";

            if (!AnalyticTypes.IsValid(widget.ResultType))
                errors.Add($"{widgetPath}.resultType: must be one of {Options(AnalyticTypes.All)}.");
            else if (!AnalyticDefinitionList.IsValidForType(widget.ResultType, widget.Code, widget.Grouping))
                errors.Add(DescribeInvalidCalculation(widgetPath, widget));

            if (!string.IsNullOrEmpty(widget.GoalDirection) && !GoalDirections.IsValid(widget.GoalDirection))
                errors.Add($"{widgetPath}.goalDirection: must be one of {Options(GoalDirections.All)}.");

            if (wiring.Sources is not { Count: > 0 })
            {
                errors.Add($"{path}.sources: a new widget needs at least one source.");
                return;
            }

            if (wiring.Sources.Count > DataLimits.MaxDashboardItemSourceCount)
            {
                errors.Add($"{path}.sources: {Messages.MaxNumberReached("widget sources", DataLimits.MaxDashboardItemSourceCount)}");
                return;
            }

            var sources = new List<CreateWidgetSourceRequestDto>();
            for (var index = 0; index < wiring.Sources.Count; index++)
            {
                var given = wiring.Sources[index];
                var sourcePath = $"{path}.sources[{index}]";

                var trackerId = plan.Lookups.ResolveTracker(sourcePath, given.TrackerName, errors);
                if (trackerId == null)
                    continue;

                var fields = new List<CreateAnalyticFieldDto>();
                if (given.Fields is not { Count: > 0 })
                    errors.Add($"{sourcePath}.fields: needs entries like \"Value: Amount\". Purposes: {Options(AnalyticPurposes.All)}.");

                foreach (var entry in given.Fields ?? [])
                {
                    if (!TrySplitSourceField(entry, out var purpose, out var reference))
                    {
                        errors.Add($"{sourcePath}.fields: \"{entry}\" should read \"Purpose: Field name\".");
                        continue;
                    }

                    if (!AnalyticPurposes.IsValid(purpose))
                    {
                        errors.Add($"{sourcePath}.fields: purpose \"{purpose}\" must be one of {Options(AnalyticPurposes.All)}.");
                        continue;
                    }

                    var fieldId = plan.Lookups.ResolveField($"{sourcePath}.fields", trackerId, reference, errors);
                    if (fieldId != null)
                        fields.Add(new CreateAnalyticFieldDto { FieldId = fieldId, Purpose = purpose });
                }

                var placement = ResolveSourcePlacement(plan, sourcePath, index, given, trackerId, null);
                itemPlan.SourceInputs.Add((placement?.Label, placement?.ViewId));
                sources.Add(new CreateWidgetSourceRequestDto { TrackerId = trackerId, Fields = fields });
            }

            itemPlan.Definition = new CreateWidgetDto
            {
                Name = itemPlan.Doc.Name,
                ResultType = widget.ResultType,
                Code = widget.Code,
                Grouping = string.IsNullOrEmpty(widget.Grouping) ? null : widget.Grouping,
                MatchedValuesOnly = widget.MatchedValuesOnly,
                GoalTarget = widget.GoalTarget,
                GoalDirection = widget.GoalDirection,
                Sources = sources
            };
        }

        // The calculations a result type takes, so a wrong pair says what would have worked.
        private static string DescribeInvalidCalculation(string path, DashboardDocumentWidgetDto widget)
        {
            var type = widget.ResultType;
            var definition = AnalyticDefinitionList.ByResultType[type];

            if (definition.UsesGrouping)
            {
                if (string.IsNullOrEmpty(widget.Grouping) || !definition.Groupings.TryGetValue(widget.Grouping, out var grouping))
                    return $"{path}.grouping: a {type} needs one of {Options(definition.Groupings.Keys)}.";

                return $"{path}.code: grouped by {widget.Grouping}, a {type} takes one of {Options(grouping.AllowedCodes)}.";
            }

            return string.IsNullOrEmpty(widget.Grouping)
                ? $"{path}.code: a {type} takes one of {Options(definition.Codes.Keys)}."
                : $"{path}.grouping: a {type} has no grouping.";
        }

        // The label and fixed view a placement puts on one source. Null when the document says
        // nothing new about either, so an existing source is left as it is. A view named the
        // same as the one already set is not looked up again, which keeps a board importable
        // even where two views of a tracker share a name.
        private PlannedSourceWrite? ResolveSourcePlacement(
            DocumentPlan plan,
            string path,
            int index,
            DashboardDocumentSourceDto source,
            string trackerId,
            string? currentViewId)
        {
            var errors = plan.Errors;

            var setLabel = source.Label.IsSet;
            var label = setLabel ? Blank(source.Label.Value) : null;
            if (label?.Length > 100)
            {
                errors.Add($"{path}.label: cannot exceed 100 characters.");
                label = null;
            }

            var setView = source.View.IsSet;
            string? viewId = null;
            var viewName = setView ? Blank(source.View.Value) : null;

            if (viewName != null)
            {
                var currentName = currentViewId == null ? null : plan.Lookups.ViewName(trackerId, currentViewId);
                if (string.Equals(currentName, viewName, StringComparison.OrdinalIgnoreCase))
                {
                    setView = false;
                }
                else
                {
                    viewId = plan.Lookups.ResolveView($"{path}.view", trackerId, viewName, errors);
                    setView = viewId != null;
                }
            }

            return setLabel || setView
                ? new PlannedSourceWrite(index, setLabel, label, setView, viewId)
                : null;
        }

        // ----- An item already on the board -----

        // The Library definition and each source's tracker and fields are fixed once a widget
        // exists, so what the document says about them has to be what is already there.
        private void ValidateExistingWiring(DocumentPlan plan, ItemPlan itemPlan)
        {
            var wiring = itemPlan.Doc.Wiring!;
            var item = itemPlan.Item;
            var path = $"{itemPlan.Path}.wiring";
            var errors = plan.Errors;
            var lookups = plan.Lookups;

            var libraryName = item.Type == DashboardWidgetTypes.Entries ? item.EntriesWidget?.Name : item.Widget?.Name;
            if (wiring.Library != null && !string.Equals(wiring.Library, libraryName, StringComparison.OrdinalIgnoreCase))
                errors.Add($"{path}.library is read-only.");

            if (wiring.Widget != null && Canonical(wiring.Widget) != Canonical(WidgetEcho(item)))
                errors.Add($"{path}.widget is read-only. Edit the widget in the Widget Library.");

            if (item.Type is DashboardWidgetTypes.QuickAdd or DashboardWidgetTypes.Entries)
            {
                var trackerId = item.Type == DashboardWidgetTypes.Entries
                    ? item.EntriesWidget?.TrackerId
                    : TryParseQuickAddConfig(item.Config)?.TrackerId;

                if (wiring.TrackerName != null
                    && !string.Equals(wiring.TrackerName, trackerId == null ? null : lookups.TrackerName(trackerId), StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{path}.trackerName is read-only.");
            }

            if (wiring.Sources == null)
                return;

            var ordered = item.Sources.OrderBy(s => s.Order).ToList();
            if (wiring.Sources.Count != ordered.Count)
            {
                errors.Add($"{path}.sources: this widget has {ordered.Count} sources, and they cannot be added or removed here.");
                return;
            }

            for (var index = 0; index < ordered.Count; index++)
            {
                var given = wiring.Sources[index];
                var existing = ordered[index];
                var sourcePath = $"{path}.sources[{index}]";
                var trackerId = existing.WidgetSource!.TrackerId;

                if (given.TrackerName != null
                    && !string.Equals(given.TrackerName, existing.WidgetSource.Tracker?.Name, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{sourcePath}.trackerName is read-only.");

                if (given.Fields != null && !given.Fields.SequenceEqual(SourceFieldEntries(existing.WidgetSource, lookups)))
                    errors.Add($"{sourcePath}.fields is read-only. Edit the widget in the Widget Library.");

                var placement = ResolveSourcePlacement(plan, sourcePath, index, given, trackerId, existing.ViewId);
                if (placement != null)
                    itemPlan.SourceWrites.Add(placement);
            }
        }

        // ----- Library definitions and new items -----

        private async Task CreateLibraryDefinition(DocumentPlan plan, ItemPlan itemPlan)
        {
            if (itemPlan.Definition != null)
            {
                var created = await widgetsService.CreateWidget(itemPlan.Definition);
                if (!created.IsSuccess)
                {
                    plan.Errors.Add($"{itemPlan.Path}.wiring.widget: {string.Join(" ", created.Messages)}");
                    return;
                }

                itemPlan.LibraryWidget = await db.Widgets
                    .Include(w => w.Sources)
                    .FirstAsync(w => w.Id == created.Data.Id);
                return;
            }

            if (itemPlan.Item.Type == DashboardWidgetTypes.Entries && itemPlan.LibraryEntries == null)
            {
                var created = await widgetsService.CreateEntriesWidget(new CreateEntriesWidgetDto
                {
                    TrackerId = itemPlan.TrackerId!,
                    Name = itemPlan.Doc.Name
                });

                if (!created.IsSuccess)
                {
                    plan.Errors.Add($"{itemPlan.Path}.wiring: {string.Join(" ", created.Messages)}");
                    return;
                }

                itemPlan.LibraryEntries = await db.EntriesWidgets.FirstAsync(w => w.Id == created.Data.Id);
            }
        }

        // Placed below what the board already holds until the document's own layout, applied
        // right after, says otherwise.
        private DashboardItem BuildNewItem(DocumentPlan plan, ItemPlan itemPlan)
        {
            var dashboard = plan.Dashboard;
            DashboardItem built;

            switch (itemPlan.Item.Type)
            {
                case DashboardWidgetTypes.Analytic:
                    var widget = itemPlan.LibraryWidget!;
                    var overrides = widget.Sources
                        .OrderBy(s => s.Order)
                        .Select((source, index) => new PlaceWidgetSourceOverrideDto
                        {
                            WidgetSourceId = source.Id,
                            Label = itemPlan.SourceInputs[index].Label,
                            ViewId = itemPlan.SourceInputs[index].ViewId
                        })
                        .ToDictionary(o => o.WidgetSourceId);

                    built = BuildAnalyticItem(dashboard, widget, new PlaceWidgetDto(), overrides);
                    break;

                case DashboardWidgetTypes.Entries:
                    built = BuildLayoutItem(dashboard, dashboard.Id, DashboardWidgetTypes.Entries, DashboardGrid.EntriesSize, null);
                    built.EntriesWidgetId = itemPlan.LibraryEntries!.Id;
                    break;

                default:
                    built = BuildLayoutItem(dashboard, dashboard.Id, itemPlan.Item.Type, DefaultSizeOf(itemPlan.Item.Type), null);
                    break;
            }

            built.Id = itemPlan.Item.Id;
            built.Key = itemPlan.Key;
            return built;
        }

        private static (int Width, int Height) DefaultSizeOf(string type) => type switch
        {
            DashboardWidgetTypes.QuickAdd => DashboardGrid.QuickAddSize,
            DashboardWidgetTypes.Filter => DashboardGrid.FilterSize,
            DashboardWidgetTypes.Header => DashboardGrid.HeaderSize,
            DashboardWidgetTypes.Divider => DashboardGrid.DividerSize,
            DashboardWidgetTypes.Note => DashboardGrid.NoteSize,
            DashboardWidgetTypes.Container => DashboardGrid.ContainerSize,
            DashboardWidgetTypes.TabsContainer => DashboardGrid.TabsContainerSize,
            _ => DashboardGrid.DefaultSizeFor(string.Empty)
        };

        // ----- Presets -----

        // A preset is told apart by its name: one the board already has is updated in place, a
        // new name creates one, and (when the document lists presets at all) a name it leaves out
        // is deleted.
        private async Task PlanPresets(DocumentPlan plan)
        {
            var existing = (await db.DashboardViews
                    .Where(v => v.DashboardId == plan.Dashboard.Id)
                    .OrderBy(v => v.Order)
                    .Select(v => new { v.Id, v.Name })
                    .ToListAsync())
                .GroupBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var presets = plan.Document.Board.Presets;
            if (presets == null)
            {
                foreach (var (name, id) in existing)
                    plan.PresetIdByName[name] = id;
                return;
            }

            plan.PresetsAuthoritative = true;
            var errors = plan.Errors;

            if (presets.Count > DataLimits.MaxDashboardViewCount)
                errors.Add($"board.presets: {Messages.MaxNumberReached("dashboard views", DataLimits.MaxDashboardViewCount)}");

            for (var index = 0; index < presets.Count; index++)
            {
                var preset = presets[index];
                var path = $"board.presets[{index}]";
                var name = preset.Name?.Trim() ?? string.Empty;

                if (name.Length == 0)
                {
                    errors.Add($"{path}.name is required.");
                    continue;
                }

                if (plan.PresetIdByName.ContainsKey(name))
                {
                    errors.Add($"{path}.name: \"{name}\" is used by another preset.");
                    continue;
                }

                var isNew = !existing.TryGetValue(name, out var id);
                var resolvedId = isNew ? Guid.NewGuid().ToString() : id!;

                var clauses = new List<ClauseDto>();
                for (var c = 0; c < preset.Clauses.Count; c++)
                {
                    var clause = ToClauseDto(preset.Clauses[c], preset.Clauses[c].Kind ?? QueryKinds.Filter);
                    var check = QueryBuilder.ValidateClause(clause);
                    if (check.IsFailure)
                        errors.Add($"{path}.clauses[{c}]: {string.Join(" ", check.Messages)}");
                    clauses.Add(clause);
                }

                plan.PresetIdByName[name] = resolvedId;
                plan.Presets.Add(new PresetPlan(resolvedId, isNew, name, clauses, path));
            }
        }

        private static ClauseDto ToClauseDto(DashboardDocumentClauseDto clause, string kind) => new()
        {
            Kind = kind,
            DataType = clause.DataType,
            Operator = kind == QueryKinds.Filter ? clause.Operator : null,
            Value = kind == QueryKinds.Filter ? clause.Value : null,
            Descending = kind == QueryKinds.Sort && clause.Descending
        };

        private async Task WritePresets(DocumentPlan plan, string ownerId)
        {
            if (!plan.PresetsAuthoritative)
                return;

            for (var index = 0; index < plan.Presets.Count; index++)
            {
                var preset = plan.Presets[index];

                var clauses = await ResolveDashboardViewClauses(ownerId, preset.Clauses);
                if (clauses.IsFailure)
                {
                    plan.Errors.Add($"{preset.Path}: {string.Join(" ", clauses.Messages)}");
                    continue;
                }

                if (preset.IsNew)
                {
                    db.DashboardViews.Add(new DashboardView
                    {
                        Id = preset.Id,
                        DashboardId = plan.Dashboard.Id,
                        Name = preset.Name,
                        Order = index
                    });
                }
                else
                {
                    var view = await db.DashboardViews.AsTracking().FirstAsync(v => v.Id == preset.Id);
                    view.Name = preset.Name;
                    view.Order = index;
                    await db.DashboardViewQueries.Where(q => q.DashboardViewId == preset.Id).ExecuteDeleteAsync();
                }

                for (var c = 0; c < clauses.Data!.Count; c++)
                    db.DashboardViewQueries.Add(new DashboardViewQuery { DashboardViewId = preset.Id, QueryId = clauses.Data[c].Id, Order = c });
            }
        }

        // ----- Filters -----

        // What a clause is called in the document when it was not given a key: its filter's key
        // and its place in the filter, which is stable for as long as the clauses are.
        private static string ClauseKey(string filterKey, int index) => $"{filterKey}-{index + 1}";

        private void ResolveFilter(DocumentPlan plan, ItemPlan itemPlan)
        {
            var filter = itemPlan.Doc.Wiring!.Filter!;
            var path = $"{itemPlan.Path}.wiring.filter";
            var errors = plan.Errors;

            var dto = new SaveFilterItemDto();
            var keys = new List<string>();
            var values = new List<string?>();

            if (filter.Clauses.Count == 0)
                errors.Add($"{path}.clauses: needs at least one clause.");

            for (var index = 0; index < filter.Clauses.Count; index++)
            {
                var clause = filter.Clauses[index];
                var clausePath = $"{path}.clauses[{index}]";

                if (clause.Kind != null && clause.Kind != QueryKinds.Filter)
                    errors.Add($"{clausePath}.kind: a filter widget only takes filters.");

                var key = Blank(clause.Key) ?? ClauseKey(itemPlan.Key, index);
                if (plan.FilterClauseKeys.ContainsKey(key))
                    errors.Add($"{clausePath}.key: \"{key}\" is used by another clause. Keys are shared across the board.");
                plan.FilterClauseKeys[key] = itemPlan.Key;

                var shape = ToClauseDto(clause, QueryKinds.Filter);
                shape.Value = null;
                var check = QueryBuilder.ValidateClause(shape);
                if (check.IsFailure)
                    errors.Add($"{clausePath}: {string.Join(" ", check.Messages)}");

                dto.Clauses.Add(shape);
                keys.Add(key);
                values.Add(clause.Value);
            }

            foreach (var name in filter.Presets ?? [])
            {
                if (plan.PresetIdByName.TryGetValue(name.Trim(), out var presetId))
                    dto.PresetIds.Add(presetId);
                else
                    errors.Add($"{path}.presets: no preset named \"{name}\".");
            }

            itemPlan.Filter = new ResolvedFilter(dto, keys, values, path);
        }

        // Once every item is planned, a link can name any of them and find its trackers.
        private void ResolveFilterLinks(DocumentPlan plan)
        {
            foreach (var itemPlan in plan.Items.Where(p => p.Filter != null))
            {
                var filter = itemPlan.Doc.Wiring!.Filter!;
                var resolved = itemPlan.Filter!;
                var previous = itemPlan.IsNew ? null : TryParseFilterConfig(itemPlan.Item.Config);

                for (var index = 0; index < filter.Links.Count; index++)
                {
                    var link = filter.Links[index];
                    var path = $"{resolved.Path}.links[{index}]";

                    if (!plan.ByKey.TryGetValue(link.Item ?? string.Empty, out var target))
                    {
                        plan.Errors.Add($"{path}.item: \"{link.Item}\" is not an item in this document.");
                        continue;
                    }

                    var trackerId = link.TrackerName != null
                        ? plan.Lookups.ResolveTracker(path, link.TrackerName, plan.Errors)
                        : SingleTrackerOf(target);

                    if (trackerId == null)
                    {
                        if (link.TrackerName == null)
                            plan.Errors.Add($"{path}: name the tracker with trackerName, since {link.Item} does not read exactly one.");
                        continue;
                    }

                    var inUse = previous?.Links
                        .FirstOrDefault(l => l.ItemId == target.Item.Id && l.TrackerId == trackerId)?.FieldByQuery.Values;

                    var fieldByQuery = new Dictionary<string, string>();
                    foreach (var (clauseKey, fieldName) in link.Fields)
                    {
                        var clauseIndex = resolved.ClauseKeys.IndexOf(clauseKey);
                        if (clauseIndex < 0)
                        {
                            plan.Errors.Add($"{path}.fields: this filter has no clause with the key \"{clauseKey}\".");
                            continue;
                        }

                        var fieldId = plan.Lookups.ResolveField($"{path}.fields.{clauseKey}", trackerId, fieldName, plan.Errors, inUse);
                        if (fieldId != null)
                            fieldByQuery[clauseIndex.ToString()] = fieldId;
                    }

                    resolved.Dto.Links.Add(new WidgetLinkDto { ItemId = target.Item.Id, TrackerId = trackerId, FieldByQuery = fieldByQuery });
                }
            }
        }

        // The one tracker a widget reads, or null when it reads none or several.
        private static string? SingleTrackerOf(ItemPlan target)
        {
            List<string> trackerIds;

            if (!target.IsNew)
                trackerIds = ResolveItemTrackerIds(target.Item);
            else if (target.Definition != null)
                trackerIds = target.Definition.Sources.Select(s => s.TrackerId).Distinct().ToList();
            else if (target.LibraryWidget != null)
                trackerIds = target.LibraryWidget.Sources.Select(s => s.TrackerId).Distinct().ToList();
            else
                trackerIds = target.TrackerId != null ? [target.TrackerId] : [];

            return trackerIds.Count == 1 ? trackerIds[0] : null;
        }

        private async Task WriteFilters(
            DocumentPlan plan,
            Dashboard dashboard,
            Dictionary<string, DashboardItem> itemsById,
            Dictionary<string, string> keyToSlot,
            string ownerId)
        {
            var restated = new HashSet<string>();

            foreach (var itemPlan in plan.Items.Where(p => p.Filter != null))
            {
                var resolved = itemPlan.Filter!;
                var item = itemsById[itemPlan.Item.Id];
                restated.Add(item.Id);

                var previous = TryParseFilterConfig(item.Config);
                var built = await BuildFilterConfig(dashboard, ownerId, resolved.Dto, previous?.Slots);
                if (!built.IsSuccess)
                {
                    plan.Errors.Add($"{resolved.Path}: {string.Join(" ", built.Messages)}");
                    continue;
                }

                var config = built.Data!;
                for (var index = 0; index < config.Slots.Count; index++)
                {
                    if (!string.IsNullOrEmpty(resolved.Values[index]))
                        config.ValueBySlot[config.Slots[index].SlotId] = resolved.Values[index];
                    keyToSlot[resolved.ClauseKeys[index]] = config.Slots[index].SlotId;
                }

                // The pooled clause queries are only added by the build, and the value check reads them back.
                await db.SaveChangesAsync();

                var queryIds = config.Slots.Select(s => s.QueryId).Distinct().ToList();
                var queries = await db.Queries.Where(q => queryIds.Contains(q.Id)).ToDictionaryAsync(q => q.Id);

                var values = ValidateFilterValues(config.Slots, queries, config.ValueBySlot);
                if (values.IsFailure)
                {
                    plan.Errors.Add($"{resolved.Path}.clauses: {string.Join(" ", values.Messages)}");
                    continue;
                }

                config.ValueBySlot = NormalizeValues(config.ValueBySlot);
                item.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
            }

            // A filter the document left alone still owns its clauses, which goal conditions may
            // name by the keys an export gave them.
            foreach (var filterItem in dashboard.Items.Where(i => i.Type == DashboardWidgetTypes.Filter && !restated.Contains(i.Id)))
            {
                var slots = TryParseFilterConfig(filterItem.Config)?.Slots ?? [];
                for (var index = 0; index < slots.Count; index++)
                    keyToSlot.TryAdd(ClauseKey(filterItem.Key!, index), slots[index].SlotId);
            }
        }

        private void WriteGoalTargets(
            DocumentPlan plan,
            Dashboard dashboard,
            Dictionary<string, DashboardItem> itemsById,
            Dictionary<string, string> keyToSlot)
        {
            foreach (var itemPlan in plan.Items.Where(p => p.GoalRows != null))
            {
                var item = itemsById[itemPlan.Item.Id];
                var path = $"{itemPlan.Path}.wiring.goalConditionalTargets";
                var rows = new List<GoalConditionalTargetDto>();
                var keysResolve = true;

                for (var index = 0; index < itemPlan.GoalRows!.Count; index++)
                {
                    var row = itemPlan.GoalRows[index];
                    var conditions = new Dictionary<string, string>();

                    foreach (var (key, value) in row.Conditions)
                    {
                        if (keyToSlot.TryGetValue(key, out var slotId))
                        {
                            conditions[slotId] = value;
                            continue;
                        }

                        plan.Errors.Add($"{path}[{index}].conditions: no filter clause has the key \"{key}\".");
                        keysResolve = false;
                    }

                    rows.Add(new GoalConditionalTargetDto { Conditions = conditions, Target = row.Target });
                }

                if (!keysResolve)
                    continue;

                if (item.Widget?.ResultType != AnalyticTypes.Goal)
                {
                    if (rows.Count > 0)
                        plan.Errors.Add($"{path}: only a goal widget has conditional targets.");
                    continue;
                }

                var targets = ValidateGoalConditionalTargets(dashboard, item, rows);
                if (targets.IsFailure)
                    plan.Errors.Add($"{path}: {string.Join(" ", targets.Messages)}");
                else
                    item.GoalConditionalTargets = targets.Data;
            }
        }
    }
}
