import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { PublicUserDto } from "../../auth/types/PublicApplicationUserDto";
import AddUserToTrackerDto from "../types/requests/AddUserToTrackerDto";
import { CreateTrackerDto } from "../types/requests/CreateTrackerDto";
import RemoveUserFromTrackerDto from "../types/requests/RemoveUserFromTrackerDto";
import { UpdateTrackerDto } from "../types/requests/UpdateTrackerDto";
import { TrackerDto } from "../types/TrackerDto";

export const trackersController = {
    getPublicTemplates: async (): Promise<ApiResponse<TrackerDto[]>> => {
        return await api.get("/trackers/templates");
    },
    getTracker: async (trackerId: string): Promise<ApiResponse<TrackerDto>> => {
        return await api.get(`/trackers/${trackerId}`);
    },
    createTracker: async (values: CreateTrackerDto): Promise<ApiResponse> => {
        return await api.post("/trackers", values);
    },
    updateTracker: async (
        trackerId: string,
        values: UpdateTrackerDto
    ): Promise<ApiResponse> => {
        return await api.put(`/trackers/${trackerId}`, values);
    },
    getAdminTemplateList: async (): Promise<ApiResponse<TrackerDto[]>> => {
        return await api.get("/trackers/admin-templates");
    },
    getTrackerList: async (
        filter?: string
    ): Promise<ApiResponse<TrackerDto[]>> => {
        return await api.get("/trackers", {
            params: filter ? { filter } : {},
        });
    },
    deleteTracker: async (trackerId: string): Promise<ApiResponse> => {
        return await api.delete(`/trackers/${trackerId}`);
    },
    getTrackerUserList: async (
        trackerId: string
    ): Promise<ApiResponse<PublicUserDto[]>> => {
        return await api.get(`/trackers/${trackerId}/users`);
    },
    addUserToTracker: async (
        trackerId: string,
        request: AddUserToTrackerDto
    ): Promise<ApiResponse> => {
        return await api.post(`/trackers/${trackerId}/users`, request);
    },
    removeUserFromTracker: async (
        trackerId: string,
        request: RemoveUserFromTrackerDto
    ) => {
        return await api.delete(`/trackers/${trackerId}/users`, {
            data: request,
        });
    },
    searchUsers: async (
        search: string
    ): Promise<ApiResponse<PublicUserDto[]>> => {
        return await api.get("/users?search=" + encodeURIComponent(search));
    },
};
