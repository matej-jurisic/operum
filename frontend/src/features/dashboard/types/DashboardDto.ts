import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";
import { EntryDto } from "../../entries/types/EntryDto";
import { FieldDto } from "../../fields/types/FieldDto";

/** The kinds of widget a dashboard item can be. */
export const WidgetTypes = {
    Analytic: "analytic",
    QuickAdd: "quickAdd",
    Filter: "filter",
    Entries: "entries",
    Header: "header",
    Divider: "divider",
    Note: "note",
    Container: "container",
    TabsContainer: "tabsContainer",
} as const;

/** The Config payload shared by WidgetTypes.Header, WidgetTypes.Note and (as its optional
    title) WidgetTypes.Container: all three are nothing but user-entered text. A
    WidgetTypes.Divider carries no config at all. */
export interface TextWidgetConfig {
    text: string;
}

/** Config is free-form JSON per widget type, so it only ever parses to the shape the widget
    itself expects, never trusted further than that. Shared by Header, Note and Container. */
export function parseTextWidgetConfig(
    config: string | undefined,
): TextWidgetConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return typeof parsed?.text === "string" ? parsed : null;
    } catch {
        return null;
    }
}

/** One tab of a WidgetTypes.TabsContainer. `id` is opaque; the child widgets in this tab
    carry it as their `parentTabId`. */
export interface TabDef {
    id: string;
    name: string;
}

/** The Config payload of a WidgetTypes.TabsContainer widget: the panel's optional title and
    its ordered tab set (there is always at least one). */
export interface TabsContainerConfig {
    title?: string;
    tabs: TabDef[];
}

/** Config is free-form JSON per widget type, same caveat as parseTextWidgetConfig. */
export function parseTabsContainerConfig(
    config: string | undefined,
): TabsContainerConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return Array.isArray(parsed?.tabs)
            ? { title: parsed.title ?? undefined, tabs: parsed.tabs }
            : null;
    } catch {
        return null;
    }
}

/** Sets a WidgetTypes.TabsContainer's title and full tab list. A tab sent with an existing
    `id` is renamed in place; one with no `id` is created; a tab left out is removed and its
    widgets move to the first remaining tab. */
export interface SaveTabsContainerDto {
    title?: string;
    tabs: { id?: string; name: string }[];
}

/** The Config payload of a WidgetTypes.QuickAdd widget: which tracker its button opens
    the quick-add entry dialog for. */
export interface QuickAddWidgetConfig {
    trackerId: string;
}

/** The tracker summary a QuickAdd widget's button is rendered from, resolved server-side
    so the card never has to fetch its own tracker just to show a name/color/icon. */
export interface QuickAddTrackerDto {
    id: string;
    name: string;
    color?: string;
    icon?: string;
}

/** A field-agnostic clause as the client sends it. The field it runs against is bound
    elsewhere (per clause in the View editor, per followed widget on a filter widget). */
export interface ClauseDto {
    kind: string;
    dataType: string;
    operator?: string | null;
    value?: string | null;
    descending?: boolean;
}

/** Names one followed Analytic/Entries widget + tracker a filter widget narrows, and which
    of that tracker's fields each clause runs against. `fieldByQuery` is keyed by clause: on
    the wire (`SaveFilterItemDto`) by the clause's index, and in the stored
    `FilterWidgetConfig` by the clause's `slotId`. */
export interface WidgetLink {
    itemId: string;
    trackerId: string;
    fieldByQuery: Record<string, string>;
}

/** One clause of a DashboardView as the client reads it back. queryId keys a filter
    widget's presetLinks fieldByQuery map. */
export interface DashboardViewClauseDto {
    queryId: string;
    kind: string;
    dataType: string;
    operator?: string | null;
    value?: string | null;
    descending: boolean;
}

/** A named clause set the board's filter widgets can offer as a preset. */
export interface DashboardViewDto {
    id: string;
    name: string;
    order: number;
    clauses: DashboardViewClauseDto[];
}

/** Creates or replaces a DashboardView. */
export interface SaveDashboardViewDto {
    name: string;
    clauses: ClauseDto[];
}

/** Reorders the board's DashboardViews. */
export interface ReorderDashboardViewsDto {
    dashboardViewIds: string[];
}

/** One preset a WidgetTypes.Filter widget offers -- a DashboardView on the same board whose
    clause shape matches the widget's, resolved to its id, name and value per clause (in the
    widget's own clause order) so picking it just fills the value inputs. */
export interface FilterPresetOptionDto {
    id: string;
    name: string;
    values: (string | null)[];
}

/** One clause of a filter widget: a stable widget-local id and the pooled query it
    currently resolves to. Two slots may share a `queryId` (two same-shape clauses mapped to
    different fields). */
