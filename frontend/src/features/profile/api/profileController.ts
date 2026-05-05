import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { UserDto } from "../../auth/types/UserDto";

export interface UserProfileStatsDto {
    trackersOwned: number;
    sharedWithMe: number;
    totalEntries: number;
}

export const profileController = {
    getStats: async (): Promise<ApiResponse<UserProfileStatsDto>> => {
        return await api.get("/users/me/stats");
    },
    updateUsername: async (userName: string): Promise<ApiResponse<UserDto>> => {
        return await api.put("/users/me/username", { userName });
    },
    changePassword: async (currentPassword: string, newPassword: string): Promise<ApiResponse> => {
        return await api.put("/users/me/password", { currentPassword, newPassword });
    },
    deleteAccount: async (): Promise<ApiResponse> => {
        return await api.delete("/users/me");
    },
    updateTimezone: async (timeZone: string): Promise<ApiResponse> => {
        return await api.patch("/users/me/timezone", { timeZone });
    },
};
