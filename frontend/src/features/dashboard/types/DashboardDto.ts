import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";
import { FieldDto } from "../../fields/types/FieldDto";

/** The kinds of widget a dashboard item can be. */
export const WidgetTypes = {
    Analytic: "analytic",
    QuickAdd: "quickAdd",
    View: "view",
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

/** One view a View widget's dropdown can be set to. */
export interface ViewOptionDto {
    id: string;
    name: string;
}

/** What a WidgetTypes.View widget's dropdown needs, resolved server-side the same way
    quickAddTracker is. viewId is the persisted, current selection — it changes (and is
    saved) whenever the dropdown does, so it's what every source linked to this widget is
    filtered by, not just an initial default. */
export interface ViewWidgetDto {
    trackerId: string;
    trackerName: string;
    color?: string;
    icon?: string;
    viewId?: string | null;
    views: ViewOptionDto[];
}

/** What a WidgetTypes.Entries widget's table needs, resolved server-side the same way
    viewWidget is: the tracker it reads from, the view it's currently filtered by (fixed,
    or followed live from a View widget the same way a source's linkedViewWidgetId is),
    and the columns that view wants shown, in its order. A view naming none shows every
    field. */
export interface EntriesWidgetDto {
    trackerId: string;
    trackerName: string;
    color?: string;
    icon?: string;
    viewId?: string | null;
    columns: FieldDto[];
}

/** Placement on one of the dashboard's grids, in that grid's columns. */
export interface WidgetLayoutDto {
    x: number;
    y: number;
    w: number;
    h: number;
}

/** A widget's placement as the board reads it back: WidgetLayoutDto plus whether this grid
    draws it as a small button that opens the real thing in a modal instead of inline
    (Analytic/Entries widgets only). Kept apart from WidgetLayoutDto itself because the
    drag/resize save path (DashboardLayoutItemDto below) only ever writes x/y/w/h back —
    expandable is set from the widget's own create/edit form, never from arranging the
    board. */
export interface DashboardWidgetLayoutDto extends WidgetLayoutDto {
    expandable: boolean;
}

/**
 * Which of a board's two grids an arrangement belongs to. A placement means nothing
 * without the column count it was made in, so the wide grid and the narrow one a phone
 * renders are stored — and saved — separately.
 */
export const LayoutVariants = {
    Desktop: "desktop",
    Mobile: "mobile",
} as const;

export type LayoutVariant = (typeof LayoutVariants)[keyof typeof LayoutVariants];

/**
 * One item of a dashboard as it is rendered: where it sits on the grid, what kind of
 * widget it is, and the payload that kind needs. An analytic widget carries the chart
 * calculated for it; a future kind carries its own config instead.
 */
export interface DashboardWidgetDto {
    id: string;
    type: string;
    /** Placement on the wide grid, in DASHBOARD_GRID_COLUMNS columns. */
    layout: DashboardWidgetLayoutDto;
    /** Placement on the narrow grid, in DASHBOARD_MOBILE_GRID_COLUMNS columns. */
    mobileLayout: DashboardWidgetLayoutDto;
    config?: string;
    analytic?: AnalyticDto;
    quickAddTracker?: QuickAddTrackerDto;
    viewWidget?: ViewWidgetDto;
    entriesWidget?: EntriesWidgetDto;
    /** The color of the single tracker every source of this widget reads from. Undefined
        when the widget has no single owning tracker (a combined chart spanning more than
        one), so the board falls back to its own color. */
    trackerColor?: string;
}

export interface DashboardLayoutItemDto extends WidgetLayoutDto {
    itemId: string;
}

export interface UpdateDashboardLayoutDto {
    /** Which grid these placements were made on. Only that grid is written. */
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
    /** The item's definition read through this source's fields. */
    name: string;
    fields: DashboardItemSourceFieldDto[];
    trackerId: string;
    trackerName: string;
    /** How the source is filtered: a fixed view of its own tracker, or the View widget on
        the board whose selection it follows. Never both. */
    viewId?: string;
    linkedViewWidgetId?: string;
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
    /** What this widget is called on the board (Analytic/Entries widgets); empty for the
        kinds that have no name. Lets a form label this item without a second fetch. */
    name: string;
    /** Every tracker this widget reads from: one for an Entries widget, one or more for an
        Analytic widget, empty for the rest. */
    trackerIds: string[];
    /** The single analytic definition every source below is calculated with. */
    resultType: string;
    code: string;
    /** Combined charts only: whether the chart is restricted to x-axis values shared by every source. */
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

/**
 * The tracker-specific half of a new widget's source: which tracker to read entries from
 * and which of its fields fill the purposes the widget's result type + code require, plus
 * this placement's own filter/label -- the same fields AddDashboardItemSourceDto used to
 * carry, just renamed to make clear they define AND place in one call (see
 * CreateAndPlaceWidgetDto).
 */
export interface CreateAndPlaceWidgetSourceDto {
    trackerId: string;
    analyticFields: CreateAnalyticFieldDto[];
    /** At most one of these narrows the source's entries: viewId fixes it,
        linkedViewWidgetId instead follows a WidgetTypes.View item already on the board, so
        the filter changes live with its dropdown. */
    viewId?: string | null;
    linkedViewWidgetId?: string | null;
    label?: string;
}

/**
 * Defines a new Widget Library chart and places it on this board in one call -- the
 * single-round-trip convenience CustomAnalyticForm relies on. Splits server-side into a
 * Widget (the definition) plus a placement, so the widget this creates is exactly as
 * reusable afterwards as one built from the Library directly.
 */
export interface CreateAndPlaceWidgetDto {
    /** Optional: left unset, the widget falls back to its definition's own label. */
    name?: string;
    description?: string;
    resultType: string;
    code: string;
    /** Combined charts only: keep just the x-axis values every source has a point for. */
    matchedValuesOnly?: boolean;
    /** Whether the widget draws as a small button that opens the chart in a modal instead
        of inline, independently on each of the board's two grids. */
    expandable?: boolean;
    mobileExpandable?: boolean;
    sources: CreateAndPlaceWidgetSourceDto[];
}

/** One WidgetSource's placement-only settings when placing an existing Widget Library
    chart: how this board filters and labels it. */
export interface PlaceWidgetSourceOverrideDto {
    widgetSourceId: string;
    label?: string;
    viewId?: string | null;
    linkedViewWidgetId?: string | null;
}

/**
 * Places an existing Widget Library chart onto this dashboard by reference: unlike the
 * old copy-on-add, nothing is duplicated. Editing the widget afterwards -- in the Library,
 * or from any other dashboard placing it -- changes what this placement draws too.
 */
export interface PlaceWidgetDto {
    widgetId: string;
    expandable?: boolean;
    mobileExpandable?: boolean;
    /** A WidgetSource not named here is placed with no label or view override: the
        widget's own display name, unfiltered. */
    sourceOverrides: PlaceWidgetSourceOverrideDto[];
}

/** Adds a WidgetTypes.QuickAdd widget: a button that opens a tracker's quick-add entry
    dialog from the board. */
export interface AddDashboardQuickAddItemDto {
    trackerId: string;
}

/** Adds a WidgetTypes.View widget: a dropdown over one tracker's views that other
    widgets' sources can link to. */
export interface AddDashboardViewItemDto {
    trackerId: string;
    /** The dropdown's starting selection. Left unset to start on "All entries". */
    viewId?: string | null;
    /** Item ids of Analytic/Entries widgets already on the board that should follow this
        selector from the moment it's added — the sources reading from `trackerId` are
        pointed at it, so the user doesn't have to open each widget to link it. */
    linkedItemIds?: string[];
}

/** Edits a WidgetTypes.View item: its starting/current selection and the full set of board
    widgets that follow it. The payload stands for the whole set the same way
    UpdateDashboardItemDto does — a widget left out of `linkedItemIds` is unlinked from this
    selector (a fixed view, or a link to a different selector, is left alone). */
export interface UpdateDashboardViewItemDto {
    viewId?: string | null;
    linkedItemIds: string[];
}

/** Defines a new Widget Library Entries table and places it on this board in one call --
    the single-round-trip convenience EntriesWidgetForm relies on. See
    CreateAndPlaceWidgetDto for the equivalent on a chart. */
export interface CreateAndPlaceEntriesWidgetDto {
    trackerId: string;
    name?: string;
    viewId?: string | null;
    linkedViewWidgetId?: string | null;
    /** Whether the widget draws as a small button that opens the table in a modal instead
        of inline, independently on each of the board's two grids. */
    expandable?: boolean;
    mobileExpandable?: boolean;
}

/** Places an existing Widget Library Entries table onto this board by reference -- see
    PlaceWidgetDto for the equivalent on a chart. */
export interface PlaceEntriesWidgetDto {
    entriesWidgetId: string;
    viewId?: string | null;
    linkedViewWidgetId?: string | null;
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

/**
 * One source of an analytic widget as the board is allowed to change it after the fact:
 * what the series is called, and which view narrows the entries it reads.
 */
export interface UpdateDashboardItemSourceDto {
    sourceId: string;
    /** Cleared when left blank, so the widget falls back to the definition's own label. */
    label?: string | null;
    /** At most one of these — see CreateAndPlaceWidgetSourceDto. */
    viewId?: string | null;
    linkedViewWidgetId?: string | null;
}

/**
 * Edits an analytic widget in place. Only what belongs to the board is editable: the
 * result type, code and field mapping are the definition the widget was built from, and
 * changing those would make it a different chart rather than the one that was placed
 * here. The payload stands for the whole widget, so every source is named every time.
 */
export interface UpdateDashboardItemDto {
    /** Whether the widget draws as a small button that opens the chart in a modal instead
        of inline, independently on each of the board's two grids. */
    expandable: boolean;
    mobileExpandable: boolean;
    sources: UpdateDashboardItemSourceDto[];
}

/**
 * Edits an Entries widget in place: only how it's filtered, and whether it collapses to a
 * button on each grid. The tracker it reads from is fixed at add time — see
 * UpdateDashboardItemDto for the equivalent rule on Analytic widgets.
 */
export interface UpdateDashboardEntriesItemDto {
    viewId?: string | null;
    linkedViewWidgetId?: string | null;
    expandable: boolean;
    mobileExpandable: boolean;
}

/** Changes what a WidgetTypes.View item's dropdown is currently set to. Persisted on the
    item itself, so every source linked to it re-filters from here on — not just this
    browser session. */
export interface SetViewWidgetSelectionDto {
    viewId?: string | null;
}

/** Changes what a WidgetTypes.Header or WidgetTypes.Note widget's text reads. Persisted the
    same way SetViewWidgetSelectionDto is. */
export interface SetTextWidgetContentDto {
    text: string;
}
