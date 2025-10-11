import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AuthResponseDto } from "../types/AuthResponseDto";
import { ConfirEmailDto } from "../types/requests/ConfirmEmailDto";
import { GoogleLoginDto } from "../types/requests/GoogleLoginDto";
import { LoginRequestDto } from "../types/requests/LoginDto";
import { RegisterDto } from "../types/requests/RegisterDto";
import { UserDto } from "../types/UserDto";

export const authController = {
    login: async (
        request: LoginRequestDto
    ): Promise<ApiResponse<AuthResponseDto>> => {
        return await api.post("/auth/login", request);
    },

    register: async (request: RegisterDto): Promise<ApiResponse> => {
        return await api.post("/auth/register", request);
    },

    logout: async (): Promise<ApiResponse> => {
        return await api.post("/auth/logout");
    },

    refreshToken: async (): Promise<ApiResponse<AuthResponseDto>> => {
        return await api.post("/auth/refresh");
    },

    me: async (): Promise<ApiResponse<UserDto>> => {
        return await api.get("/auth/me");
    },

    confirmEmail: async (request: ConfirEmailDto): Promise<ApiResponse> => {
        return await api.post("/auth/confirm-email", request);
    },

    googleLogin: async (
        request: GoogleLoginDto
    ): Promise<ApiResponse<AuthResponseDto>> => {
        return await api.post("/auth/google", request);
    },
};
