import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { CreateFieldDto } from "../types/CreateFieldDto";
import { FieldDto } from "../types/FieldDto";
import { UpdateFieldDto } from "../types/UpdateFieldDto";

export const fieldsController = {
    getFields: async (trackerId: string): Promise<ApiResponse<FieldDto[]>> => {
        return await api.get(`/trackers/${trackerId}/fields`);
    },
    createField: async (
        trackerId: string,
        values: CreateFieldDto
    ): Promise<ApiResponse> => {
        return await api.post(`/trackers/${trackerId}/fields`, values);
    },
    updateFieldOrder: async (
        trackerId: string,
        fieldIds: string[]
    ): Promise<ApiResponse> => {
        return await api.put(`/trackers/${trackerId}/fields/reorder`, {
            fieldIds,
        });
    },
    updateField: async (
        trackerId: string,
        fieldId: string,
        values: UpdateFieldDto
    ): Promise<ApiResponse> => {
        return await api.put(
            `/trackers/${trackerId}/fields/${fieldId}`,
            values
        );
    },
    deleteField: async (
        trackerId: string,
        fieldId: string
    ): Promise<ApiResponse> => {
        return await api.delete(`/trackers/${trackerId}/fields/${fieldId}`);
    },
};
