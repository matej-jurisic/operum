import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { TrackerNotificationDto } from "../types/NotificationDto";
import {
    CreateTrackerNotificationDto,
    UpdateTrackerNotificationDto,
} from "../types/requests/CreateTrackerNotificationDto";

export const notificationsController = {
    getNotifications: async (
        trackerId: string
    ): Promise<ApiResponse<TrackerNotificationDto[]>> =>
        api.get(`/trackers/${trackerId}/notifications`),

    createNotification: async (
        trackerId: string,
        dto: CreateTrackerNotificationDto
    ): Promise<ApiResponse<TrackerNotificationDto>> =>
        api.post(`/trackers/${trackerId}/notifications`, dto),

    updateNotification: async (
        trackerId: string,
        notificationId: string,
        dto: UpdateTrackerNotificationDto
    ): Promise<ApiResponse<TrackerNotificationDto>> =>
        api.put(`/trackers/${trackerId}/notifications/${notificationId}`, dto),

    deleteNotification: async (
        trackerId: string,
        notificationId: string
    ): Promise<ApiResponse> =>
        api.delete(`/trackers/${trackerId}/notifications/${notificationId}`),

    toggleEnabled: async (
        trackerId: string,
        notificationId: string
    ): Promise<ApiResponse<TrackerNotificationDto>> =>
        api.patch(`/trackers/${trackerId}/notifications/${notificationId}/toggle`, {}),
};
