import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import {
    AddDashboardHeaderItemDto,
    AddDashboardNoteItemDto,
    AddDashboardQuickAddItemDto,
    AddDashboardViewItemDto,
    CreateAndPlaceEntriesWidgetDto,
    CreateAndPlaceWidgetDto,
    CreateDashboardDto,
    DashboardDto,
    DashboardItemDto,
    DashboardWidgetDto,
    PlaceEntriesWidgetDto,
    PlaceWidgetDto,
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

    createAndPlaceWidget: async (
        dashboardId: string,
        dto: CreateAndPlaceWidgetDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items`, dto);
    },

    placeWidget: async (
        dashboardId: string,
        dto: PlaceWidgetDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/place-widget`, dto);
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

    createAndPlaceEntriesWidget: async (
        dashboardId: string,
        dto: CreateAndPlaceEntriesWidgetDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/entries`, dto);
    },

    placeEntriesWidget: async (
        dashboardId: string,
        dto: PlaceEntriesWidgetDto
    ): Promise<ApiResponse<DashboardItemDto>> => {
        return await api.post(`/dashboard/${dashboardId}/items/place-entries-widget`, dto);
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
