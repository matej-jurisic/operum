import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { CorrelationInsightDto } from "../types/CorrelationInsightDto";

export const correlationInsightsController = {
    getInsights: async (): Promise<ApiResponse<CorrelationInsightDto[]>> => {
        return await api.get("/correlationinsights");
    },

    runNow: async (): Promise<ApiResponse<CorrelationInsightDto[]>> => {
        return await api.post("/correlationinsights/run");
    },

    dismiss: async (insightId: string): Promise<ApiResponse> => {
        return await api.post(`/correlationinsights/${insightId}/dismiss`);
    },
};
