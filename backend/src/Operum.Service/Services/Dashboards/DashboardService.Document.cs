using System.Text.Json;
using System.Text.Json.Nodes;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.Enums;
using Operum.Model.Models;

namespace Operum.Service.Services.Dashboards
{
    // The board as one hand-editable document. Everything the grid and the edit dialogs
    // decide is writable here; the wiring a widget was built with is echoed read-only, and a
    // save that changed it fails rather than dropping the edit silently.
    public partial class DashboardService
    {
        public async Task<Result<DashboardDocumentDto>> GetDashboardDocument(string dashboardId)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            return Result.Success(BuildDocument(dashboard));
        }

        // Validated whole before anything is written, so a document with three mistakes in it
        // reports all three and leaves the board untouched.
        public async Task<Result<List<DashboardWidgetDto>>> SaveDashboardDocument(string dashboardId, DashboardDocumentDto document)
        {
            var dashboard = await GetUserDashboard(dashboardId);
            if (dashboard == null)
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("dashboard"));

            if (document.SchemaVersion != DashboardDocumentDto.CurrentSchemaVersion)
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"schemaVersion: expected {DashboardDocumentDto.CurrentSchemaVersion}, got {document.SchemaVersion}.");

            var errors = new List<string>();

            if (!string.IsNullOrEmpty(document.Board.Id) && document.Board.Id != dashboard.Id)
                errors.Add("board.id is read-only.");

            var boardName = document.Board.Name?.Trim() ?? string.Empty;
            if (boardName.Length == 0)
                errors.Add("board.name is required.");

            var itemErrors = MatchItemSet(dashboard, document);
            if (itemErrors.Count > 0)
                return Result.Failure(ResultStatusCodes.BadRequest, [.. errors, .. itemErrors]);

            var itemsById = dashboard.Items.ToDictionary(i => i.Id);
            var containerIds = dashboard.Items
                .Where(i => DashboardWidgetTypes.IsContainer(i.Type))
                .Select(i => i.Id)
                .ToHashSet();

            // Every write the document asks for, collected first and applied only once the
            // whole document has checked out.
            var planned = new List<PlannedItemWrite>();

            for (var index = 0; index < document.Items.Count; index++)
            {
                var documentItem = document.Items[index];
                var item = itemsById[documentItem.Id];
                var path = $"items[{index}]";

                ValidateReadOnly(path, documentItem, item, errors);

                var write = new PlannedItemWrite(item);

                ValidatePlacement(path, documentItem, item, containerIds, itemsById, write, errors);
                await ValidateTypeSpecific(path, documentItem, item, write, errors);

                planned.Add(write);
            }

            if (errors.Count > 0)
                return Result.Failure(ResultStatusCodes.BadRequest, errors);

            dashboard.Name = boardName;
            if (document.Board.Color.IsSet)
                dashboard.Color = Blank(document.Board.Color.Value);
            if (document.Board.Icon.IsSet)
                dashboard.Icon = Blank(document.Board.Icon.Value);

            foreach (var write in planned)
                write.Apply();

            var tabOrderByContainer = dashboard.Items
                .Where(i => i.Type == DashboardWidgetTypes.TabsContainer)
                .ToDictionary(
                    i => i.Id,
                    i => (TryParseTabsContainerConfig(i.Config)?.Tabs ?? []).Select(t => t.Id).ToList());

            RecomputeItemOrder(dashboard, tabOrderByContainer);

            await db.SaveChangesAsync();

            return Result.Success(await BuildWidgets(dashboard));
        }

        // A document must account for the board exactly as it stands: widgets are added and
        // removed in the UI, where a removal also reparents children and unpicks the filter
        // links that named the widget.
        private static List<string> MatchItemSet(Dashboard dashboard, DashboardDocumentDto document)
        {
            var errors = new List<string>();

            var seen = new HashSet<string>();
            var duplicated = new List<string>();
            foreach (var documentItem in document.Items)
            {
                if (string.IsNullOrEmpty(documentItem.Id))
                {
                    errors.Add("Every item needs an id.");
                    return errors;
                }

                if (!seen.Add(documentItem.Id))
                    duplicated.Add(documentItem.Id);
            }

            if (duplicated.Count > 0)
                errors.Add($"Listed more than once: {string.Join(", ", duplicated.Distinct())}.");

            var onBoard = dashboard.Items.Select(i => i.Id).ToHashSet();

            var unknown = seen.Except(onBoard).ToList();
            if (unknown.Count > 0)
                errors.Add($"Not on this board: {string.Join(", ", unknown)}. Widgets are added from the board's Add widget menu.");

            var missing = onBoard.Except(seen).ToList();
            if (missing.Count > 0)
                errors.Add($"Missing from the document: {string.Join(", ", missing)}. Every widget on the board has to be listed; delete one from the board itself.");

            return errors;
        }

        private static void ValidateReadOnly(string path, DashboardDocumentItemDto documentItem, DashboardItem item, List<string> errors)
        {
            if (documentItem.Type != null && documentItem.Type != item.Type)
                errors.Add($"{path}.type is read-only.");

            if (documentItem.Name != null && documentItem.Name != ResolveItemName(item))
                errors.Add($"{path}.name is read-only. Rename the widget in the Widget Library.");

            if (documentItem.Wiring != null && Canonical(documentItem.Wiring) != Canonical(BuildWiring(item)))
                errors.Add($"{path}.wiring is read-only. Sources, filter links and goal targets are edited from the widget's own dialog.");
        }

        private static void ValidatePlacement(
            string path,
            DashboardDocumentItemDto documentItem,
            DashboardItem item,
            IReadOnlySet<string> containerIds,
            IReadOnlyDictionary<string, DashboardItem> itemsById,
            PlannedItemWrite write,
            List<string> errors)
        {
            if (documentItem.Layout != null)
                ValidateLayout($"{path}.layout", documentItem.Layout, DashboardLayoutVariants.Desktop, write, isMobile: false, errors);

            if (documentItem.MobileLayout != null)
                ValidateLayout($"{path}.mobileLayout", documentItem.MobileLayout, DashboardLayoutVariants.Mobile, write, isMobile: true, errors);

            if (documentItem.Color.IsSet)
            {
                write.Color = Blank(documentItem.Color.Value);
                write.SetColor = true;
            }

            if (documentItem.ShowTrend.HasValue)
                write.ShowTrend = documentItem.ShowTrend;

            if (documentItem.YAxisFromZero.HasValue)
                write.YAxisFromZero = documentItem.YAxisFromZero;

            // A field left out keeps what the item already has, so the pair is validated as
            // it will end up rather than as it was written.
            var parentId = documentItem.ParentItemId.IsSet ? Blank(documentItem.ParentItemId.Value) : item.ParentItemId;
            var tabId = documentItem.ParentTabId.IsSet ? Blank(documentItem.ParentTabId.Value) : item.ParentTabId;

            if (parentId == null)
            {
                if (tabId != null)
                    errors.Add($"{path}.parentTabId: only an item inside a tabs container has a tab.");

                write.ParentItemId = null;
                write.ParentTabId = null;
                write.SetParent = true;
                return;
            }

            if (parentId == item.Id)
            {
                errors.Add($"{path}.parentItemId: an item cannot contain itself.");
                return;
            }

            if (DashboardWidgetTypes.IsContainer(item.Type))
            {
                errors.Add($"{path}.parentItemId: a container cannot be nested inside another.");
                return;
            }

            if (!containerIds.Contains(parentId))
            {
                errors.Add($"{path}.parentItemId: {parentId} is not a container on this board.");
                return;
            }

            var parent = itemsById[parentId];
            if (parent.Type == DashboardWidgetTypes.TabsContainer)
            {
                // Validated against the tabs already stored: the document can rename and
                // reorder them but never change which ids exist.
                var tabIds = (TryParseTabsContainerConfig(parent.Config)?.Tabs ?? []).Select(t => t.Id).ToList();
                if (tabId == null)
                    errors.Add($"{path}.parentTabId is required inside a tabs container.");
                else if (!tabIds.Contains(tabId))
                    errors.Add($"{path}.parentTabId: {tabId} is not a tab of {parentId}.");
            }
            else if (tabId != null)
            {
                errors.Add($"{path}.parentTabId: {parentId} has no tabs.");
            }

            write.ParentItemId = parentId;
            write.ParentTabId = parent.Type == DashboardWidgetTypes.TabsContainer ? tabId : null;
            write.SetParent = true;
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

        private async Task ValidateTypeSpecific(
            string path,
            DashboardDocumentItemDto documentItem,
            DashboardItem item,
            PlannedItemWrite write,
            List<string> errors)
        {
            var takesText = item.Type is DashboardWidgetTypes.Header
                or DashboardWidgetTypes.Note
                or DashboardWidgetTypes.Container
                or DashboardWidgetTypes.TabsContainer;

            if (documentItem.Text.IsSet && !takesText)
                errors.Add($"{path}.text: a {item.Type} widget has no text.");

            if (documentItem.Tabs.IsSet && item.Type != DashboardWidgetTypes.TabsContainer)
                errors.Add($"{path}.tabs: only a tabs container has tabs.");

            if (documentItem.ColumnFieldIds.IsSet && item.Type != DashboardWidgetTypes.Entries)
                errors.Add($"{path}.columnFieldIds: only an entries widget has columns.");

            // These three have no null state, so null would otherwise be a write that quietly
            // did nothing.
            if (documentItem.Text is { IsSet: true, Value: null })
                errors.Add($"{path}.text: cannot be null. Use \"\" to clear it.");

            if (documentItem.Tabs is { IsSet: true, Value: null })
                errors.Add($"{path}.tabs: cannot be null. Leave it out to keep the tabs as they are.");

            if (documentItem.ColumnFieldIds is { IsSet: true, Value: null })
                errors.Add($"{path}.columnFieldIds: cannot be null. Use [] to show every field.");

            if (documentItem.Text is { IsSet: true, Value: not null } text
                && takesText
                && item.Type != DashboardWidgetTypes.TabsContainer)
            {
                var maxLength = item.Type == DashboardWidgetTypes.Note
                    ? DataLimits.MaxNoteTextLength
                    : DataLimits.MaxHeaderTextLength;

                if (text.Value!.Length > maxLength)
                    errors.Add($"{path}.text: cannot exceed {maxLength} characters.");
                else
                    write.Config = JsonSerializer.Serialize(new TextWidgetConfigDto { Text = text.Value }, ConfigJsonOptions);
            }

            if (item.Type == DashboardWidgetTypes.TabsContainer)
                ValidateTabs(path, documentItem, item, write, errors);

            if (item.Type == DashboardWidgetTypes.Entries
                && documentItem.ColumnFieldIds is { IsSet: true, Value: not null } columnFieldIds)
            {
                if (item.EntriesWidget == null)
                {
                    errors.Add($"{path}.columnFieldIds: this entries widget has no tracker.");
                    return;
                }

                var columns = await ResolveEntriesColumns(item.EntriesWidget.TrackerId, columnFieldIds.Value!);
                if (columns.IsFailure)
                    errors.Add($"{path}.columnFieldIds: {string.Join(" ", columns.Messages)}");
                else
                    write.Config = JsonSerializer.Serialize(new EntriesWidgetConfigDto { ColumnFieldIds = columns.Data! }, ConfigJsonOptions);
            }
        }

        private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // Renaming and reordering only: adding or removing a tab moves whichever widgets sat
        // in it, which is the tabs dialog's job.
        private static void ValidateTabs(
            string path,
            DashboardDocumentItemDto documentItem,
            DashboardItem item,
            PlannedItemWrite write,
            List<string> errors)
        {
            var setsTitle = documentItem.Text is { IsSet: true, Value: not null };
            var setsTabs = documentItem.Tabs is { IsSet: true, Value: not null };
            if (!setsTitle && !setsTabs)
                return;

            var current = TryParseTabsContainerConfig(item.Config);
            if (current == null)
            {
                errors.Add($"{path}: this tabs container's stored tabs could not be read, so the document cannot edit them.");
                return;
            }

            var title = Blank(documentItem.Text.Value);
            if (setsTitle && (title?.Length ?? 0) > DataLimits.MaxHeaderTextLength)
            {
                errors.Add($"{path}.text: cannot exceed {DataLimits.MaxHeaderTextLength} characters.");
                return;
            }

            var tabs = current.Tabs;
            if (setsTabs)
            {
                var currentIds = current.Tabs.Select(t => t.Id).ToList();
                var documentIds = documentItem.Tabs.Value!.Select(t => t.Id ?? string.Empty).ToList();

                if (!currentIds.OrderBy(id => id).SequenceEqual(documentIds.OrderBy(id => id)))
                {
                    errors.Add($"{path}.tabs: must list every existing tab id exactly once. Tabs are added and removed from the tabs container's own dialog.");
                    return;
                }

                var renamed = new List<TabDefDto>();
                foreach (var tab in documentItem.Tabs.Value!)
                {
                    var name = tab.Name?.Trim() ?? string.Empty;
                    if (name.Length == 0 || name.Length > DataLimits.MaxTabNameLength)
                    {
                        errors.Add($"{path}.tabs: a tab name is required and cannot exceed {DataLimits.MaxTabNameLength} characters.");
                        return;
                    }

                    renamed.Add(new TabDefDto { Id = tab.Id, Name = name });
                }

                tabs = renamed;
            }

            write.Config = JsonSerializer.Serialize(new TabsContainerConfigDto
            {
                Title = setsTitle ? title : current.Title,
                Tabs = tabs
            }, ConfigJsonOptions);
        }

        private static DashboardDocumentDto BuildDocument(Dashboard dashboard) => new()
        {
            SchemaVersion = DashboardDocumentDto.CurrentSchemaVersion,
            Board = new DashboardDocumentBoardDto
            {
                Id = dashboard.Id,
                Name = dashboard.Name,
                Color = new Optional<string>(dashboard.Color),
                Icon = new Optional<string>(dashboard.Icon)
            },
            Items = dashboard.Items.OrderBy(i => i.Order).Select(BuildDocumentItem).ToList()
        };

        private static DashboardDocumentItemDto BuildDocumentItem(DashboardItem item)
        {
            var documentItem = new DashboardDocumentItemDto
            {
                Id = item.Id,
                Type = item.Type,
                Name = ResolveItemName(item),
                ParentItemId = new Optional<string>(item.ParentItemId),
                ParentTabId = new Optional<string>(item.ParentTabId),
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
                Wiring = BuildWiring(item)
            };

            if (item.Type is DashboardWidgetTypes.Header or DashboardWidgetTypes.Note or DashboardWidgetTypes.Container)
                documentItem.Text = new Optional<string>(TryParseTextConfig(item.Config)?.Text ?? string.Empty);

            if (item.Type == DashboardWidgetTypes.TabsContainer)
            {
                var config = TryParseTabsContainerConfig(item.Config);
                documentItem.Text = new Optional<string>(config?.Title ?? string.Empty);
                documentItem.Tabs = new Optional<List<DashboardDocumentTabDto>>(
                    (config?.Tabs ?? [])
                        .Select(t => new DashboardDocumentTabDto { Id = t.Id, Name = t.Name })
                        .ToList());
            }

            if (item.Type == DashboardWidgetTypes.Entries)
                documentItem.ColumnFieldIds = new Optional<List<string>>(
                    TryParseEntriesConfig(item.Config)?.ColumnFieldIds ?? []);

            return documentItem;
        }

        private static DashboardDocumentWiringDto? BuildWiring(DashboardItem item)
        {
            var sources = item.Sources
                .OrderBy(s => s.Order)
                .Select(s => new DashboardDocumentSourceDto
                {
                    Id = s.Id,
                    TrackerName = s.WidgetSource?.Tracker?.Name ?? string.Empty,
                    Label = s.Label,
                    ViewId = s.ViewId,
                    Fields = (s.WidgetSource?.Fields ?? [])
                        .Where(f => f.Field != null)
                        .Select(f => $"{f.Purpose}: {f.Field.Name}")
                        .ToList()
                })
                .ToList();

            var filter = item.Type == DashboardWidgetTypes.Filter ? TryParseFilterConfig(item.Config) : null;
            var goalTargets = ParseGoalConditionalTargets(item.GoalConditionalTargets);

            if (sources.Count == 0 && filter == null && goalTargets.Count == 0)
                return null;

            return new DashboardDocumentWiringDto
            {
                Sources = sources.Count > 0 ? sources : null,
                Filter = filter,
                GoalConditionalTargets = goalTargets.Count > 0 ? goalTargets : null
            };
        }

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

        private sealed class PlannedItemWrite(DashboardItem item)
        {
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
                    (item.X, item.Y, item.W, item.H) = Layout.Value;
                    item.DisplayMode = DisplayMode!.Value;
                }

                if (MobileLayout.HasValue)
                {
                    (item.MobileX, item.MobileY, item.MobileW, item.MobileH) = MobileLayout.Value;
                    item.MobileDisplayMode = MobileDisplayMode!.Value;
                }

                if (SetParent)
                {
                    item.ParentItemId = ParentItemId;
                    item.ParentTabId = ParentTabId;
                }

                if (SetColor)
                    item.Color = Color;

                if (ShowTrend.HasValue)
                    item.ShowTrend = ShowTrend.Value;

                if (YAxisFromZero.HasValue)
                    item.YAxisFromZero = YAxisFromZero.Value;

                if (Config != null)
                    item.Config = Config;
            }
        }
    }
}
