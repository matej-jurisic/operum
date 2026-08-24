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
    /** The single analytic definition every source below is calculated with. */
    resultType: string;
    code: string;
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

export interface AddDashboardItemDto {
    resultType: string;
    code: string;
    sources: AddDashboardItemSourceDto[];
}
