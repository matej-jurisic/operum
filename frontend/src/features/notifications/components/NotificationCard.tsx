import {
    ActionIcon,
    Badge,
    Card,
    Flex,
    Group,
    Stack,
    Switch,
    Text,
    Title,
    Tooltip,
} from "@mantine/core";
import { formatDateTime } from "../../../shared/utils/formatters/TypeFormatter";
import { MdDelete, MdEdit, MdReplay } from "react-icons/md";
import { useNotifications } from "../context/NotificationsContext";
import { NotificationEventDto, TrackerNotificationDto } from "../types/NotificationDto";

const VALUE_MODE_LABELS: Record<string, string> = {
    Entry: "Entry records",
    Analytic: "Computed value",
};

function formatEvent(event: NotificationEventDto): string {
    if (!event) return "On change";
    switch (event.eventType) {
        case "Triggered":
            return "On change";
        case "Day": {
            const interval = event.intervalDays ?? 1;
            const skip = event.skipWeekendsDay ? ", weekdays only" : "";
            const time = event.timeOfDay ?? "00:00";
            return interval === 1
                ? `Daily at ${time}${skip}`
                : `Every ${interval} days at ${time}${skip}`;
        }
        case "Week": {
            const interval = event.intervalWeeks ?? 1;
            const days = (event.daysOfWeek ?? []).join(", ");
            const time = event.timeOfDay ?? "00:00";
            return interval === 1
                ? `Weekly on ${days} at ${time}`
                : `Every ${interval} weeks on ${days} at ${time}`;
        }
        case "Month": {
            const time = event.timeOfDay ?? "00:00";
            const day = event.lastDayOfMonth
                ? "last day"
                : `day ${event.dayOfMonth ?? 1}`;
            const skip = event.skipWeekendsMonth ? ", weekdays only" : "";
            return `Monthly on ${day} at ${time}${skip}`;
        }
        default:
            return event.eventType;
    }
}

interface Props {
    notification: TrackerNotificationDto;
    color?: string;
    canEditSchema: boolean;
    onEdit: (n: TrackerNotificationDto) => void;
    onDelete: (n: TrackerNotificationDto) => void;
    onReset: (n: TrackerNotificationDto) => void;
    viewNames: Record<string, string>;
}

export default function NotificationCard({
    notification,
    color,
    canEditSchema,
    onEdit,
    onDelete,
    onReset,
    viewNames,
}: Props) {
    const { _toggleEnabled } = useNotifications();

    const scopedViewName = notification.viewId
        ? viewNames[notification.viewId]
        : undefined;

    return (
        <Card p="md" radius="md" withBorder>
            <Flex
                direction={{ base: "column", sm: "row" }}
                align={{ base: "stretch", sm: "flex-start" }}
                justify="space-between"
                gap="sm"
            >
                <Stack gap="xs" flex={1} miw={0}>
                    <Title order={4} lineClamp={1} className="wrapped-text">
                        {notification.name}
                    </Title>
                    <Text c="dimmed" size="sm" lineClamp={2} className="wrapped-text">
                        {formatEvent(notification.event)} · {VALUE_MODE_LABELS[notification.condition.valueMode] ?? notification.condition.valueMode}
                    </Text>
                    <Group wrap="wrap">
                        {notification.isTriggered && (
                            <Badge variant="light" color="red" size="sm">
                                Triggered
                            </Badge>
                        )}
                        {scopedViewName ? (
                            <Badge variant="light" color={color} size="sm">
                                {scopedViewName}
                            </Badge>
                        ) : (
                            <Badge variant="light" color="gray" size="sm">
                                All entries
                            </Badge>
                        )}
                    </Group>
                    {notification.lastFiredAt && (
                        <Text size="xs" c="dimmed">
                            Last fired: {formatDateTime(notification.lastFiredAt)}
                        </Text>
                    )}
                </Stack>

                <Flex
                    gap="xs"
                    wrap="nowrap"
                    align="flex-start"
                    justify={{ base: "flex-end", sm: "flex-start" }}
                >
                    <Switch
                        checked={notification.isEnabled}
                        color={color}
                        onChange={() => _toggleEnabled(notification.id)}
                    />
                    {canEditSchema && (
                        <>
                            <Tooltip
                                label="Alert again about entries it already reported"
                                withArrow
                                multiline
                                w={220}
                            >
                                <ActionIcon
                                    variant="outline"
                                    color="gray"
                                    size="lg"
                                    onClick={() => onReset(notification)}
                                    aria-label={`Reset notification ${notification.name}`}
                                >
                                    <MdReplay size={16} />
                                </ActionIcon>
                            </Tooltip>
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
                </Flex>
            </Flex>
        </Card>
    );
}
