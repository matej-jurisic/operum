import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";

/** The kinds of widget a dashboard item can be. */
export const WidgetTypes = {
    Analytic: "analytic",
    QuickAdd: "quickAdd",
    View: "view",
} as const;

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
    viewId?: string | null;
    views: ViewOptionDto[];
}

/** Placement on one of the dashboard's grids, in that grid's columns. */
export interface WidgetLayoutDto {
    x: number;
    y: number;
    w: number;
    h: number;
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
    layout: WidgetLayoutDto;
    /** Placement on the narrow grid, in DASHBOARD_MOBILE_GRID_COLUMNS columns. */
    mobileLayout: WidgetLayoutDto;
    config?: string;
    analytic?: AnalyticDto;
    quickAddTracker?: QuickAddTrackerDto;
    viewWidget?: ViewWidgetDto;
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
    viewId?: string;
    label?: string;
    order: number;
}

export interface DashboardItemDto {
    id: string;
    order: number;
    type: string;
    layout: WidgetLayoutDto;
    mobileLayout: WidgetLayoutDto;
    config?: string;
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
 * The tracker-specific half of an item: which tracker to read entries from and which
 * of its fields fill the purposes the item's result type + code require.
 */
export interface AddDashboardItemSourceDto {
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
 * Adds a widget by copying a tracker's own analytic instead of defining one inline. The
 * copy is taken at add time, so editing the tracker's analytic afterwards leaves the
 * board as it was.
 */
export interface AddDashboardItemFromAnalyticDto {
    analyticId: string;
    /** Optional: a tracker analytic carries no view of its own, so the board picks one. At
        most one of these — see AddDashboardItemSourceDto. */
    viewId?: string | null;
    linkedViewWidgetId?: string | null;
}

export interface AddDashboardItemDto {
    resultType: string;
    code: string;
    /** Combined charts only: keep just the x-axis values every source has a point for. */
    matchedValuesOnly?: boolean;
    sources: AddDashboardItemSourceDto[];
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
}

/** Changes what a WidgetTypes.View item's dropdown is currently set to. Persisted on the
    item itself, so every source linked to it re-filters from here on — not just this
    browser session. */
export interface SetViewWidgetSelectionDto {
    viewId?: string | null;
}
