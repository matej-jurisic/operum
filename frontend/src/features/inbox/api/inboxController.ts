import api from "../../../shared/api/api";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { InboxPageDto } from "../types/InboxNotificationDto";

export const inboxController = {
    getInbox: async (skip: number, take: number): Promise<ApiResponse<InboxPageDto>> =>
        api.get(`/inbox?skip=${skip}&take=${take}`),

    getUnreadCount: async (): Promise<ApiResponse<number>> =>
        api.get(`/inbox/unread-count`),

    markRead: async (id: string): Promise<ApiResponse> =>
        api.post(`/inbox/${id}/read`, {}),

    markAllRead: async (): Promise<ApiResponse> =>
        api.post(`/inbox/read-all`, {}),

    deleteItem: async (id: string): Promise<ApiResponse> =>
        api.delete(`/inbox/${id}`),
};
