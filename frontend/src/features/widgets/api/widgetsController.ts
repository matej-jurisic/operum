import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import {
    CreateEntriesWidgetDto,
    CreateWidgetDto,
    EntriesWidgetDefinitionDto,
    UpdateEntriesWidgetDto,
    UpdateWidgetDto,
    WidgetDto,
} from "../types/WidgetDto";

export const widgetsController = {
    getWidgets: async (trackerId?: string): Promise<ApiResponse<WidgetDto[]>> => {
        const qs = trackerId ? `?trackerId=${trackerId}` : "";
        return await api.get(`/widgets${qs}`);
    },

    getWidget: async (widgetId: string): Promise<ApiResponse<WidgetDto>> => {
        return await api.get(`/widgets/${widgetId}`);
    },

    createWidget: async (dto: CreateWidgetDto): Promise<ApiResponse<WidgetDto>> => {
        return await api.post("/widgets", dto);
    },

    updateWidget: async (
        widgetId: string,
        dto: UpdateWidgetDto
    ): Promise<ApiResponse<WidgetDto>> => {
        return await api.put(`/widgets/${widgetId}`, dto);
    },

    deleteWidget: async (widgetId: string): Promise<ApiResponse> => {
        return await api.delete(`/widgets/${widgetId}`);
    },

    getEntriesWidgets: async (
        trackerId?: string
    ): Promise<ApiResponse<EntriesWidgetDefinitionDto[]>> => {
        const qs = trackerId ? `?trackerId=${trackerId}` : "";
        return await api.get(`/widgets/entries${qs}`);
    },

    getEntriesWidget: async (
        entriesWidgetId: string
    ): Promise<ApiResponse<EntriesWidgetDefinitionDto>> => {
        return await api.get(`/widgets/entries/${entriesWidgetId}`);
    },

    createEntriesWidget: async (
        dto: CreateEntriesWidgetDto
    ): Promise<ApiResponse<EntriesWidgetDefinitionDto>> => {
        return await api.post("/widgets/entries", dto);
    },

    updateEntriesWidget: async (
        entriesWidgetId: string,
        dto: UpdateEntriesWidgetDto
    ): Promise<ApiResponse<EntriesWidgetDefinitionDto>> => {
        return await api.put(`/widgets/entries/${entriesWidgetId}`, dto);
    },

    deleteEntriesWidget: async (entriesWidgetId: string): Promise<ApiResponse> => {
        return await api.delete(`/widgets/entries/${entriesWidgetId}`);
    },
};
