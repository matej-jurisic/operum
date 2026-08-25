import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import {
    AddDashboardItemDto,
    AddDashboardItemFromAnalyticDto,
    AddDashboardQuickAddItemDto,
    CreateDashboardDto,
    DashboardDto,
    DashboardItemDto,
    DashboardWidgetDto,
    UpdateDashboardDto,
    UpdateDashboardLayoutDto,
} from "../types/DashboardDto";

export const dashboardController = {
    getDashboards: async (): Promise<ApiResponse<DashboardDto[]>> => {
        return await api.get("/dashboard");
    },

    getDashboard: async (dashboardId: string): Promise<ApiResponse<DashboardDto>> => {
        return await api.get(`/dashboard/${dashboardId}`);
    },

    getDashboardWidgets: async (
        dashboardId: string
    ): Promise<ApiResponse<DashboardWidgetDto[]>> => {
        return await api.get(`/dashboard/${dashboardId}/widgets`);
    },

    createDashboard: async (dto: CreateDashboardDto): Promise<ApiResponse<DashboardDto>> => {
        return await api.post("/dashboard", dto);
    },

    updateDashboard: async (
        dashboardId: string,
        dto: UpdateDashboardDto
    ): Promise<ApiResponse<DashboardDto>> => {
        return await api.put(`/dashboard/${dashboardId}`, dto);
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

    addDashboardItemFromAnalytic: async (
        dashboardId: string,
        dto: AddDashboardItemFromAnalyticDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/from-analytic`, dto);
    },

    addQuickAddItem: async (
        dashboardId: string,
        dto: AddDashboardQuickAddItemDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/quick-add`, dto);
    },

    removeDashboardItem: async (
        dashboardId: string,
        itemId: string
    ): Promise<ApiResponse> => {
        return await api.delete(`/dashboard/${dashboardId}/items/${itemId}`);
    },

    updateDashboardLayout: async (
        dashboardId: string,
        dto: UpdateDashboardLayoutDto
    ): Promise<ApiResponse> => {
        return await api.put(`/dashboard/${dashboardId}/layout`, dto);
    },
};
