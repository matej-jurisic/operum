using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Model.DTOs.Queries;
using Operum.Model.DTOs.Widgets.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;

namespace Operum.Service.Services.Dashboards
{
    // The board as one hand-editable document that can also build a board from nothing, with
    // no ids in it: items are named by key, everything else by name. A save is planned whole
    // before anything is written, so a document with three mistakes in it reports all three;
    // what remains (Library definitions the services create, and the checks that need the
    // saved items) runs in one transaction that a failure rolls back.
    public partial class DashboardService
    {
        public async Task<Result<DashboardDocumentDto>> GetDashboardDocument(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            await EnsureItemKeys(dashboard);

            return Result.Success(await BuildDocument(dashboard));
        }

        public async Task<Result<List<DashboardWidgetDto>>> SaveDashboardDocument(string dashboardId, DashboardDocumentDto document)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            await using var transaction = await db.Database.BeginTransactionAsync();

            var result = await ApplyDocument(dashboard, document);
            if (result.IsSuccess)
                await transaction.CommitAsync();

            return result;
        }

        public async Task<Result<DashboardDto>> CreateDashboardFromDocument(DashboardDocumentDto document)
        {
            var name = document.Board.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
                return Result.Failure(ResultStatusCodes.BadRequest, "board.name is required.");

            await using var transaction = await db.Database.BeginTransactionAsync();

            var created = await CreateDashboard(new CreateDashboardDto
            {
                Name = name,
                Color = document.Board.Color.IsSet ? Blank(document.Board.Color.Value) : null,
                Icon = document.Board.Icon.IsSet ? Blank(document.Board.Icon.Value) : null
            });
            if (!created.IsSuccess)
                return Result.Failure(created.StatusCode, created.Messages);

            var dashboard = (await GetUserDashboard(created.Data.Id))!;

            var applied = await ApplyDocument(dashboard, document);
            if (!applied.IsSuccess)
                return Result.Failure(applied.StatusCode, applied.Messages);

            await transaction.CommitAsync();

            return Result.Success(MapToDto((await GetUserDashboard(created.Data.Id))!));
        }

        private async Task<Result<List<DashboardWidgetDto>>> ApplyDocument(Dashboard dashboard, DashboardDocumentDto document)
        {
            if (document.SchemaVersion != DashboardDocumentDto.CurrentSchemaVersion)
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"schemaVersion: expected {DashboardDocumentDto.CurrentSchemaVersion}, got {document.SchemaVersion}. Export the board again to get the current format.");

            var errors = new List<string>();

            var boardName = document.Board.Name?.Trim() ?? string.Empty;
            if (boardName.Length == 0)
                errors.Add("board.name is required.");

            await EnsureItemKeys(dashboard);

            var plan = new DocumentPlan(dashboard, document, await LoadDocumentLookups(), errors);

            if (!MatchItemSet(plan))
                return Result.Failure(ResultStatusCodes.BadRequest, errors);

            await PlanPresets(plan);
            PlanTabs(plan);

            for (var index = 0; index < plan.Items.Count; index++)
                await PlanItem(plan, plan.Items[index]);

            ResolveFilterLinks(plan);

            if (errors.Count > 0)
                return Result.Failure(ResultStatusCodes.BadRequest, errors);

