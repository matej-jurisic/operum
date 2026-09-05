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
            px="sm"
            py="xs"
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
                mt={6}
                style={{
                    flex: "0 0 auto",
                    borderRadius: "50%",
                    backgroundColor: unread
                        ? "var(--mantine-primary-color-filled)"
                        : "transparent",
                }}
            />
            <Stack gap={2} flex={1} miw={0}>
                <Text size="sm" fw={unread ? 600 : 400} lineClamp={1}>
                    {item.title}
                </Text>
                <Text size="xs" c="dimmed" lineClamp={2}>
                    {item.body}
                </Text>
                <Text size="xs" c="dimmed">
                    {relativeTime(item.createdAt)}
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
