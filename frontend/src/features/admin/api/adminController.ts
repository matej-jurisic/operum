import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AdminStatsDto } from "../types/AdminStatsDto";
import { AdminTrackerDto } from "../types/AdminTrackerDto";

export const adminController = {
    getStats: async (): Promise<ApiResponse<AdminStatsDto>> => {
        return await api.get("/admin/stats");
    },
    getAllTrackers: async (): Promise<ApiResponse<AdminTrackerDto[]>> => {
        return await api.get("/admin/trackers");
    },
};
