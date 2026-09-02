import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import {
    IntegrationDto,
    IntegrationTargetDto,
    ProviderDto,
    SyncResultDto,
} from "../types/IntegrationDto";
import {
    ConnectIntegrationDto,
    SaveIntegrationTargetDto,
} from "../types/requests/SaveIntegrationTargetDto";

export const integrationsController = {
    getProviders: async (): Promise<ApiResponse<ProviderDto[]>> =>
        api.get("/integrations/providers"),

    getIntegrations: async (): Promise<ApiResponse<IntegrationDto[]>> =>
        api.get("/integrations"),

    connect: async (
        dto: ConnectIntegrationDto,
    ): Promise<ApiResponse<IntegrationDto>> => api.post("/integrations", dto),

    disconnect: async (integrationId: string): Promise<ApiResponse> =>
        api.delete(`/integrations/${integrationId}`),

    createTarget: async (
        integrationId: string,
        dto: SaveIntegrationTargetDto,
    ): Promise<ApiResponse<IntegrationTargetDto>> =>
        api.post(`/integrations/${integrationId}/targets`, dto),

    updateTarget: async (
        integrationId: string,
        targetId: string,
        dto: SaveIntegrationTargetDto,
    ): Promise<ApiResponse<IntegrationTargetDto>> =>
        api.put(`/integrations/${integrationId}/targets/${targetId}`, dto),

    deleteTarget: async (
        integrationId: string,
        targetId: string,
    ): Promise<ApiResponse> =>
        api.delete(`/integrations/${integrationId}/targets/${targetId}`),

    syncNow: async (
        integrationId: string,
        targetId: string,
    ): Promise<ApiResponse<SyncResultDto>> =>
        api.post(`/integrations/${integrationId}/targets/${targetId}/sync`, {}),

    resyncTarget: async (
        integrationId: string,
        targetId: string,
    ): Promise<ApiResponse<SyncResultDto>> =>
        api.post(`/integrations/${integrationId}/targets/${targetId}/resync`, {}),

    syncIntegration: async (
        integrationId: string,
    ): Promise<ApiResponse<SyncResultDto>> =>
        api.post(`/integrations/${integrationId}/sync`, {}),

    rotateWebhookSecret: async (
        integrationId: string,
        targetId: string,
    ): Promise<ApiResponse<IntegrationTargetDto>> =>
        api.post(
            `/integrations/${integrationId}/targets/${targetId}/rotate-secret`,
            {},
        ),
};
