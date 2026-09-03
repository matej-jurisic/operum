import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { EvaluateWidgetDto } from "../types/EvaluateWidgetDto";

export const exploreController = {
    evaluate: async (
        dto: EvaluateWidgetDto
    ): Promise<ApiResponse<AnalyticDto>> => {
        return await api.post("/analytics/evaluate", dto);
    },
};