export interface FilterClauseSlot {
    slotId: string;
    queryId: string;
}

/** The Config payload of a WidgetTypes.Filter widget: an ordered clause set (`slots`), the
    current value per clause (keyed by `slotId`), the followed widgets in `links`, and
    `presetIds` -- the board's DashboardViews it offers as presets, each a named value set
    whose clause shape matches these clauses exactly. */
export interface FilterWidgetConfig {
    slots: FilterClauseSlot[];
    valueBySlot: Record<string, string | null>;
    links: WidgetLink[];
    presetIds: string[];
}

/** Config is free-form JSON per widget type, same caveat as parseTextWidgetConfig. Shared
    by the filter widget's own edit dialog and anything that appends a follower link to it
    without going through that dialog. Pre-slot configs stored a parallel `queryIds` list
    with `valueByQuery` / `fieldByQuery` keyed by pooled query id; those are folded into
    slots here (mirroring the backend), a clause with a unique shape keeping its query id as
    its slot id. */
export function parseFilterWidgetConfig(config: string | undefined): FilterWidgetConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);

        if (Array.isArray(parsed?.slots)) {
            return {
                slots: parsed.slots,
                valueBySlot: parsed.valueBySlot ?? {},
                links: parsed.links ?? [],
                presetIds: parsed.presetIds ?? [],
            };
        }

        if (!Array.isArray(parsed?.queryIds)) return null;

        const queryIds: string[] = parsed.queryIds;
        const duplicated = new Set(
            queryIds.filter((id, i) => queryIds.indexOf(id) !== i),
        );
        const slots: FilterClauseSlot[] = queryIds.map((queryId, i) => ({
            slotId: duplicated.has(queryId) ? `${queryId}~${i}` : queryId,
            queryId,
        }));
        const slotByQuery = new Map<string, string>();
        for (const s of slots) if (!slotByQuery.has(s.queryId)) slotByQuery.set(s.queryId, s.slotId);
        const remap = (map: Record<string, string> | undefined) =>
            Object.fromEntries(
                Object.entries(map ?? {})
                    .filter(([q]) => slotByQuery.has(q))
                    .map(([q, v]) => [slotByQuery.get(q)!, v]),
            );

        return {
            slots,
            valueBySlot: remap(parsed.valueByQuery),
            links: (parsed.links ?? []).map((l: WidgetLink) => ({
                itemId: l.itemId,
                trackerId: l.trackerId,
                fieldByQuery: remap(l.fieldByQuery),
            })),
            presetIds: parsed.presetIds ?? [],
        };
    } catch {
        return null;
    }
}

/** One clause of a filter widget's own typed clause set, resolved server-side for the
    card to render an input for. slotId keys the widget's valueBySlot map. */
export interface FilterClauseDto {
    slotId: string;
    kind: string;
    dataType: string;
    operator?: string | null;
    value?: string | null;
}

/** What a WidgetTypes.Filter widget's card needs, resolved server-side the same way
    quickAddTracker is: its filter clauses with their current values, and the matching-shape
    DashboardViews it offers as presets (each with its value per clause). */
export interface FilterWidgetDto {
    clauses: FilterClauseDto[];
    presets: FilterPresetOptionDto[];
}

/** Adds or edits a WidgetTypes.Filter item. `clauses` are all filters, never sorts, and are
    required; each carries the value it starts out filtering on. A `links` entry's
    fieldByQuery is keyed by the clause's index in `clauses` — the client has no stable
    clause id until the save resolves one — and the backend rewrites those keys to the
    per-clause slot ids it stores. `presetIds` names the board's DashboardViews this widget
    offers as presets; each must be a view whose filter-clause shape matches `clauses`
    exactly. */
export interface SaveFilterItemDto {
    clauses: ClauseDto[];
    links: WidgetLink[];
    presetIds: string[];
}

/** Changes the values a WidgetTypes.Filter item's own typed clauses are currently set to,
    keyed by each clause's slotId. */
export interface SetFilterValuesDto {
    values: Record<string, string | null>;
}

/** What a WidgetTypes.Entries widget's table needs, resolved server-side: the tracker it
    reads from, the columns to show in order (Config's columnFieldIds, or every field when it
    names none), and the rows themselves — already filtered/sorted by the filter widgets
    this placement follows and capped to the most recent handful. The card renders these
    directly rather than fetching its own. */
export interface EntriesWidgetDto {
    trackerId: string;
    trackerName: string;
    color?: string;
    icon?: string;
    columns: FieldDto[];
    entries: EntryDto[];
}

