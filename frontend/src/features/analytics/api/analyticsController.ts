import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AnalyticConfigDto } from "../types/AnalyticConfigDto";
import { AnalyticDto } from "../types/AnalyticDto";
import { CreateAnalyticDto } from "../types/requests/CreateAnalyticDto";

export const analyticsController = {
    getAnalyticsConfig: async (): Promise<ApiResponse<AnalyticConfigDto>> => {
        return await api.get("/analytics");
    },
    updateAnalyticsOrder: async (
        trackerId: string,
        analyticIds: string[]
    ): Promise<ApiResponse> => {
        return await api.put(`/trackers/${trackerId}/analytics/reorder`, {
            analyticIds,
        });
    },
    getTrackerAnalytics: async (
        trackerId: string,
        viewId?: string
    ): Promise<ApiResponse<AnalyticDto[]>> => {
        return await api.get(`/trackers/${trackerId}/analytics`, {
            params: viewId ? { viewId } : {},
        });
    },
    addAnalytic: async (
        trackerId: string,
        addAnalytic: CreateAnalyticDto
    ): Promise<ApiResponse> => {
        return await api.post(`/trackers/${trackerId}/analytics`, addAnalytic);
    },
    removeAnalytic: async (
        trackerId: string,
        analyticId: string
    ): Promise<ApiResponse> => {
        return await api.delete(
            `/trackers/${trackerId}/analytics/${analyticId}`
        );
    },
};
