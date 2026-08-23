import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";

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
    /** Null when the source carries its own ad hoc, dashboard-only definition. */
    analyticId?: string | null;
    analyticName: string;
    resultType: string;
    code: string;
    isAdHoc: boolean;
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
 * A source is defined one of two ways, never both: `analyticId` reuses an analytic
 * saved on the tracker, while `resultType` + `code` + `analyticFields` define one
 * inline that only ever exists on this dashboard.
 */
export interface AddDashboardItemSourceDto {
    trackerId: string;
    analyticId?: string;
    resultType?: string;
    code?: string;
    analyticFields?: CreateAnalyticFieldDto[];
    viewIds: string[];
    label?: string;
}

export interface AddDashboardItemDto {
    sources: AddDashboardItemSourceDto[];
}

export interface AnalyticSummaryDto {
    id: string;
    name: string;
    resultType: string;
    code: string;
}
