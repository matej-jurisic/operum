import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";

/** The kinds of widget a dashboard item can be. */
export const WidgetTypes = {
    Analytic: "analytic",
} as const;

/** Placement on the dashboard grid, in DASHBOARD_GRID_COLUMNS columns. */
export interface WidgetLayoutDto {
    x: number;
    y: number;
    w: number;
    h: number;
}

/**
 * One item of a dashboard as it is rendered: where it sits on the grid, what kind of
 * widget it is, and the payload that kind needs. An analytic widget carries the chart
 * calculated for it; a future kind carries its own config instead.
 */
export interface DashboardWidgetDto {
    id: string;
    type: string;
    layout: WidgetLayoutDto;
    config?: string;
    analytic?: AnalyticDto;
}

export interface DashboardLayoutItemDto extends WidgetLayoutDto {
    itemId: string;
}

export interface UpdateDashboardLayoutDto {
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
    viewIds: string[];
    label?: string;
    order: number;
}

export interface DashboardItemDto {
    id: string;
    order: number;
    type: string;
    layout: WidgetLayoutDto;
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
    viewIds: string[];
    label?: string;
}

/**
 * Adds a widget by copying a tracker's own analytic instead of defining one inline. The
 * copy is taken at add time, so editing the tracker's analytic afterwards leaves the
 * board as it was.
 */
export interface AddDashboardItemFromAnalyticDto {
    analyticId: string;
    /** Optional: a tracker analytic carries no views of its own, so the board picks them. */
    viewIds?: string[];
}

export interface AddDashboardItemDto {
    resultType: string;
    code: string;
    /** Combined charts only: keep just the x-axis values every source has a point for. */
    matchedValuesOnly?: boolean;
    sources: AddDashboardItemSourceDto[];
}
