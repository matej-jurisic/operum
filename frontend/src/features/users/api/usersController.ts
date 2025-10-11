import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { UserDto } from "../../auth/types/UserDto";
import { ChangeUserRoleDto } from "../types/requests/ChangeUserRoleDto";

export const usersController = {
    getRoles: async (): Promise<ApiResponse<string[]>> => {
        return await api.get("/users/roles");
    },
    changeRole: async (
        userId: string,
        request: ChangeUserRoleDto
    ): Promise<ApiResponse> => {
        return await api.post(`/users/${userId}/role`, request);
    },
    getUserList: async (): Promise<ApiResponse<UserDto[]>> => {
        return await api.get("/users/all");
    },
    cnofirmEmail: async (userId: string): Promise<ApiResponse> => {
        return api.post(`/users/${userId}/confirm-email`);
    },
};
