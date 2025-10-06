import {
    ActionIcon,
    Button,
    em,
    Group,
    Paper,
    ScrollArea,
    Skeleton,
    Stack,
    Text,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiSettings } from "react-icons/fi";
import { useTracker } from "../context/TrackerContext";
import globalStore from "../stores/GlobalStore";

import { useMediaQuery } from "@mantine/hooks";
import { MdAdd } from "react-icons/md";
import AnalyticSelectionDialog from "./AnalyticSelectionDialog";
import { AnalyticsGrid } from "./AnalyticsGrid";
import EntryDetailsDialog from "./EntryDetailsDialog";

export default function AnalyticsList() {
    const { analytics, refreshAnalyticsIfDirty, selectedViewId, tracker } =
        useTracker();
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const [isLoadingData, setIsLoadingData] = useState(true);
    const [openDialogType, setOpenDialogType] = useState<
        "configureAnalytics" | undefined
    >();
    const [isConfiguring, setIsConfiguring] = useState(false);
    const [selectedEntryId, setSelectedEntryId] = useState<string>();

    useEffect(() => {
        const loadData = async () => {
            setIsLoadingData(true);
            await refreshAnalyticsIfDirty();
            setIsLoadingData(false);
        };
        loadData();
    }, [selectedViewId]);

    const hasAnalytics = analytics && analytics.analytics.length > 0;

    const showEmptyState = !isLoadingData && !hasAnalytics && !isConfiguring;

    return (
        <>
            <Stack gap="md" h="100%">
                {globalStore.currentUser?.id === tracker.ownerId && (
                    <Group justify="flex-end" w="100%" h={36}>
                        {isConfiguring && (
                            <Button
                                variant="outline"
                                color={tracker.color}
                                leftSection={<MdAdd size={20} />}
                                onClick={() => {
                                    setOpenDialogType("configureAnalytics");
                                }}
                            >
                                Add Analytic
                            </Button>
                        )}
                        <ActionIcon
                            size="lg"
                            variant={isConfiguring ? "filled" : "outline"}
                            color={tracker.color}
                            onClick={() => setIsConfiguring((prev) => !prev)}
                        >
                            <FiSettings size={18} />
                        </ActionIcon>
                    </Group>
                )}

                {showEmptyState ? (
                    <Paper withBorder p="xl" radius="md">
                        <Stack gap="md" align="center">
                            <Text size="lg" fw={500} c="dimmed">
                                No Analytics Available
                            </Text>
                            <Text ta="center" c="dimmed">
                                Analytics will appear here once you configure
                                them and have data entries for your tracker.
                            </Text>
                        </Stack>
                    </Paper>
                ) : (
                    <ScrollArea
                        flex={1}
                        mih={0}
                        offsetScrollbars
                        type={isMobile && isConfiguring ? "always" : "scroll"}
                    >
                        <Skeleton visible={isLoadingData} h={500}>
                            {analytics && (
                                <AnalyticsGrid
                                    analytics={analytics}
                                    isConfiguring={isConfiguring}
                                    onEntryClick={setSelectedEntryId}
                                />
                            )}
                        </Skeleton>
                    </ScrollArea>
                )}
            </Stack>

            {selectedEntryId && (
                <EntryDetailsDialog
                    entryId={selectedEntryId}
                    tracker={tracker}
                    onClose={() => setSelectedEntryId(undefined)}
                />
            )}

            {openDialogType === "configureAnalytics" && (
                <AnalyticSelectionDialog
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
        </>
    );
}
