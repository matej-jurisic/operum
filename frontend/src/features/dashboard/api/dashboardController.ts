import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import {
    AddDashboardItemDto,
    CreateDashboardDto,
    DashboardDto,
    DashboardItemDto,
} from "../types/DashboardDto";

export const dashboardController = {
    getDashboards: async (): Promise<ApiResponse<DashboardDto[]>> => {
        return await api.get("/dashboard");
    },

    getDashboard: async (dashboardId: string): Promise<ApiResponse<DashboardDto>> => {
        return await api.get(`/dashboard/${dashboardId}`);
    },

    getDashboardAnalytics: async (dashboardId: string): Promise<ApiResponse<AnalyticDto[]>> => {
        return await api.get(`/dashboard/${dashboardId}/analytics`);
    },

    createDashboard: async (dto: CreateDashboardDto): Promise<ApiResponse<DashboardDto>> => {
        return await api.post("/dashboard", dto);
    },

    deleteDashboard: async (dashboardId: string): Promise<ApiResponse> => {
        return await api.delete(`/dashboard/${dashboardId}`);
    },

    addDashboardItem: async (
        dashboardId: string,
        dto: AddDashboardItemDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items`, dto);
    },

    removeDashboardItem: async (
        dashboardId: string,
        itemId: string
    ): Promise<ApiResponse> => {
        return await api.delete(`/dashboard/${dashboardId}/items/${itemId}`);
    },

    reorderDashboardItems: async (
        dashboardId: string,
        orderedItemIds: string[]
    ): Promise<ApiResponse> => {
        return await api.put(`/dashboard/${dashboardId}/items/reorder`, orderedItemIds);
    },
};