/** Placement on one of the dashboard's grids, in that grid's columns. */
export interface WidgetLayoutDto {
    x: number;
    y: number;
    w: number;
    h: number;
}

/** How an Analytic/Entries widget is drawn on one of the board's two grids. Set
    independently per grid, so a chart can be shown in full on desktop and collapsed (or
    dropped) on mobile. Serialized as its numeric value. */
export enum DashboardItemDisplayMode {
    /** Drawn inline at the size it was given on the grid. */
    Full = 0,
    /** Drawn as a small button that opens the widget at full size in a modal. */
    Expandable = 1,
    /** Not drawn on this grid at all. Reachable from the board's hidden-widgets list. */
    Hidden = 2,
}

export interface DashboardWidgetLayoutDto extends WidgetLayoutDto {
    displayMode: DashboardItemDisplayMode;
}

export const LayoutVariants = {
    Desktop: "desktop",
    Mobile: "mobile",
} as const;

export type LayoutVariant = (typeof LayoutVariants)[keyof typeof LayoutVariants];

export interface DashboardWidgetDto {
    id: string;
    type: string;
    /** The WidgetTypes.Container item this one sits inside on the wide grid, or absent
        when it sits on the board itself. Always absent on the narrow grid, which flattens
        containers away. */
    parentItemId?: string;
    /** When the parent is a WidgetTypes.TabsContainer, which of its tabs this widget sits
        in. Only the active tab's widgets are drawn. Absent otherwise. */
    parentTabId?: string;
    layout: DashboardWidgetLayoutDto;
    mobileLayout: DashboardWidgetLayoutDto;
    config?: string;
    analytic?: AnalyticDto;
    quickAddTracker?: QuickAddTrackerDto;
    filter?: FilterWidgetDto;
    entriesWidget?: EntriesWidgetDto;
    trackerColor?: string;
    /** This placement's color override (EditWidgetModal), beating trackerColor and the
        dashboard's own color. Only ever set for a single-tracker widget. */
    color?: string;
}

export interface DashboardLayoutItemDto extends WidgetLayoutDto {
    itemId: string;
    /** The container this placement is inside, or null for a spot on the board itself.
        Only sent for the wide grid. */
    parentItemId?: string | null;
    /** When parentItemId is a tabs container, which of its tabs. Ignored otherwise. */
    parentTabId?: string | null;
}

export interface UpdateDashboardLayoutDto {
    variant: LayoutVariant;
    items: DashboardLayoutItemDto[];
}

export interface DashboardDto {
    id: string;
    name: string;
    color?: string;
    icon?: string;
    items: DashboardItemDto[];
}

export interface DashboardItemSourceFieldDto {
    purpose: string;
    fieldId: string;
    fieldName: string;
}

export interface DashboardItemSourceDto {
    id: string;
    name: string;
    fields: DashboardItemSourceFieldDto[];
    trackerId: string;
    trackerName: string;
    /** The fixed tracker view this source reads through, if any. A filter widget on the
        board can layer further clauses on top of it. */
    viewId?: string;
    label?: string;
    order: number;
}

/** One row of a goal placement's conditional targets. `conditions` maps a followed filter
    clause's pooled query id to the value it must currently be set to on the board for this
    row to apply; a clause not mentioned is a wildcard. Rows are evaluated in order, first
    full match wins, and its `target` replaces the widget's default. */
export interface GoalConditionalTargetDto {
    conditions: Record<string, string>;
    target: string;
}

export interface DashboardItemDto {
    id: string;
    order: number;
    type: string;
    parentItemId?: string;
    /** When the parent is a tabs container, which of its tabs this item sits in. */
    parentTabId?: string;
    layout: DashboardWidgetLayoutDto;
    mobileLayout: DashboardWidgetLayoutDto;
    config?: string;
    name: string;
    trackerIds: string[];
    resultType: string;
    code: string;
    /** Line/Bar only: how the axis field is bucketed before the code aggregates it. */
    grouping?: string;
    matchedValuesOnly: boolean;
    /** Line chart widgets only: whether the Y axis starts at zero or is fitted to the
        data's own range. */
    yAxisFromZero: boolean;
    /** Goal widgets only: the placement's conditional targets, in order. */
    goalConditionalTargets: GoalConditionalTargetDto[];
    /** The color override currently set on this placement, or absent for "Auto". */
    color?: string;
    /** SingleValue/Goal widgets only: whether the trend sparkline is shown when this
        placement follows a date-bounded filter clause. */
    showTrend: boolean;
    /** Goal widgets only: the shared widget's direction (see GoalDirections). */
    goalDirection?: string;
    sources: DashboardItemSourceDto[];
}

