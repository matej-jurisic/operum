import { Box, CloseButton, Group, Stack, Text } from "@mantine/core";
import { observer } from "mobx-react";
import { useNavigate } from "react-router-dom";
import inboxStore from "../../../shared/stores/InboxStore";
import { relativeTime } from "../../../shared/utils/relativeTime";
import { InboxNotificationDto } from "../types/InboxNotificationDto";

interface Props {
    item: InboxNotificationDto;
    onNavigate: () => void;
}

const InboxItem = observer(({ item, onNavigate }: Props) => {
    const navigate = useNavigate();
    const unread = !item.readAt;

    const open = () => {
        inboxStore.markRead(item.id);
        navigate(item.url);
        onNavigate();
    };

    return (
        <Group
            gap="xs"
            wrap="nowrap"
            align="flex-start"
            px="md"
            py="sm"
            onClick={open}
            style={{
                cursor: "pointer",
                backgroundColor: unread
                    ? "var(--mantine-color-default-hover)"
                    : undefined,
            }}
        >
            <Box
                w={8}
                h={8}
                mt={7}
                style={{
                    flex: "0 0 auto",
                    borderRadius: "50%",
                    backgroundColor: unread
                        ? "var(--mantine-primary-color-filled)"
                        : "transparent",
                }}
            />
            <Stack gap={4} flex={1} miw={0}>
                <Text size="sm" fw={unread ? 600 : 500} lineClamp={1}>
                    {item.notificationName ?? item.title}
                </Text>
                <Text
                    size="sm"
                    c="dimmed"
                    lineClamp={3}
                    style={{ whiteSpace: "pre-line" }}
                >
                    {item.body}
                </Text>
                <Text size="xs" c="dimmed">
                    {item.trackerName} · {relativeTime(item.createdAt)}
                </Text>
            </Stack>
            <CloseButton
                size="sm"
                aria-label="Remove notification"
                onClick={(e) => {
                    e.stopPropagation();
                    inboxStore.remove(item.id);
                }}
            />
        </Group>
    );
});

export default InboxItem;
