import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { QueryDto } from "../types/QueryDto";
import { CreateQueryDto } from "../types/requests/CreateQueryDto";
import { UpdateQueryDto } from "../types/requests/UpdateQueryDto";

export const queriesController = {
    getQuery: async (
        trackerId: string,
        queryId: string
    ): Promise<ApiResponse<QueryDto>> => {
        return await api.get(`trackers/${trackerId}/queries/${queryId}`);
    },
    getQueryList: async (trackerId: string): Promise<ApiResponse<QueryDto[]>> => {
        return await api.get(`trackers/${trackerId}/queries`);
    },
    createQuery: async (
        trackerId: string,
        values: CreateQueryDto
    ): Promise<ApiResponse<QueryDto>> => {
        return await api.post(`/trackers/${trackerId}/queries`, values);
    },
    updateQuery: async (
        trackerId: string,
        queryId: string,
        values: UpdateQueryDto
    ): Promise<ApiResponse<QueryDto>> => {
        return await api.put(`/trackers/${trackerId}/queries/${queryId}`, values);
    },
    deleteQuery: async (
        trackerId: string,
        queryId: string
    ): Promise<ApiResponse> => {
        return api.delete(`trackers/${trackerId}/queries/${queryId}`);
    },
};
