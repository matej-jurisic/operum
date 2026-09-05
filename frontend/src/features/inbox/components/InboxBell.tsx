import {
    ActionIcon,
    Button,
    Group,
    Indicator,
    Loader,
    Modal,
    ScrollArea,
    Stack,
    Tooltip,
} from "@mantine/core";
import { observer } from "mobx-react";
import { useState } from "react";
import { TbBell } from "react-icons/tb";
import EmptyState from "../../../shared/components/EmptyState";
import inboxStore from "../../../shared/stores/InboxStore";
import InboxItem from "./InboxItem";

interface Props {
    /** Collapsed rail: icon is larger and gets a hover tooltip, matching the search/account buttons. */
    collapsed: boolean;
}

const InboxBell = observer(({ collapsed }: Props) => {
    const [opened, setOpened] = useState(false);
    const count = inboxStore.unreadCount;
    const badgeLabel = count > 9 ? "9+" : String(count);

    const open = () => {
        setOpened(true);
        inboxStore.loadFirstPage();
    };
    const close = () => setOpened(false);

    const trigger = (
        <Indicator
            disabled={count === 0}
            label={badgeLabel}
            size={16}
            offset={collapsed ? 4 : 2}
        >
            <ActionIcon
                size={collapsed ? "lg" : "md"}
                variant={opened ? "light" : "subtle"}
                color="gray"
                aria-label="Notifications"
                onClick={open}
            >
                <TbBell size={18} />
            </ActionIcon>
        </Indicator>
    );

    return (
        <>
            {collapsed ? (
                <Tooltip label="Notifications" position="right" withArrow>
                    {trigger}
                </Tooltip>
            ) : (
                trigger
            )}

            <Modal
                opened={opened}
                onClose={close}
                title="Notifications"
                size="md"
                centered
                scrollAreaComponent={ScrollArea.Autosize}
            >
                <Stack gap="sm">
                    {count > 0 && (
                        <Group justify="flex-end">
                            <Button
                                variant="subtle"
                                size="compact-xs"
                                onClick={() => inboxStore.markAllRead()}
                            >
                                Mark all read
                            </Button>
                        </Group>
                    )}

                    {inboxStore.items.length === 0 ? (
                        inboxStore.loading ? (
                            <Group justify="center" py="xl">
                                <Loader size="sm" />
                            </Group>
                        ) : (
                            <EmptyState
                                title="No notifications yet"
                                hint="Alerts from your trackers show up here."
                            />
                        )
                    ) : (
                        <>
                            <Stack
                                gap={0}
                                mx="calc(var(--mantine-spacing-md) * -1)"
                            >
                                {inboxStore.items.map((item) => (
                                    <InboxItem
                                        key={item.id}
                                        item={item}
                                        onNavigate={close}
                                    />
                                ))}
                            </Stack>
                            {inboxStore.hasMore && (
                                <Group justify="center">
                                    <Button
                                        variant="subtle"
                                        size="compact-sm"
                                        loading={inboxStore.loading}
                                        onClick={() => inboxStore.loadMore()}
                                    >
                                        Load more
                                    </Button>
                                </Group>
                            )}
                        </>
                    )}
                </Stack>
            </Modal>
        </>
    );
});

export default InboxBell;
