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
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import { CiCirclePlus, CiSettings } from "react-icons/ci";
import EntryDetailsDialog from "../../entries/components/EntryDetailsDialog";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useAnalytics } from "../context/AnalyticsContext";
import AnalyticSelectionDialog from "./analyticSelection/AnalyticSelectionDialog";
import { AnalyticsGrid } from "./AnalyticsGrid";

export default function Analytics() {
    const { tracker, canEditSchema } = useTracker();
    const { refreshAnalytics, analytics, analyticsDirty } = useAnalytics();
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const [isLoadingData, setIsLoadingData] = useState(analyticsDirty);
    const [openDialogType, setOpenDialogType] = useState<
        "configureAnalytics" | undefined
    >();
    const [isConfiguring, setIsConfiguring] = useState(false);
    const [selectedEntryId, setSelectedEntryId] = useState<string>();

    useEffect(() => {
        if (!analyticsDirty) return;
        const loadData = async () => {
            setIsLoadingData(true);
            await refreshAnalytics();
            setIsLoadingData(false);
        };
        loadData();
    }, [analyticsDirty]);

    const hasAnalytics = analytics.length > 0;

    const showEmptyState = !isLoadingData && !hasAnalytics && !isConfiguring;

    return (
        <>
            <Stack gap="md" h="100%">
                {canEditSchema && (
                    <Group justify="flex-end" w="100%" h={36}>
                        {isConfiguring && (
                            <Button
                                variant="outline"
                                color={tracker.color}
                                leftSection={<CiCirclePlus size={20} />}
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
                            <CiSettings size={18} />
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
                        offsetScrollbars={isMobile && isConfiguring}
                        type={isMobile && isConfiguring ? "always" : "scroll"}
                        scrollbarSize={25}
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
