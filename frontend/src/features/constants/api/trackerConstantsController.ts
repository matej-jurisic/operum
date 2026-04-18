import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import {
    CreateTrackerConstantDto,
    TrackerConstantDto,
    UpdateTrackerConstantDto,
} from "../types/TrackerConstantDto";

export const trackerConstantsController = {
    getConstants: async (
        trackerId: string
    ): Promise<ApiResponse<TrackerConstantDto[]>> => {
        return await api.get(`/trackers/${trackerId}/constants`);
    },
    createConstant: async (
        trackerId: string,
        values: CreateTrackerConstantDto
    ): Promise<ApiResponse<TrackerConstantDto>> => {
        return await api.post(`/trackers/${trackerId}/constants`, values);
    },
    updateConstant: async (
        trackerId: string,
        constantId: string,
        values: UpdateTrackerConstantDto
    ): Promise<ApiResponse<TrackerConstantDto>> => {
        return await api.put(
            `/trackers/${trackerId}/constants/${constantId}`,
            values
        );
    },
    deleteConstant: async (
        trackerId: string,
        constantId: string
    ): Promise<ApiResponse> => {
        return await api.delete(
            `/trackers/${trackerId}/constants/${constantId}`
        );
    },
};
