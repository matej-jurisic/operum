import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AnalyticConfigDto } from "../types/AnalyticConfigDto";

export const analyticsController = {
    getAnalyticsConfig: async (): Promise<ApiResponse<AnalyticConfigDto>> => {
        return await api.get("/analytics");
    },
};
