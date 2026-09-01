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
} as const;

/** The Config payload shared by WidgetTypes.Header and WidgetTypes.Note: both are nothing
    but user-entered text. WidgetTypes.Divider carries no config at all. */
export interface TextWidgetConfig {
    text: string;
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

/** Names one followed Analytic/Entries widget + tracker a filter widget narrows, and
    which of that tracker's fields each clause runs against, keyed by the clause's pooled
    query id. Shared by both of a filter widget's independent link lists -- `links` (its
    own typed clauses) and `presetLinks` (whichever preset is selected). */
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

/** One preset a WidgetTypes.Filter widget's dropdown can be set to -- a DashboardView on
    the same board, resolved to just its id and name for the card to render. */
export interface FilterPresetOptionDto {
    id: string;
    name: string;
}

/** The Config payload of a WidgetTypes.Filter widget. Two independent facets:
    - Own typed clauses: an ordered clause set (pooled query ids), the current value per
      clause (keyed by the pooled query id), and the followed widgets in `links`.
    - Presets: the board's DashboardViews it offers as quick-apply presets, the current
      selection, and the followed widgets in `presetLinks` -- functionally what the old
      standalone view selector widget was, folded in here as a second facet. */
export interface FilterWidgetConfig {
    queryIds: string[];
    valueByQuery: Record<string, string | null>;
    links: WidgetLink[];

    presetIds: string[];
    selectedPresetId?: string | null;
    presetLinks: WidgetLink[];
}

/** One clause of a filter widget's own typed clause set, resolved server-side for the
    card to render an input for. queryId keys the widget's valueByQuery map. */
export interface FilterClauseDto {
    queryId: string;
    kind: string;
    dataType: string;
    operator?: string | null;
    value?: string | null;
}

/** What a WidgetTypes.Filter widget's card needs, resolved server-side the same way
    quickAddTracker is: its own typed clauses with their current values, and the DashboardViews
    it offers as presets plus which one is currently selected. */
export interface FilterWidgetDto {
    clauses: FilterClauseDto[];
    presets: FilterPresetOptionDto[];
    selectedPresetId?: string | null;
}

/** Adds or edits a WidgetTypes.Filter item. Two independent facets, at least one of which
    must be present:
    - Own clauses: `clauses` are all filters, never sorts; each carries the value it starts
      out filtering on. A `links` entry's fieldByQuery is keyed by the clause's index in
      `clauses` — the client has no pooled query id until the save resolves one — and the
      backend rewrites those keys to the ids it stores.
    - Presets: `presetIds` names the board's DashboardViews this widget offers, `selectedPresetId`
      the starting selection, and `presetLinks` the followed widgets for whichever preset is
      applied, keyed directly by pooled DashboardViewQuery ids (no index rewrite needed). */
export interface SaveFilterItemDto {
    clauses: ClauseDto[];
    links: WidgetLink[];

    presetIds: string[];
    selectedPresetId?: string | null;
    presetLinks: WidgetLink[];
}

/** Changes the values a WidgetTypes.Filter item's own typed clauses are currently set
    to. */
export interface SetFilterValuesDto {
    values: Record<string, string | null>;
}

/** Changes what a WidgetTypes.Filter item's preset dropdown is currently set to. */
export interface SetFilterPresetDto {
    selectedPresetId?: string | null;
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

export interface DashboardWidgetLayoutDto extends WidgetLayoutDto {
    expandable: boolean;
}

export const LayoutVariants = {
    Desktop: "desktop",
    Mobile: "mobile",
} as const;

export type LayoutVariant = (typeof LayoutVariants)[keyof typeof LayoutVariants];

export interface DashboardWidgetDto {
    id: string;
    type: string;
    layout: DashboardWidgetLayoutDto;
    mobileLayout: DashboardWidgetLayoutDto;
    config?: string;
    analytic?: AnalyticDto;
    quickAddTracker?: QuickAddTrackerDto;
    filter?: FilterWidgetDto;
    entriesWidget?: EntriesWidgetDto;
    trackerColor?: string;
}

export interface DashboardLayoutItemDto extends WidgetLayoutDto {
    itemId: string;
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

export interface DashboardItemDto {
    id: string;
    order: number;
    type: string;
    layout: DashboardWidgetLayoutDto;
    mobileLayout: DashboardWidgetLayoutDto;
    config?: string;
    name: string;
    trackerIds: string[];
    resultType: string;
    code: string;
    matchedValuesOnly: boolean;
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
    matchedValuesOnly?: boolean;
    expandable?: boolean;
    mobileExpandable?: boolean;
    sources: CreateAndPlaceWidgetSourceDto[];
}

export interface PlaceWidgetSourceOverrideDto {
    widgetSourceId: string;
    label?: string;
    viewId?: string | null;
}

export interface PlaceWidgetDto {
    widgetId: string;
    expandable?: boolean;
    mobileExpandable?: boolean;
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
    expandable?: boolean;
    mobileExpandable?: boolean;
}

export interface PlaceEntriesWidgetDto {
    entriesWidgetId: string;
    /** Tracker fields to show as columns, in order. Empty/omitted shows every field. */
    columnFieldIds?: string[];
    expandable?: boolean;
    mobileExpandable?: boolean;
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
    expandable: boolean;
    mobileExpandable: boolean;
    sources: UpdateDashboardItemSourceDto[];
}

export interface UpdateDashboardEntriesItemDto {
    /** Tracker fields to show as columns, in order. Empty/omitted shows every field. */
    columnFieldIds?: string[];
    expandable: boolean;
    mobileExpandable: boolean;
}

/** Changes what a WidgetTypes.Header or WidgetTypes.Note widget's text reads. */
export interface SetTextWidgetContentDto {
    text: string;
}