export interface CreateDashboardDto {
    name: string;
    color?: string;
    icon?: string;
}

export interface UpdateDashboardDto {
    name: string;
    color?: string;
    icon?: string;
}

export interface CreateAndPlaceWidgetSourceDto {
    trackerId: string;
    analyticFields: CreateAnalyticFieldDto[];
    /** The fixed tracker view this placement's entries read through, if any. */
    viewId?: string | null;
    label?: string;
}

export interface CreateAndPlaceWidgetDto {
    name?: string;
    description?: string;
    resultType: string;
    code: string;
    /** Line/Bar only: how the axis field is bucketed before the code aggregates it. */
    grouping?: string;
    matchedValuesOnly?: boolean;
    /** Goal widgets only, and required for them: a number or an hh:mm:ss duration. */
    goalTarget?: string;
    /** Goal widgets only: a GoalDirection value. Left unset behaves as HigherIsBetter. */
    goalDirection?: string;
    displayMode?: DashboardItemDisplayMode;
    mobileDisplayMode?: DashboardItemDisplayMode;
    /** Line charts only; defaults to true (0-anchored) server-side when omitted. */
    yAxisFromZero?: boolean;
    /** Left unset behaves as "Auto". Ignored server-side for a combined (multi-source) widget. */
    color?: string;
    /** SingleValue/Goal widgets only; defaults to true server-side when omitted. */
    showTrend?: boolean;
    sources: CreateAndPlaceWidgetSourceDto[];
}

export interface PlaceWidgetSourceOverrideDto {
    widgetSourceId: string;
    label?: string;
    viewId?: string | null;
}

export interface PlaceWidgetDto {
    widgetId: string;
    displayMode?: DashboardItemDisplayMode;
    mobileDisplayMode?: DashboardItemDisplayMode;
    /** Line charts only; defaults to true (0-anchored) server-side when omitted. */
    yAxisFromZero?: boolean;
    /** Left unset behaves as "Auto". Ignored server-side for a combined (multi-source) widget. */
    color?: string;
    /** SingleValue/Goal widgets only; defaults to true server-side when omitted. */
    showTrend?: boolean;
    sourceOverrides: PlaceWidgetSourceOverrideDto[];
}

/** Adds a WidgetTypes.QuickAdd widget: a button that opens a tracker's quick-add entry
    dialog from the board. */
export interface AddDashboardQuickAddItemDto {
    trackerId: string;
}

export interface CreateAndPlaceEntriesWidgetDto {
    trackerId: string;
    name?: string;
    /** Tracker fields to show as columns, in order. Empty/omitted shows every field. */
    columnFieldIds?: string[];
    displayMode?: DashboardItemDisplayMode;
    mobileDisplayMode?: DashboardItemDisplayMode;
}

export interface PlaceEntriesWidgetDto {
    entriesWidgetId: string;
    /** Tracker fields to show as columns, in order. Empty/omitted shows every field. */
    columnFieldIds?: string[];
    displayMode?: DashboardItemDisplayMode;
    mobileDisplayMode?: DashboardItemDisplayMode;
}

/** Adds a WidgetTypes.Header widget: a short line of text read as a section title. */
export interface AddDashboardHeaderItemDto {
    text: string;
}

/** Adds a WidgetTypes.Note widget: a free-form block of text. */
export interface AddDashboardNoteItemDto {
    text: string;
}

export interface UpdateDashboardItemSourceDto {
    sourceId: string;
    label?: string | null;
    viewId?: string | null;
}

export interface UpdateDashboardItemDto {
    displayMode: DashboardItemDisplayMode;
    mobileDisplayMode: DashboardItemDisplayMode;
    /** Line charts only: whether the Y axis starts at zero or is fitted to the data range. */
    yAxisFromZero: boolean;
    /** Goal widgets only: the whole conditional-target list. An empty list clears them. */
    goalConditionalTargets: GoalConditionalTargetDto[];
    /** Overrides this placement's color. Null clears it back to "Auto". Ignored server-side
        for a combined (multi-source) widget. */
    color?: string | null;
    /** SingleValue/Goal widgets only: whether the trend sparkline is shown. */
    showTrend: boolean;
    sources: UpdateDashboardItemSourceDto[];
}

export interface UpdateDashboardEntriesItemDto {
    /** Tracker fields to show as columns, in order. Empty/omitted shows every field. */
    columnFieldIds?: string[];
    displayMode: DashboardItemDisplayMode;
    mobileDisplayMode: DashboardItemDisplayMode;
}

/** Changes what a WidgetTypes.Header or WidgetTypes.Note widget's text reads. */
export interface SetTextWidgetContentDto {
    text: string;
}
