import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AnalyticConfigDto } from "../types/AnalyticConfigDto";

// Everything else that used to live here (tracker-scoped analytic CRUD) moved to the
// Widget Library -- see features/widgets/api/widgetsController.ts. This catalog lookup
// (result types -> codes -> purposes) is tracker-agnostic and stayed as-is.
export const analyticsController = {
    getAnalyticsConfig: async (): Promise<ApiResponse<AnalyticConfigDto>> => {
        return await api.get("/analytics");
    },
};