            return await WriteDocument(plan, boardName);
        }

        // ----- Keys -----

        private static readonly Regex ValidKey = new(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.Compiled);

        // An item is given its key the first time its board is exported or imported, from what it
        // is called, and keeps it from then on. Items placed in the UI have none until then.
        private async Task EnsureItemKeys(Dashboard dashboard)
        {
            var missing = dashboard.Items.Where(i => i.Key == null).OrderBy(i => i.Order).ToList();
            if (missing.Count == 0)
                return;

            var taken = dashboard.Items.Where(i => i.Key != null).Select(i => i.Key!).ToHashSet();
            foreach (var item in missing)
            {
                var seed = KeySeed(item);
                var key = seed;
                for (var n = 2; taken.Contains(key); n++)
                    key = $"{seed}-{n}";

                taken.Add(key);
                item.Key = key;
            }

            await db.SaveChangesAsync();
        }

        private static string KeySeed(DashboardItem item)
        {
            var text = item.Type switch
            {
                DashboardWidgetTypes.Header or DashboardWidgetTypes.Note or DashboardWidgetTypes.Container
                    => TryParseTextConfig(item.Config)?.Text,
                DashboardWidgetTypes.TabsContainer => TryParseTabsContainerConfig(item.Config)?.Title,
                DashboardWidgetTypes.Analytic or DashboardWidgetTypes.Entries => ResolveItemName(item),
                _ => null
            };

            var slug = Slugify(text);
            return slug.Length > 0 ? slug : Slugify(item.Type);
        }

        private static string Slugify(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var spaced = Regex.Replace(text, "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
            var slug = Regex.Replace(spaced, "[^a-z0-9]+", "-").Trim('-');
            return slug.Length > 40 ? slug[..40].TrimEnd('-') : slug;
        }

        // ----- Planning: everything that can be checked without writing -----

        private sealed class DocumentPlan(Dashboard dashboard, DashboardDocumentDto document, DocumentLookups lookups, List<string> errors)
        {
            public Dashboard Dashboard { get; } = dashboard;
            public DashboardDocumentDto Document { get; } = document;
            public DocumentLookups Lookups { get; } = lookups;
            public List<string> Errors { get; } = errors;

            public List<ItemPlan> Items { get; } = [];
            public Dictionary<string, ItemPlan> ByKey { get; } = [];
            public HashSet<string> DeletedItemIds { get; } = [];

            public Dictionary<string, List<TabDefDto>> TabsByContainer { get; } = [];
            public Dictionary<string, string> FilterClauseKeys { get; } = [];

            public List<PresetPlan> Presets { get; } = [];
            public bool PresetsAuthoritative { get; set; }
            public Dictionary<string, string> PresetIdByName { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class ItemPlan(string path, DashboardDocumentItemDto doc, DashboardItem item, bool isNew)
        {
            public string Path { get; } = path;
            public DashboardDocumentItemDto Doc { get; } = doc;
            public string Key => Doc.Key;
            public DashboardItem Item { get; set; } = item;
            public bool IsNew { get; } = isNew;
            public PlannedItemWrite Write { get; } = new();

            // Analytic
            public Widget? LibraryWidget { get; set; }
            public CreateWidgetDto? Definition { get; set; }
            public List<(string? Label, string? ViewId)> SourceInputs { get; } = [];
            public List<PlannedSourceWrite> SourceWrites { get; } = [];
            public List<GoalConditionalTargetDto>? GoalRows { get; set; }

            // QuickAdd and entries
            public string? TrackerId { get; set; }
            public EntriesWidget? LibraryEntries { get; set; }

            public ResolvedFilter? Filter { get; set; }
        }

        private sealed record PlannedSourceWrite(int Index, bool SetLabel, string? Label, bool SetView, string? ViewId);

        private sealed record ResolvedFilter(
            SaveFilterItemDto Dto,
            List<string> ClauseKeys,
            List<string?> Values,
            string Path);

        private sealed record PresetPlan(string Id, bool IsNew, string Name, List<ClauseDto> Clauses, string Path);

        // Every item on the board is accounted for: matched to one the document lists by key, or
        // deleted for being left out. A key the board does not have is a new item.
        private static bool MatchItemSet(DocumentPlan plan)
        {
            var errors = plan.Errors;
            var before = errors.Count;
            var onBoard = plan.Dashboard.Items.ToDictionary(i => i.Key!);
            var seen = new HashSet<string>();
            var duplicated = new List<string>();

            for (var index = 0; index < plan.Document.Items.Count; index++)
            {
                var doc = plan.Document.Items[index];
                var path = $"items[{index}]";

                if (string.IsNullOrWhiteSpace(doc.Key))
                {
                    errors.Add($"{path}.key: every item needs one.");
                    return false;
                }

                if (!seen.Add(doc.Key))
                {
                    duplicated.Add(doc.Key);
                    continue;
                }

                if (onBoard.TryGetValue(doc.Key, out var existing))
                {
                    var itemPlan = new ItemPlan(path, doc, existing, isNew: false);
                    plan.Items.Add(itemPlan);
                    plan.ByKey[doc.Key] = itemPlan;
                    continue;
                }

                if (!ValidKey.IsMatch(doc.Key))
                {
                    errors.Add($"{path}.key: \"{doc.Key}\" can use letters, digits, dot, dash and underscore, up to 64 characters.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(doc.Type) || !DashboardWidgetTypes.IsValid(doc.Type))
                {
                    errors.Add($"{path}.type: \"{doc.Key}\" is not on this board, so it is created as a new item and needs a type: {string.Join(", ", DashboardWidgetTypes.All.Order())}.");
                    continue;
                }

                var fresh = new ItemPlan(path, doc, new DashboardItem { DashboardId = plan.Dashboard.Id, Type = doc.Type }, isNew: true);
                plan.Items.Add(fresh);
                plan.ByKey[doc.Key] = fresh;
            }

            if (duplicated.Count > 0)
                errors.Add($"Listed more than once: {string.Join(", ", duplicated.Distinct())}.");

            foreach (var (_, item) in onBoard.Where(kv => !seen.Contains(kv.Key)))
                plan.DeletedItemIds.Add(item.Id);

            if (seen.Count > DataLimits.MaxDashboardItemCount)
                errors.Add(Messages.MaxNumberReached("dashboard items", DataLimits.MaxDashboardItemCount));

            return errors.Count == before;
        }

        private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task PlanItem(DocumentPlan plan, ItemPlan itemPlan)
        {
            var doc = itemPlan.Doc;
            var errors = plan.Errors;

            if (itemPlan.IsNew)
                await ResolveNewItem(plan, itemPlan);
            else
                ValidateReadOnly(plan, itemPlan);

            ValidateWiringFitsType(itemPlan, errors);
            ValidatePlacement(plan, itemPlan);
            ValidateTypeSpecific(plan, itemPlan);

            if (doc.Wiring?.Filter != null)
                ResolveFilter(plan, itemPlan);

            if (doc.Wiring?.GoalConditionalTargets != null)
            {
                if (itemPlan.Item.Type != DashboardWidgetTypes.Analytic)
                    errors.Add($"{itemPlan.Path}.wiring.goalConditionalTargets: only an analytic widget has them.");
                else
                    itemPlan.GoalRows = doc.Wiring.GoalConditionalTargets;
            }
        }

        private void ValidateReadOnly(DocumentPlan plan, ItemPlan itemPlan)
        {
            var doc = itemPlan.Doc;
            var item = itemPlan.Item;
            var path = itemPlan.Path;

            if (doc.Type != null && doc.Type != item.Type)
                plan.Errors.Add($"{path}.type is read-only.");

            if (doc.Name != null && doc.Name != ResolveItemName(item))
                plan.Errors.Add($"{path}.name is read-only. Rename the widget in the Widget Library.");

            if (doc.Wiring != null)
                ValidateExistingWiring(plan, itemPlan);
        }

        // Only some parts of the wiring belong to a type; naming another type's part is
        // refused rather than dropped.
        private static void ValidateWiringFitsType(ItemPlan itemPlan, List<string> errors)
        {
            var wiring = itemPlan.Doc.Wiring;
            if (wiring == null)
                return;

            var type = itemPlan.Item.Type;
            var path = $"{itemPlan.Path}.wiring";

            void Reject(bool present, string property, string owner)
            {
                if (present)
                    errors.Add($"{path}.{property}: only {owner} has this.");
            }

            var analytic = type == DashboardWidgetTypes.Analytic;
            Reject(wiring.Widget != null && !analytic, "widget", "an analytic widget");
            Reject(wiring.Sources != null && !analytic, "sources", "an analytic widget");
            Reject(wiring.Filter != null && type != DashboardWidgetTypes.Filter, "filter", "a filter widget");
            Reject(wiring.Library != null && !(analytic || type == DashboardWidgetTypes.Entries), "library", "an analytic or entries widget");
            Reject(wiring.TrackerName != null && type is not (DashboardWidgetTypes.QuickAdd or DashboardWidgetTypes.Entries), "trackerName", "a quick add or entries widget");
        }

        private void ValidatePlacement(DocumentPlan plan, ItemPlan itemPlan)
        {
            var doc = itemPlan.Doc;
            var item = itemPlan.Item;
            var path = itemPlan.Path;
            var write = itemPlan.Write;
            var errors = plan.Errors;

            if (doc.Layout != null)
                ValidateLayout($"{path}.layout", doc.Layout, DashboardLayoutVariants.Desktop, write, isMobile: false, errors);

            if (doc.MobileLayout != null)
                ValidateLayout($"{path}.mobileLayout", doc.MobileLayout, DashboardLayoutVariants.Mobile, write, isMobile: true, errors);

            if (doc.Color.IsSet)
            {
                write.Color = Blank(doc.Color.Value);
                write.SetColor = true;
            }

            if (doc.ShowTrend.HasValue)
                write.ShowTrend = doc.ShowTrend;

            if (doc.YAxisFromZero.HasValue)
                write.YAxisFromZero = doc.YAxisFromZero;

            // A field left out keeps what the item already has, so the pair is validated as
            // it will end up rather than as it was written.
            var parentKey = doc.Parent.IsSet ? Blank(doc.Parent.Value) : ExistingParentKey(plan, item);
            var tabName = Blank(doc.Tab.Value);

            if (parentKey == null)
            {
                if (doc.Tab.IsSet && tabName != null)
                    errors.Add($"{path}.tab: only an item inside a tabs container has a tab.");

                write.ParentItemId = null;
                write.ParentTabId = null;
                write.SetParent = true;
                return;
            }

            if (parentKey == doc.Key)
            {
                errors.Add($"{path}.parent: an item cannot contain itself.");
                return;
            }

            if (DashboardWidgetTypes.IsContainer(item.Type))
            {
                errors.Add($"{path}.parent: a container cannot be nested inside another.");
                return;
            }

            if (!plan.ByKey.TryGetValue(parentKey, out var parentPlan) || !DashboardWidgetTypes.IsContainer(parentPlan.Item.Type))
            {
                errors.Add($"{path}.parent: \"{parentKey}\" is not a container in this document.");
                return;
            }

            var parent = parentPlan.Item;
            string? tabId = null;

            if (parent.Type == DashboardWidgetTypes.TabsContainer)
            {
                var tabs = plan.TabsByContainer.GetValueOrDefault(parent.Id) ?? [];

                if (doc.Tab.IsSet)
                {
                    tabId = tabName == null ? null : tabs.FirstOrDefault(t => string.Equals(t.Name, tabName, StringComparison.OrdinalIgnoreCase))?.Id;
                    if (tabName == null)
                        errors.Add($"{path}.tab is required inside a tabs container.");
                    else if (tabId == null)
                        errors.Add($"{path}.tab: \"{tabName}\" is not a tab of {parentKey}. Its tabs are {Options(tabs.Select(t => t.Name))}.");
                }
                else if (item.ParentItemId == parent.Id && item.ParentTabId != null && tabs.Any(t => t.Id == item.ParentTabId))
                {
                    tabId = item.ParentTabId;
                }
                else
                {
                    errors.Add($"{path}.tab is required inside a tabs container.");
                }
            }
            else if (doc.Tab.IsSet && tabName != null)
            {
                errors.Add($"{path}.tab: {parentKey} has no tabs.");
            }

            write.ParentItemId = parent.Id;
            write.ParentTabId = tabId;
            write.SetParent = true;
        }

        private static string? ExistingParentKey(DocumentPlan plan, DashboardItem item)
        {
            if (item.ParentItemId == null)
                return null;

            return plan.Dashboard.Items.FirstOrDefault(i => i.Id == item.ParentItemId)?.Key;
        }

        // The bounds UpdateDashboardLayout clamps a dragged widget to, reported instead of
        // applied: a number silently corrected in a document reads as the save having failed.
        private static void ValidateLayout(
            string path,
            DashboardDocumentLayoutDto layout,
            string variant,
            PlannedItemWrite write,
            bool isMobile,
            List<string> errors)
        {
            var columns = DashboardGrid.ColumnsFor(variant);
            var minWidth = DashboardGrid.MinWidthFor(variant);
            var ok = true;

            if (layout.W < minWidth || layout.W > columns)
            {
                errors.Add($"{path}.w: must be between {minWidth} and {columns}.");
                ok = false;
            }

            if (layout.H < DashboardGrid.MinHeight || layout.H > DashboardGrid.MaxHeight)
            {
                errors.Add($"{path}.h: must be between {DashboardGrid.MinHeight} and {DashboardGrid.MaxHeight}.");
                ok = false;
            }

            if (layout.X < 0 || (ok && layout.X + layout.W > columns))
            {
                errors.Add($"{path}.x: must leave the widget inside {columns} columns.");
                ok = false;
            }

            if (layout.Y < 0)
            {
                errors.Add($"{path}.y: cannot be negative.");
                ok = false;
            }

            if (!DashboardDocumentDisplayModes.TryParse(layout.DisplayMode, out var displayMode))
            {
                errors.Add($"{path}.displayMode: must be one of {string.Join(", ", DashboardDocumentDisplayModes.All)}.");
                ok = false;
            }

            if (!ok)
                return;

            if (isMobile)
            {
                write.MobileLayout = (layout.X, layout.Y, layout.W, layout.H);
                write.MobileDisplayMode = displayMode;
            }
            else
            {
                write.Layout = (layout.X, layout.Y, layout.W, layout.H);
                write.DisplayMode = displayMode;
            }
        }

        private void ValidateTypeSpecific(DocumentPlan plan, ItemPlan itemPlan)
        {
            var doc = itemPlan.Doc;
            var item = itemPlan.Item;
            var path = itemPlan.Path;
            var write = itemPlan.Write;
            var errors = plan.Errors;

            var takesText = item.Type is DashboardWidgetTypes.Header
                or DashboardWidgetTypes.Note
                or DashboardWidgetTypes.Container
                or DashboardWidgetTypes.TabsContainer;

            if (doc.Text.IsSet && !takesText)
                errors.Add($"{path}.text: a {item.Type} widget has no text.");

            if (doc.Tabs.IsSet && item.Type != DashboardWidgetTypes.TabsContainer)
                errors.Add($"{path}.tabs: only a tabs container has tabs.");

            if (doc.Columns.IsSet && item.Type != DashboardWidgetTypes.Entries)
                errors.Add($"{path}.columns: only an entries widget has columns.");

            // These three have no null state, so null would otherwise be a write that quietly
            // did nothing.
            if (doc.Text is { IsSet: true, Value: null })
                errors.Add($"{path}.text: cannot be null. Use \"\" to clear it.");

            if (doc.Tabs is { IsSet: true, Value: null })
                errors.Add($"{path}.tabs: cannot be null. Leave it out to keep the tabs as they are.");

            if (doc.Columns is { IsSet: true, Value: null })
                errors.Add($"{path}.columns: cannot be null. Use [] to show every field.");

            if (item.Type == DashboardWidgetTypes.TabsContainer)
            {
                WriteTabsConfig(plan, itemPlan);
            }
            else if (takesText)
            {
                var maxLength = item.Type == DashboardWidgetTypes.Note
                    ? DataLimits.MaxNoteTextLength
                    : DataLimits.MaxHeaderTextLength;

                if (doc.Text is { IsSet: true, Value: not null } text)
                {
                    if (text.Value!.Length > maxLength)
                        errors.Add($"{path}.text: cannot exceed {maxLength} characters.");
                    else
                        write.Config = JsonSerializer.Serialize(new TextWidgetConfigDto { Text = text.Value }, ConfigJsonOptions);
                }
                else if (itemPlan.IsNew && item.Type is DashboardWidgetTypes.Header or DashboardWidgetTypes.Note)
                {
                    write.Config = JsonSerializer.Serialize(new TextWidgetConfigDto { Text = string.Empty }, ConfigJsonOptions);
                }
            }

            if (item.Type == DashboardWidgetTypes.Entries)
                WriteEntriesConfig(plan, itemPlan);

            if (item.Type == DashboardWidgetTypes.QuickAdd && itemPlan.IsNew && itemPlan.TrackerId != null)
                write.Config = JsonSerializer.Serialize(new QuickAddWidgetConfigDto { TrackerId = itemPlan.TrackerId }, ConfigJsonOptions);
        }

        private void WriteEntriesConfig(DocumentPlan plan, ItemPlan itemPlan)
        {
            var trackerId = itemPlan.IsNew ? itemPlan.TrackerId : itemPlan.Item.EntriesWidget?.TrackerId;
            var path = $"{itemPlan.Path}.columns";

            if (!itemPlan.Doc.Columns.IsSet || itemPlan.Doc.Columns.Value == null)
            {
                if (itemPlan.IsNew)
                    itemPlan.Write.Config = JsonSerializer.Serialize(new EntriesWidgetConfigDto(), ConfigJsonOptions);
                return;
            }

            if (trackerId == null)
            {
                if (!itemPlan.IsNew)
                    plan.Errors.Add($"{path}: this entries widget has no tracker.");
                return;
            }

            // A column already shown keeps its field even where two fields share a name.
            var current = itemPlan.IsNew ? [] : TryParseEntriesConfig(itemPlan.Item.Config)?.ColumnFieldIds ?? [];

            var resolved = new List<string>();
            foreach (var name in itemPlan.Doc.Columns.Value!)
            {
                var fieldId = plan.Lookups.ResolveField(path, trackerId, name, plan.Errors, current);
                if (fieldId != null && !resolved.Contains(fieldId))
                    resolved.Add(fieldId);
            }

            if (resolved.Count > DataLimits.MaxColumns)
            {
                plan.Errors.Add($"{path}: {Messages.MaxNumberReached("columns", DataLimits.MaxColumns)}");
                return;
            }

            itemPlan.Write.Config = JsonSerializer.Serialize(new EntriesWidgetConfigDto { ColumnFieldIds = resolved }, ConfigJsonOptions);
        }

        // ----- Tabs -----

        // Settled for every container before any child is placed, since a child names its tab.
        // A tab keeps its stored id when the document names it again, or, where the document
        // renames tabs, takes over the id of the one at the same position.
        private void PlanTabs(DocumentPlan plan)
        {
            foreach (var itemPlan in plan.Items.Where(p => p.Item.Type == DashboardWidgetTypes.TabsContainer))
            {
                var path = $"{itemPlan.Path}.tabs";
                var existing = itemPlan.IsNew ? [] : TryParseTabsContainerConfig(itemPlan.Item.Config)?.Tabs ?? [];

                if (itemPlan.Doc.Tabs is not { IsSet: true, Value: not null } setTabs)
                {
                    plan.TabsByContainer[itemPlan.Item.Id] = existing.Count > 0
                        ? existing
                        : [new TabDefDto { Id = Guid.NewGuid().ToString(), Name = "Tab 1" }];
                    continue;
                }

                var names = setTabs.Value!.Select(n => n?.Trim() ?? string.Empty).ToList();

                if (names.Count is < 1 or > DataLimits.MaxDashboardTabCount)
                {
                    plan.Errors.Add($"{path}: needs between 1 and {DataLimits.MaxDashboardTabCount} tabs.");
                    continue;
                }

                if (names.Any(n => n.Length == 0 || n.Length > DataLimits.MaxTabNameLength))
                {
                    plan.Errors.Add($"{path}: a tab name is required and cannot exceed {DataLimits.MaxTabNameLength} characters.");
                    continue;
                }

                if (names.Select(n => n.ToLowerInvariant()).Distinct().Count() != names.Count)
                {
                    plan.Errors.Add($"{path}: tab names must differ from one another.");
                    continue;
                }

                var kept = new Dictionary<int, string>();
                var claimed = new HashSet<string>();
                for (var i = 0; i < names.Count; i++)
                {
                    var match = existing.FirstOrDefault(t => string.Equals(t.Name, names[i], StringComparison.OrdinalIgnoreCase));
                    if (match != null && claimed.Add(match.Id))
                        kept[i] = match.Id;
                }

                var unclaimed = new Queue<TabDefDto>(existing.Where(t => !claimed.Contains(t.Id)));
                var tabs = new List<TabDefDto>();
                for (var i = 0; i < names.Count; i++)
                {
                    var id = kept.TryGetValue(i, out var keptId)
                        ? keptId
                        : unclaimed.TryDequeue(out var renamed) ? renamed.Id : Guid.NewGuid().ToString();

                    tabs.Add(new TabDefDto { Id = id, Name = names[i] });
                }

                plan.TabsByContainer[itemPlan.Item.Id] = tabs;
            }
        }

        private void WriteTabsConfig(DocumentPlan plan, ItemPlan itemPlan)
        {
            if (!plan.TabsByContainer.TryGetValue(itemPlan.Item.Id, out var tabs))
                return;

            var setsTitle = itemPlan.Doc.Text is { IsSet: true, Value: not null };
            var setsTabs = itemPlan.Doc.Tabs is { IsSet: true, Value: not null };
            if (!itemPlan.IsNew && !setsTitle && !setsTabs)
                return;

            var title = Blank(itemPlan.Doc.Text.Value);
            if (setsTitle && (title?.Length ?? 0) > DataLimits.MaxHeaderTextLength)
            {
                plan.Errors.Add($"{itemPlan.Path}.text: cannot exceed {DataLimits.MaxHeaderTextLength} characters.");
                return;
            }

            var current = itemPlan.IsNew ? null : TryParseTabsContainerConfig(itemPlan.Item.Config);
            itemPlan.Write.Config = JsonSerializer.Serialize(new TabsContainerConfigDto
            {
                Title = setsTitle ? title : current?.Title,
                Tabs = tabs
            }, ConfigJsonOptions);
        }

        // ----- Writing -----

        private async Task<Result<List<DashboardWidgetDto>>> WriteDocument(DocumentPlan plan, string boardName)
        {
            var user = currentUserService.GetCurrentUser();
            var dashboard = plan.Dashboard;
            var document = plan.Document;
            var errors = plan.Errors;

            dashboard.Name = boardName;
            if (document.Board.Color.IsSet)
                dashboard.Color = Blank(document.Board.Color.Value);
            if (document.Board.Icon.IsSet)
                dashboard.Icon = Blank(document.Board.Icon.Value);

            await WritePresets(plan, user.Id);
            if (errors.Count > 0)
                return Result.Failure(ResultStatusCodes.BadRequest, errors);

            foreach (var itemPlan in plan.Items.Where(p => p.IsNew))
                await CreateLibraryDefinition(plan, itemPlan);

            if (errors.Count > 0)
                return Result.Failure(ResultStatusCodes.BadRequest, errors);

            foreach (var itemPlan in plan.Items.Where(p => p.IsNew))
            {
                itemPlan.Item = BuildNewItem(plan, itemPlan);
                db.DashboardItems.Add(itemPlan.Item);
                if (!dashboard.Items.Contains(itemPlan.Item))
                    dashboard.Items.Add(itemPlan.Item);
            }

            foreach (var itemPlan in plan.Items)
            {
                itemPlan.Write.Item = itemPlan.Item;
                itemPlan.Write.Apply();

                foreach (var sourceWrite in itemPlan.SourceWrites)
                {
                    var source = itemPlan.Item.Sources.OrderBy(s => s.Order).ElementAt(sourceWrite.Index);
                    if (sourceWrite.SetLabel)
                        source.Label = sourceWrite.Label;
                    if (sourceWrite.SetView)
                        source.ViewId = sourceWrite.ViewId;
                }
            }

            await db.SaveChangesAsync();

            // The board is read again so every item, new ones included, carries the full
            // graph the filter and goal checks read their trackers and fields from.
            db.ChangeTracker.Clear();
            dashboard = (await GetUserDashboard(dashboard.Id))!;

            var itemsById = dashboard.Items.ToDictionary(i => i.Id);
            var keyToSlot = new Dictionary<string, string>();

            await WriteFilters(plan, dashboard, itemsById, keyToSlot, user.Id);
            WriteGoalTargets(plan, dashboard, itemsById, keyToSlot);

            if (errors.Count > 0)
                return Result.Failure(ResultStatusCodes.BadRequest, errors);

            await WriteRemovals(plan, dashboard, itemsById);

            var tabOrderByContainer = dashboard.Items
                .Where(i => i.Type == DashboardWidgetTypes.TabsContainer)
                .ToDictionary(
                    i => i.Id,
                    i => (TryParseTabsContainerConfig(i.Config)?.Tabs ?? []).Select(t => t.Id).ToList());

            RecomputeItemOrder(dashboard, tabOrderByContainer);

            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        private async Task WriteRemovals(DocumentPlan plan, Dashboard dashboard, Dictionary<string, DashboardItem> itemsById)
        {
            // A filter this document did not restate keeps its config, minus whatever it named
            // that is now gone.
            var restated = plan.Items.Where(p => p.Filter != null).Select(p => p.Item.Id).ToHashSet();
            var survivingPresetIds = plan.PresetsAuthoritative
                ? plan.Presets.Select(p => p.Id).ToHashSet()
                : null;

            foreach (var filterItem in dashboard.Items.Where(i => i.Type == DashboardWidgetTypes.Filter && !restated.Contains(i.Id)))
            {
                var config = TryParseFilterConfig(filterItem.Config);
                if (config == null)
                    continue;

                var links = config.Links.RemoveAll(l => plan.DeletedItemIds.Contains(l.ItemId));
                var presets = survivingPresetIds == null
                    ? 0
                    : config.PresetIds.RemoveAll(id => !survivingPresetIds.Contains(id));

                if (links + presets > 0)
                    filterItem.Config = JsonSerializer.Serialize(config, ConfigJsonOptions);
            }

            foreach (var id in plan.DeletedItemIds)
            {
                var item = itemsById[id];
                db.DashboardItems.Remove(item);
                dashboard.Items.Remove(item);
            }

            if (plan.PresetsAuthoritative)
            {
                var stale = await db.DashboardViews
                    .Where(v => v.DashboardId == dashboard.Id)
                    .Select(v => v.Id)
                    .ToListAsync();

                var keep = plan.Presets.Select(p => p.Id).ToHashSet();
                foreach (var viewId in stale.Where(id => !keep.Contains(id)))
                    await db.DashboardViews.Where(v => v.Id == viewId).ExecuteDeleteAsync();
            }
        }

        private sealed class PlannedItemWrite
        {
            public DashboardItem Item { get; set; } = null!;
            public (int X, int Y, int W, int H)? Layout { get; set; }
            public (int X, int Y, int W, int H)? MobileLayout { get; set; }
            public DashboardItemDisplayMode? DisplayMode { get; set; }
            public DashboardItemDisplayMode? MobileDisplayMode { get; set; }
            public bool SetParent { get; set; }
            public string? ParentItemId { get; set; }
            public string? ParentTabId { get; set; }
            public bool SetColor { get; set; }
            public string? Color { get; set; }
            public bool? ShowTrend { get; set; }
            public bool? YAxisFromZero { get; set; }
            public string? Config { get; set; }

            public void Apply()
            {
                if (Layout.HasValue)
                {
                    (Item.X, Item.Y, Item.W, Item.H) = Layout.Value;
                    Item.DisplayMode = DisplayMode!.Value;
                }

                if (MobileLayout.HasValue)
                {
                    (Item.MobileX, Item.MobileY, Item.MobileW, Item.MobileH) = MobileLayout.Value;
                    Item.MobileDisplayMode = MobileDisplayMode!.Value;
                }

                if (SetParent)
                {
                    Item.ParentItemId = ParentItemId;
                    Item.ParentTabId = ParentTabId;
                }

                if (SetColor)
                    Item.Color = Color;

                if (ShowTrend.HasValue)
                    Item.ShowTrend = ShowTrend.Value;

                if (YAxisFromZero.HasValue)
                    Item.YAxisFromZero = YAxisFromZero.Value;

                if (Config != null)
                    Item.Config = Config;
            }
        }
    }
}
