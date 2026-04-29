export interface DashboardDto {
    id: string;
    name: string;
    color?: string;
    icon?: string;
    items: DashboardItemDto[];
}

export interface DashboardItemDto {
    id: string;
    analyticId: string;
    analyticName: string;
    trackerId: string;
    trackerName: string;
    viewIds: string[];
    order: number;
}

export interface CreateDashboardDto {
    name: string;
    color?: string;
    icon?: string;
}

export interface AddDashboardItemDto {
    analyticId: string;
    trackerId: string;
    viewIds: string[];
}

export interface AnalyticSummaryDto {
    id: string;
    name: string;
    resultType: string;
}
