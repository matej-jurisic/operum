import { LineChart } from "@mantine/charts";
import {
    ActionIcon,
    Button,
    Group,
    Paper,
    ScrollArea,
    SimpleGrid,
    Skeleton,
    Stack,
    Text,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiSettings } from "react-icons/fi";
import { MdLink } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import {
    NumericChartAnalyticResultDto,
    SingleValueAnalyticResultDto,
} from "../model/AnalyticResultDto";
import { TrackerDto } from "../model/TrackerDto";
import globalStore from "../stores/GlobalStore";
import EntryDetailsDialog from "./EntryDetailsDialog";
import TrackerAnalyticsDialog from "./TrackerAnalyticsDialog";

const StatCard = ({
    analytic,
    tracker,
    onEntryClick,
}: {
    analytic: SingleValueAnalyticResultDto;
    tracker: TrackerDto;
    onEntryClick: (entryId: string) => void;
}) => {
    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group justify="space-between" align="center">
                    <Text size="sm" c="dimmed" fw={500}>
                        {`${analytic.name}: ${analytic.fieldName}`}
                    </Text>
                    {analytic.entryId && (
                        <ActionIcon
                            color={tracker.color}
                            onClick={() => onEntryClick(analytic.entryId!)}
                        >
                            <MdLink size={18} />
                        </ActionIcon>
                    )}
                </Group>
                <Text
                    size="xl"
                    fw={600}
                    style={{ wordBreak: "break-word", lineHeight: 1.2 }}
                >
                    {analytic.value}
                </Text>
            </Stack>
        </Paper>
    );
};

const ChartCard = ({
    analytic,
}: {
    analytic: NumericChartAnalyticResultDto;
}) => {
    const { tracker } = useTracker();

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Text size="sm" c="dimmed" mb="sm">
                    {`${analytic.name}: ${analytic.xFieldName} - ${analytic.yFieldName}`}
                </Text>
                <LineChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    gridProps={{ xAxisId: "bottom", yAxisId: "left" }}
                    data={analytic.points}
                    dataKey="x"
                    h={"300"}
                    series={[
                        {
                            name: "y",
                            color: tracker.color,
                            label: analytic.yFieldName,
                        },
                    ]}
                />
            </Stack>
        </Paper>
    );
};

export default function AnalyticsList() {
    const { analytics, refreshAnalyticsIfDirty, selectedViewId, tracker } =
        useTracker();
    const [isLoadingData, setIsLoadingData] = useState(false);
    const [openDialogType, setOpenDialogType] = useState<
        "configureAnalytics" | undefined
    >(undefined);
    const [selectedEntryId, setSelectedEntryId] = useState<string>();

    useEffect(() => {
        const loadData = async () => {
            setIsLoadingData(true);
            await refreshAnalyticsIfDirty();
            setIsLoadingData(false);
        };
        loadData();
    }, [selectedViewId]);

    return (
        <>
            <Stack gap="md" h="100%">
                {globalStore.currentUser?.id === tracker.ownerId && (
                    <Group justify="flex-start" w="100%">
                        <Button
                            color={tracker.color}
                            onClick={() =>
                                setOpenDialogType("configureAnalytics")
                            }
                            variant="outline"
                            leftSection={<FiSettings size={16} />}
                        >
                            Configure
                        </Button>
                    </Group>
                )}

                {!analytics ? (
                    <Paper withBorder p="xl" radius="md">
                        <Stack gap="md" align="center">
                            <Text size="lg" fw={500} c="dimmed">
                                No Analytics Available
                            </Text>
                            <Text ta="center" c="dimmed">
                                Analytics will appear here once you have data
                                entries for your tracker.
                            </Text>
                        </Stack>
                    </Paper>
                ) : (
                    <Skeleton visible={isLoadingData} h="100%" w="100%">
                        <ScrollArea flex={1} mih={0}>
                            {/* Masonry Grid Container */}
                            <Stack gap="md">
                                {/* Single value analytics grid */}
                                {analytics.singleValueAnalytics.length > 0 && (
                                    <SimpleGrid
                                        cols={{
                                            base: 1,
                                            xs: 2,
                                            sm: 3,
                                            md: 4,
                                            lg: 5,
                                        }}
                                        spacing="md"
                                    >
                                        {analytics.singleValueAnalytics.map(
                                            (analytic) => (
                                                <StatCard
                                                    key={analytic.analyticId}
                                                    analytic={
                                                        analytic as SingleValueAnalyticResultDto
                                                    }
                                                    tracker={tracker}
                                                    onEntryClick={
                                                        setSelectedEntryId
                                                    }
                                                />
                                            )
                                        )}
                                    </SimpleGrid>
                                )}

                                {/* Numeric chart analytics grid */}
                                {analytics?.numericChartAnalytics.length >
                                    0 && (
                                    <SimpleGrid
                                        cols={{ base: 1, md: 2 }}
                                        spacing="md"
                                    >
                                        {analytics.numericChartAnalytics.map(
                                            (analytic) => (
                                                <ChartCard
                                                    key={analytic.analyticId}
                                                    analytic={analytic}
                                                />
                                            )
                                        )}
                                    </SimpleGrid>
                                )}
                            </Stack>
                        </ScrollArea>
                    </Skeleton>
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
                <TrackerAnalyticsDialog
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
        </>
    );
}
