import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AnalyticConfigDto } from "../types/AnalyticConfigDto";

// Tracker-scoped analytic CRUD lives in features/widgets/api/widgetsController.ts.
export const analyticsController = {
    getAnalyticsConfig: async (): Promise<ApiResponse<AnalyticConfigDto>> => {
        return await api.get("/analytics");
    },
};
