import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { AnalyticSummaryDto } from "../../dashboard/types/DashboardDto";
import AddUserToTrackerDto from "../types/requests/AddUserToTrackerDto";
import { CreateTrackerDto } from "../types/requests/CreateTrackerDto";
import RemoveUserFromTrackerDto from "../types/requests/RemoveUserFromTrackerDto";
import UpdateCollaboratorPermissionsDto from "../types/requests/UpdateCollaboratorPermissionsDto";
import { UpdateTrackerDto } from "../types/requests/UpdateTrackerDto";
import { TrackerCollaboratorDto } from "../types/TrackerCollaboratorDto";
import { TrackerDto } from "../types/TrackerDto";

export const trackersController = {
    getPublicTemplates: async (): Promise<ApiResponse<TrackerDto[]>> => {
        return await api.get("/trackers/templates");
    },
    getTracker: async (trackerId: string): Promise<ApiResponse<TrackerDto>> => {
        return await api.get(`/trackers/${trackerId}`);
    },
    createTracker: async (values: CreateTrackerDto): Promise<ApiResponse<TrackerDto>> => {
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
    ): Promise<ApiResponse<TrackerCollaboratorDto[]>> => {
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
    updateCollaboratorPermissions: async (
        trackerId: string,
        request: UpdateCollaboratorPermissionsDto
    ): Promise<ApiResponse> => {
        return await api.put(`/trackers/${trackerId}/users`, request);
    },
    reorderTrackers: async (trackerIds: string[], filter: string): Promise<ApiResponse> => {
        return await api.put("/trackers/reorder", { trackerIds, filter });
    },
    searchUsers: async (
        search: string
    ): Promise<ApiResponse<{ id: string; userName: string }[]>> => {
        return await api.get("/users?search=" + encodeURIComponent(search));
    },
    getTrackerAnalyticsSummary: async (
        trackerId: string
    ): Promise<ApiResponse<AnalyticSummaryDto[]>> => {
        return await api.get(`/trackers/${trackerId}/analytics/summary`);
    },
};
