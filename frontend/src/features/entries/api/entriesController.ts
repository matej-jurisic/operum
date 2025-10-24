import { AxiosRequestConfig, AxiosResponse } from "axios";
import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { EntryDto } from "../types/EntryDto";

export const entriesController = {
    exportCsv: async (
        trackerId: string,
        viewId?: string
    ): Promise<AxiosResponse<BlobPart>> => {
        return await api.get(`/trackers/${trackerId}/entries/export-csv`, {
            params: viewId ? { viewId } : {},
            responseType: "blob",
        });
    },
    getEntry: async (
        trackerId: string,
        entryId: string
    ): Promise<ApiResponse<EntryDto>> => {
        return await api.get(`/trackers/${trackerId}/entries/${entryId}`);
    },
    getEntries: async (
        trackerId: string,
        viewId?: string
    ): Promise<ApiResponse<EntryDto[]>> => {
        return await api.get(`/trackers/${trackerId}/entries`, {
            params: viewId ? { viewId } : {},
        });
    },
    createEntry: async (
        trackerId: string,
        fieldValues: Record<string, string>
    ): Promise<ApiResponse> => {
        return await api.post(`/trackers/${trackerId}/entries`, {
            fieldValues,
        });
    },
    updateEntry: async (
        trackerId: string,
        entryId: string,
        fieldValues: Record<string, string>
    ): Promise<ApiResponse> => {
        return await api.put(`/trackers/${trackerId}/entries/${entryId}`, {
            fieldValues,
        });
    },
    deleteEntry: async (
        trackerId: string,
        entryId: string
    ): Promise<ApiResponse> => {
        return await api.delete(`/trackers/${trackerId}/entries/${entryId}`);
    },
    deleteEntries: async (
        trackerId: string,
        entryIds: string[]
    ): Promise<ApiResponse> => {
        return await api.delete(`/trackers/${trackerId}/entries`, {
            data: { entryIds },
        });
    },
    importEntries: async (
        trackerId: string,
        data: FormData
    ): Promise<ApiResponse> => {
        const config: AxiosRequestConfig = {
            headers: {
                "Content-Type": "multipart/form-data",
            },
        };
        return await api.post(
            `/trackers/${trackerId}/entries/import-csv`,
            data,
            config
        );
    },
};
