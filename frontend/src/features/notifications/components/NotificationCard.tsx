import {
    ActionIcon,
    Badge,
    Card,
    Group,
    Stack,
    Switch,
    Text,
    Title,
} from "@mantine/core";
import { MdDelete, MdEdit } from "react-icons/md";
import { useNotifications } from "../context/NotificationsContext";
import { TrackerNotificationDto } from "../types/NotificationDto";

interface Props {
    notification: TrackerNotificationDto;
    color?: string;
    canEditSchema: boolean;
    onEdit: (n: TrackerNotificationDto) => void;
    onDelete: (n: TrackerNotificationDto) => void;
    viewNames: Record<string, string>;
}

export default function NotificationCard({
    notification,
    color,
    canEditSchema,
    onEdit,
    onDelete,
    viewNames,
}: Props) {
    const { _toggleEnabled } = useNotifications();

    const scopedViewNames = notification.viewIds
        .map((id) => viewNames[id])
        .filter(Boolean);

    return (
        <Card p="md" radius="md" withBorder>
            <Group align="flex-start" justify="space-between" wrap="nowrap">
                <Stack gap="xs" flex={1}>
                    <Title order={4} lineClamp={1} className="wrapped-text">
                        {notification.name}
                    </Title>
                    <Text c="dimmed" size="sm" lineClamp={2} className="wrapped-text">
                        {notification.condition.code} {notification.condition.operator} {notification.condition.value}
                    </Text>
                    <Group wrap="wrap">
                        {notification.isTriggered && (
                            <Badge variant="light" color="red" size="sm">
                                Triggered
                            </Badge>
                        )}
                        {scopedViewNames.length > 0 ? (
                            scopedViewNames.map((name) => (
                                <Badge key={name} variant="light" color={color} size="sm">
                                    {name}
                                </Badge>
                            ))
                        ) : (
                            <Badge variant="light" color="gray" size="sm">
                                All entries
                            </Badge>
                        )}
                    </Group>
                    {notification.lastTriggeredAt && (
                        <Text size="xs" c="dimmed">
                            Last triggered: {new Date(notification.lastTriggeredAt).toLocaleString()}
                        </Text>
                    )}
                </Stack>

                <Group gap="xs" wrap="nowrap" align="flex-start">
                    <Switch
                        checked={notification.isEnabled}
                        color={color}
                        onChange={() => _toggleEnabled(notification.id)}
                    />
                    {canEditSchema && (
                        <>
                            <ActionIcon
                                variant="outline"
                                color="green"
                                size="lg"
                                onClick={() => onEdit(notification)}
                                aria-label={`Edit notification ${notification.name}`}
                            >
                                <MdEdit size={16} />
                            </ActionIcon>
                            <ActionIcon
                                variant="outline"
                                color="red"
                                size="lg"
                                onClick={() => onDelete(notification)}
                                aria-label={`Delete notification ${notification.name}`}
                            >
                                <MdDelete size={16} />
                            </ActionIcon>
                        </>
                    )}
                </Group>
            </Group>
        </Card>
    );
}
