import { AxiosRequestConfig, AxiosResponse } from "axios";
import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { EntryDto } from "../types/EntryDto";

export const entriesController = {
    exportCsv: async (
        trackerId: string,
        viewIds?: string[]
    ): Promise<AxiosResponse<BlobPart>> => {
        const params = new URLSearchParams();
        viewIds?.forEach((id) => params.append("viewId", id));
        const qs = params.toString();
        return await api.get(
            `/trackers/${trackerId}/entries/export-csv${qs ? `?${qs}` : ""}`,
            { responseType: "blob" }
        );
    },
    getEntry: async (
        trackerId: string,
        entryId: string
    ): Promise<ApiResponse<EntryDto>> => {
        return await api.get(`/trackers/${trackerId}/entries/${entryId}`);
    },
    getEntries: async (
        trackerId: string,
        viewIds?: string[]
    ): Promise<ApiResponse<EntryDto[]>> => {
        const params = new URLSearchParams();
        viewIds?.forEach((id) => params.append("viewId", id));
        const qs = params.toString();
        return await api.get(
            `/trackers/${trackerId}/entries${qs ? `?${qs}` : ""}`
        );
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
    recalculateEntries: async (
        trackerId: string,
        entryIds: string[]
    ): Promise<ApiResponse> => {
        return await api.post(`/trackers/${trackerId}/entries/recalculate`, {
            entryIds,
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
