import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import {
    AddDashboardEntriesItemDto,
    AddDashboardHeaderItemDto,
    AddDashboardItemDto,
    AddDashboardItemFromAnalyticDto,
    AddDashboardNoteItemDto,
    AddDashboardQuickAddItemDto,
    AddDashboardViewItemDto,
    CreateDashboardDto,
    DashboardDto,
    DashboardItemDto,
    DashboardWidgetDto,
    SetTextWidgetContentDto,
    SetViewWidgetSelectionDto,
    UpdateDashboardDto,
    UpdateDashboardEntriesItemDto,
    UpdateDashboardItemDto,
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

    addViewItem: async (
        dashboardId: string,
        dto: AddDashboardViewItemDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/view`, dto);
    },

    addEntriesItem: async (
        dashboardId: string,
        dto: AddDashboardEntriesItemDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/entries`, dto);
    },

    addHeaderItem: async (
        dashboardId: string,
        dto: AddDashboardHeaderItemDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/header`, dto);
    },

    addDividerItem: async (
        dashboardId: string
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/divider`);
    },

    addNoteItem: async (
        dashboardId: string,
        dto: AddDashboardNoteItemDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/note`, dto);
    },

    updateDashboardItem: async (
        dashboardId: string,
        itemId: string,
        dto: UpdateDashboardItemDto
    ): Promise<ApiResponse<DashboardWidgetDto[]>> => {
        return await api.put(`/dashboard/${dashboardId}/items/${itemId}`, dto);
    },

    updateEntriesItem: async (
        dashboardId: string,
        itemId: string,
        dto: UpdateDashboardEntriesItemDto
    ): Promise<ApiResponse<DashboardWidgetDto[]>> => {
        return await api.put(`/dashboard/${dashboardId}/items/${itemId}/entries`, dto);
    },

    setViewWidgetSelection: async (
        dashboardId: string,
        itemId: string,
        dto: SetViewWidgetSelectionDto
    ): Promise<ApiResponse<DashboardWidgetDto[]>> => {
        return await api.put(`/dashboard/${dashboardId}/items/${itemId}/view-selection`, dto);
    },

    setTextWidgetContent: async (
        dashboardId: string,
        itemId: string,
        dto: SetTextWidgetContentDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.put(`/dashboard/${dashboardId}/items/${itemId}/text`, dto);
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
