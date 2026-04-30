import {
    Button,
    Group,
    Paper,
    ScrollArea,
    Stack,
    Text,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { FiPlus } from "react-icons/fi";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../../views/context/ViewsContext";
import { useNotifications } from "../context/NotificationsContext";
import { TrackerNotificationDto } from "../types/NotificationDto";
import NotificationCard from "./NotificationCard";
import NotificationFormDialog from "./NotificationFormDialog";

enum OpenDialogType {
    Create,
    Edit,
    Delete,
}

export default function Notifications() {
    const { tracker, canEditSchema } = useTracker();
    const { notifications, refreshNotificationsIfDirty, _deleteNotification } =
        useNotifications();
    const { views, refreshViewsIfDirty } = useViews();

    const [selectedNotification, setSelectedNotification] =
        useState<TrackerNotificationDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    useEffect(() => {
        refreshNotificationsIfDirty();
        refreshViewsIfDirty();
    }, []);

    const viewNames = useMemo(
        () => Object.fromEntries(views.map((v) => [v.id, v.name])),
        [views]
    );

    return (
        <>
            <Stack gap="md" h="100%">
                {canEditSchema && (
                    <Group justify="flex-start">
                        <Button
                            color={tracker.color}
                            onClick={() => setOpenDialogType(OpenDialogType.Create)}
                            variant="outline"
                            leftSection={<FiPlus size={18} />}
                        >
                            Create
                        </Button>
                    </Group>
                )}

                <ScrollArea flex={1} mih={0}>
                    {notifications.length > 0 ? (
                        <Stack gap="md">
                            {notifications.map((n) => (
                                <NotificationCard
                                    key={n.id}
                                    notification={n}
                                    color={tracker.color}
                                    canEditSchema={canEditSchema}
                                    viewNames={viewNames}
                                    onEdit={(notification) => {
                                        setSelectedNotification(notification);
                                        setOpenDialogType(OpenDialogType.Edit);
                                    }}
                                    onDelete={(notification) => {
                                        setSelectedNotification(notification);
                                        setOpenDialogType(OpenDialogType.Delete);
                                    }}
                                />
                            ))}
                        </Stack>
                    ) : (
                        <Paper withBorder p="xl" radius="md">
                            <Stack gap="md" align="center">
                                <Text size="lg" fw={500} c="dimmed">
                                    No Notifications
                                </Text>
                                <Text ta="center" c="dimmed">
                                    Add a notification to be alerted when a
                                    condition is met.
                                </Text>
                            </Stack>
                        </Paper>
                    )}
                </ScrollArea>
            </Stack>

            {openDialogType === OpenDialogType.Create && (
                <NotificationFormDialog
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}

            {selectedNotification &&
                openDialogType === OpenDialogType.Edit && (
                    <NotificationFormDialog
                        onClose={() => {
                            setOpenDialogType(undefined);
                            setSelectedNotification(undefined);
                        }}
                        initialNotification={selectedNotification}
                    />
                )}

            {selectedNotification &&
                openDialogType === OpenDialogType.Delete && (
                    <ConfirmationDialog
                        isOpen
                        onClose={() => setOpenDialogType(undefined)}
                        onConfirm={async () => {
                            await _deleteNotification(selectedNotification.id);
                            setOpenDialogType(undefined);
                            setSelectedNotification(undefined);
                        }}
                        severity="warning"
                        message={`Are you sure you want to delete the notification "${selectedNotification.name}"?`}
                    />
                )}
        </>
    );
}
