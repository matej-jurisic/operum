import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";
import { EntryDto } from "../../entries/types/EntryDto";
import { FieldDto } from "../../fields/types/FieldDto";

/** The kinds of widget a dashboard item can be. */
export const WidgetTypes = {
    Analytic: "analytic",
    QuickAdd: "quickAdd",
    ViewSelector: "viewSelector",
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
    elsewhere (per clause in the View editor, per followed widget on a view selector). */
export interface ClauseDto {
    kind: string;
    dataType: string;
    operator?: string | null;
    value?: string | null;
    descending?: boolean;
}

/** One option a view selector's dropdown can be set to. */
export interface ViewSelectorOptionDto {
    id: string;
    name: string;
}

/** What a WidgetTypes.ViewSelector widget's dropdown needs, resolved server-side the same
    way quickAddTracker is: the DashboardViews it offers and the current selection. */
export interface ViewSelectorWidgetDto {
    options: ViewSelectorOptionDto[];
    selectedId?: string | null;
}

/** Per followed Analytic widget + tracker, which of that tracker's fields each clause runs
    against, keyed by the pooled query id. */
export interface ViewSelectorLink {
    itemId: string;
    trackerId: string;
    fieldByQuery: Record<string, string>;
}

/** The Config payload of a WidgetTypes.ViewSelector widget. */
export interface ViewSelectorWidgetConfig {
    optionIds: string[];
    selectedId?: string | null;
    links: ViewSelectorLink[];
}

/** One clause of a DashboardView as the client reads it back. queryId keys a view
    selector's fieldByQuery map. */
export interface DashboardViewClauseDto {
    queryId: string;
    kind: string;
    dataType: string;
    operator?: string | null;
    value?: string | null;
    descending: boolean;
}

/** A named clause set the board's view selectors can offer. */
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

/** Adds or edits a WidgetTypes.ViewSelector item. */
export interface SaveViewSelectorItemDto {
    optionIds: string[];
    selectedId?: string | null;
    links: ViewSelectorLink[];
}

/** Changes what a WidgetTypes.ViewSelector item's dropdown is currently set to. */
export interface SetViewSelectorSelectionDto {
    selectedId?: string | null;
}

/** What a WidgetTypes.Entries widget's table needs, resolved server-side: the tracker it
    reads from, the columns to show in order (Config's columnFieldIds, or every field when it
    names none), and the rows themselves — already filtered/sorted by the view selectors this
    placement follows and capped to the most recent handful. The card renders these directly
    rather than fetching its own. */
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
    viewSelector?: ViewSelectorWidgetDto;
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
    /** The fixed tracker view this source reads through, if any. A view selector on the
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
