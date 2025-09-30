import { LineChart } from "@mantine/charts";
import {
    ActionIcon,
    Box,
    em,
    Group,
    Paper,
    ScrollArea,
    SimpleGrid,
    Skeleton,
    Stack,
    Text,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import { FiSettings } from "react-icons/fi";
import { MdAdd, MdDelete, MdLink } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import {
    NumericChartAnalyticResultDto,
    SingleValueAnalyticResultDto,
} from "../model/AnalyticResultDto";
import { FieldTypes } from "../model/constants/DataTypes";
import globalStore from "../stores/GlobalStore";
import { formatBoolean, formatMinutesToTime } from "../util/TypeFormatter";
import AnalyticSelectionDialog from "./AnalyticSelectionDialog";
import EntryDetailsDialog from "./EntryDetailsDialog";

const StatCard = ({
    analytic,
    onEntryClick,
    isConfiguring,
}: {
    analytic: SingleValueAnalyticResultDto;
    onEntryClick: (entryId: string) => void;
    isConfiguring: boolean;
}) => {
    const { tracker, RemoveAnalyticFromTracker } = useTracker();

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group
                    justify="space-between"
                    align="center"
                    mih={28}
                    wrap="nowrap"
                >
                    <Text size="sm" c="dimmed" fw={500}>
                        {`${analytic.name}: ${analytic.fieldName}`}
                    </Text>
                    <Group>
                        {analytic.entryId && (
                            <ActionIcon
                                color={tracker.color}
                                onClick={() => onEntryClick(analytic.entryId!)}
                            >
                                <MdLink size={18} />
                            </ActionIcon>
                        )}
                        {isConfiguring && (
                            <ActionIcon
                                size={"md"}
                                color={tracker.color}
                                variant="outline"
                                onClick={() =>
                                    RemoveAnalyticFromTracker(
                                        analytic.trackerAnalyticId
                                    )
                                }
                            >
                                <MdDelete size={18} />
                            </ActionIcon>
                        )}
                    </Group>
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

const getYAxisFormatter = (fieldType: string) => {
    if (fieldType === FieldTypes.TimeSpan) {
        return formatMinutesToTime;
    }
    if (fieldType === FieldTypes.Bool) {
        return formatBoolean;
    }
    return undefined;
};

const createTooltipContent = (
    fieldType: string,
    fieldName: string,
    color: string
) => {
    return ({ payload, label }: any) => {
        if (!payload?.[0]) return null;
        const value = payload[0].value as number;
        let formatted;
        if (fieldType === FieldTypes.TimeSpan)
            formatted = formatMinutesToTime(value);
        else if (fieldType === FieldTypes.Bool)
            formatted = formatBoolean(value);
        else formatted = value;

        return (
            <Paper p="sm" shadow="sm" withBorder>
                <Text size="sm" c="dimmed" mb="xs">
                    {label}
                </Text>
                <Group gap="xs">
                    <Box
                        w={10}
                        h={10}
                        style={{ borderRadius: "50%" }}
                        bg={color}
                    />
                    <Text size="sm" c="white">
                        {fieldName}
                    </Text>
                    <Text size="sm" c="white" ml="auto">
                        {formatted}
                    </Text>
                </Group>
            </Paper>
        );
    };
};

const ChartCard = ({
    analytic,
    isConfiguring,
}: {
    analytic: NumericChartAnalyticResultDto;
    isConfiguring: boolean;
}) => {
    const { tracker, RemoveAnalyticFromTracker } = useTracker();
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Text size="sm" mb="sm">
                        {`${analytic.name}: ${analytic.xFieldName} - ${analytic.yFieldName}`}
                    </Text>
                    {isConfiguring && (
                        <ActionIcon
                            size={"md"}
                            color={tracker.color}
                            variant="outline"
                            onClick={() =>
                                RemoveAnalyticFromTracker(
                                    analytic.trackerAnalyticId
                                )
                            }
                        >
                            <MdDelete size={18} />
                        </ActionIcon>
                    )}
                </Group>
                <LineChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    gridProps={{ xAxisId: "bottom", yAxisId: "left" }}
                    data={analytic.points}
                    dataKey="x"
                    h={isMobile ? "210" : "260"}
                    series={[
                        {
                            name: "y",
                            color: tracker.color,
                            label: analytic.yFieldName,
                        },
                    ]}
                    yAxisProps={{
                        tickFormatter: getYAxisFormatter(analytic.yFieldType),
                    }}
                    tooltipProps={{
                        content: createTooltipContent(
                            analytic.yFieldType,
                            analytic.yFieldName,
                            tracker.color ?? "blue"
                        ),
                    }}
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

    return (
        <>
            <Stack gap="md" h="100%">
                {globalStore.currentUser?.id === tracker.ownerId && (
                    <Group justify="flex-end" w="100%">
                        {/* <Button
                            color={tracker.color}
                            onClick={() =>
                                setOpenDialogType("configureAnalytics")
                            }
                            variant="outline"
                            leftSection={<FiSettings size={16} />}
                        >
                            Configure
                        </Button> */}
                        <ActionIcon
                            size={"lg"}
                            variant={isConfiguring ? "filled" : "outline"}
                            color={tracker.color}
                            onClick={() => setIsConfiguring((prev) => !prev)}
                        >
                            <FiSettings size={18} />
                        </ActionIcon>
                    </Group>
                )}

                {!analytics ||
                (!isConfiguring &&
                    analytics.numericChartAnalytics.length +
                        analytics.singleValueAnalytics.length ===
                        0) ? (
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
                    <Skeleton visible={isLoadingData} h="100%" w="100%">
                        <ScrollArea flex={1} mih={0}>
                            {/* Masonry Grid Container */}
                            <Stack gap="md" pb={"md"}>
                                {/* Single value analytics grid */}
                                {(isConfiguring ||
                                    analytics.singleValueAnalytics.length >
                                        0) && (
                                    <SimpleGrid
                                        cols={{
                                            base: 1,
                                            xs: 2,
                                            sm: 3,
                                            md: 3,
                                            lg: 4,
                                        }}
                                        spacing="md"
                                    >
                                        {isConfiguring && (
                                            <Paper
                                                withBorder
                                                p="md"
                                                radius="md"
                                            >
                                                <Stack
                                                    justify="center"
                                                    align="center"
                                                >
                                                    <ActionIcon
                                                        variant="outline"
                                                        onClick={() =>
                                                            setOpenDialogType(
                                                                "configureAnalytics"
                                                            )
                                                        }
                                                        size={"xl"}
                                                        color={tracker.color}
                                                    >
                                                        <MdAdd size={20} />
                                                    </ActionIcon>
                                                </Stack>
                                            </Paper>
                                        )}
                                        {analytics.singleValueAnalytics.map(
                                            (analytic) => (
                                                <StatCard
                                                    key={analytic.analyticId}
                                                    analytic={
                                                        analytic as SingleValueAnalyticResultDto
                                                    }
                                                    onEntryClick={
                                                        setSelectedEntryId
                                                    }
                                                    isConfiguring={
                                                        isConfiguring
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
                                        cols={{ base: 1, md: 2, xl: 3 }}
                                        spacing="md"
                                    >
                                        {analytics.numericChartAnalytics.map(
                                            (analytic) => (
                                                <ChartCard
                                                    key={analytic.analyticId}
                                                    analytic={analytic}
                                                    isConfiguring={
                                                        isConfiguring
                                                    }
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
                <AnalyticSelectionDialog
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
        </>
    );
}
