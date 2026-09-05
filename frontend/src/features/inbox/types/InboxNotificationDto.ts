export interface InboxNotificationDto {
    id: string;
    trackerId: string;
    trackerName: string;
    notificationName: string | null;
    title: string;
    body: string;
    url: string;
    createdAt: string;
    readAt: string | null;
}

export interface InboxPageDto {
    items: InboxNotificationDto[];
    unreadCount: number;
    hasMore: boolean;
}
