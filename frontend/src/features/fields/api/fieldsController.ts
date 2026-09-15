import api, { LONG_REQUEST_TIMEOUT_MS } from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { CreateFieldDto } from "../types/CreateFieldDto";
import {
    ExtractFieldsDto,
    ExtractFieldsResultDto,
} from "../types/ExtractFieldsDto";
import { FieldDto } from "../types/FieldDto";
import { UpdateFieldDto } from "../types/UpdateFieldDto";

export const fieldsController = {
    getFields: async (trackerId: string): Promise<ApiResponse<FieldDto[]>> => {
        return await api.get(`/trackers/${trackerId}/fields`);
    },
    createField: async (
        trackerId: string,
        values: CreateFieldDto
    ): Promise<ApiResponse<FieldDto>> => {
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
    extractFields: async (
        trackerId: string,
        values: ExtractFieldsDto
    ): Promise<ApiResponse<ExtractFieldsResultDto>> => {
        return await api.post(
            `/trackers/${trackerId}/fields/extract`,
            values,
            { timeout: LONG_REQUEST_TIMEOUT_MS }
        );
    },
};
