import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { CreateViewDto } from "../types/requests/CreateViewDto";
import { UpdateViewDto } from "../types/requests/UpdateViewDto";
import { ViewDto } from "../types/ViewDto";

export const viewsController = {
    getView: async (
        trackerId: string,
        viewId: string
    ): Promise<ApiResponse<ViewDto>> => {
        return await api.get(`trackers/${trackerId}/views/${viewId}`);
    },
    getViewList: async (trackerId: string): Promise<ApiResponse<ViewDto[]>> => {
        return await api.get(`trackers/${trackerId}/views`);
    },
    setDefaultView: async (
        trackerId: string,
        viewId?: string
    ): Promise<ApiResponse> => {
        return await api.put(`/trackers/${trackerId}/default-view`, null, {
            params: { defaultViewId: viewId },
        });
    },
    createView: async (
        trackerId: string,
        values: CreateViewDto
    ): Promise<ApiResponse> => {
        return await api.post(`/trackers/${trackerId}/views`, values);
    },
    updateView: async (
        trackerId: string,
        viewId: string,
        values: UpdateViewDto
    ): Promise<ApiResponse> => {
        return await api.put(`/trackers/${trackerId}/views/${viewId}`, values);
    },
    deleteView: async (
        trackerId: string,
        viewId: string
    ): Promise<ApiResponse> => {
        return api.delete(`trackers/${trackerId}/views/${viewId}`);
    },
    updateViewOrder: async (
        trackerId: string,
        viewIds: string[]
    ): Promise<ApiResponse> => {
        return await api.put(`/trackers/${trackerId}/views/reorder`, { viewIds });
    },
};
