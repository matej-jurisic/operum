import React, { createContext, useCallback, useContext, useState } from "react";
import { useTracker } from "../../trackers/context/TrackerContext";
import { notificationsController } from "../api/notificationsController";
import { TrackerNotificationDto } from "../types/NotificationDto";
import {
    CreateTrackerNotificationDto,
    UpdateTrackerNotificationDto,
} from "../types/requests/CreateTrackerNotificationDto";

type NotificationsContextType = {
    notifications: TrackerNotificationDto[];
    notificationsDirty: boolean;
    refreshNotifications: () => Promise<void>;
    refreshNotificationsIfDirty: () => Promise<void>;
    markNotificationsDirty: () => void;
    _createNotification: (dto: CreateTrackerNotificationDto) => Promise<void>;
    _updateNotification: (id: string, dto: UpdateTrackerNotificationDto) => Promise<void>;
    _deleteNotification: (id: string) => Promise<void>;
    _toggleEnabled: (id: string) => Promise<void>;
};

const NotificationsContext = createContext<NotificationsContextType | undefined>(undefined);

export const NotificationsProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const { tracker } = useTracker();
    const [notifications, setNotifications] = useState<TrackerNotificationDto[]>([]);
    const [notificationsDirty, setNotificationsDirty] = useState(true);

    const refreshNotifications = useCallback(async () => {
        const response = await notificationsController.getNotifications(tracker.id);
        setNotifications(response.data);
        setNotificationsDirty(false);
    }, [tracker.id]);

    const refreshNotificationsIfDirty = useCallback(async () => {
        if (notificationsDirty) await refreshNotifications();
    }, [notificationsDirty, refreshNotifications]);

    const markNotificationsDirty = useCallback(() => setNotificationsDirty(true), []);

    const _createNotification = async (dto: CreateTrackerNotificationDto) => {
        await notificationsController.createNotification(tracker.id, dto);
        await refreshNotifications();
    };

    const _updateNotification = async (id: string, dto: UpdateTrackerNotificationDto) => {
        await notificationsController.updateNotification(tracker.id, id, dto);
        await refreshNotifications();
    };

    const _deleteNotification = async (id: string) => {
        await notificationsController.deleteNotification(tracker.id, id);
        await refreshNotifications();
    };

    const _toggleEnabled = async (id: string) => {
        await notificationsController.toggleEnabled(tracker.id, id);
        await refreshNotifications();
    };

    return (
        <NotificationsContext.Provider
            value={{
                notifications,
                notificationsDirty,
                refreshNotifications,
                refreshNotificationsIfDirty,
                markNotificationsDirty,
                _createNotification,
                _updateNotification,
                _deleteNotification,
                _toggleEnabled,
            }}
        >
            {children}
        </NotificationsContext.Provider>
    );
};

export const useNotifications = () => {
    const ctx = useContext(NotificationsContext);
    if (!ctx) throw new Error("useNotifications must be used within NotificationsProvider");
    return ctx;
};
