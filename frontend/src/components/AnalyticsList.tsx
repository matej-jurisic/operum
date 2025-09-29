import {
    ActionIcon,
    Badge,
    Collapse,
    Group,
    Paper,
    ScrollArea,
    SimpleGrid,
    Skeleton,
    Stack,
    Text,
    Title,
    UnstyledButton,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiSettings } from "react-icons/fi";
import { MdExpandLess, MdExpandMore, MdLink } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import { FieldAnalyticsDto } from "../model/FieldAnalyticsDto";
import { TrackerDto } from "../model/TrackerDto";
import globalStore from "../stores/GlobalStore";
import {
    formatDateOnly,
    formatDateTime,
    formatTimeSpan,
} from "../util/TypeFormatter";
import EntryDetailsDialog from "./EntryDetailsDialog";
import TrackerAnalyticFormDialog from "./TrackerAnalyticFormDialog";

const StatCard = ({
    label,
    value,
    onClick,
    tracker,
}: {
    label: string;
    value: number | string;
    onClick?: (() => void) | null;
    tracker: TrackerDto;
}) => {
    return (
        <Paper
            withBorder
            p="md"
            radius="md"
            style={{
                transition: "transform 0.2s ease, box-shadow 0.2s ease",
                cursor: "default",
            }}
        >
            <Stack gap="xs">
                <Group justify="space-between" align="center">
                    <Text size="sm" c="dimmed" fw={500}>
                        {label}
                    </Text>
                    {onClick && (
                        <ActionIcon color={tracker.color} onClick={onClick}>
                            <MdLink size={18} />
                        </ActionIcon>
                    )}
                </Group>
                <Text
                    size="xl"
                    fw={600}
                    style={{
                        wordBreak: "break-word",
                        lineHeight: 1.2,
                    }}
                >
                    {typeof value === "number" ? value.toLocaleString() : value}
                </Text>
            </Stack>
        </Paper>
    );
};

const AnalyticsSection = ({
    analytic,
    tracker,
}: {
    analytic: FieldAnalyticsDto;
    tracker: TrackerDto;
}) => {
    const allStats = [];
    const [selectedEntryId, setSelectedEntryId] = useState<string>();
    const [opened, setOpened] = useState(false);

    // Add all available stats
    if (analytic.min != null) {
        allStats.push({
            label: "Minimum",
            value: analytic.min.toFixed(2),
            onClick: () => {
                if (analytic.minEntryId) {
                    setSelectedEntryId(analytic.minEntryId);
                }
            },
        });
    }
    if (analytic.max != null) {
        allStats.push({
            label: "Maximum",
            value: analytic.max.toFixed(2),
            onClick: () => {
                if (analytic.maxEntryId) {
                    setSelectedEntryId(analytic.maxEntryId);
                }
            },
        });
    }
    if (analytic.average != null) {
        allStats.push({ label: "Average", value: analytic.average.toFixed(2) });
    }
    if (analytic.sum != null) {
        allStats.push({ label: "Sum", value: analytic.sum.toFixed(2) });
    }
    if (analytic.stdDev != null) {
        allStats.push({
            label: "Standard Deviation",
            value: analytic.stdDev.toFixed(2),
        });
    }
    if (analytic.count != null) {
        allStats.push({ label: "Total Count", value: analytic.count });
    }
    if (analytic.minDateTime) {
        allStats.push({
            label: "Earliest",
            value: formatDateTime(analytic.minDateTime),
            onClick: () => {
                if (analytic.minDateTimeEntryId) {
                    setSelectedEntryId(analytic.minDateTimeEntryId);
                }
            },
        });
    }
    if (analytic.maxDateTime) {
        allStats.push({
            label: "Latest",
            value: formatDateTime(analytic.maxDateTime),
            onClick: () => {
                if (analytic.maxDateTimeEntryId) {
                    setSelectedEntryId(analytic.maxDateTimeEntryId);
                }
            },
        });
    }
    if (analytic.minDate) {
        allStats.push({
            label: "Earliest",
            value: formatDateOnly(analytic.minDate),
            onClick: () => {
                if (analytic.minDateEntryId) {
                    setSelectedEntryId(analytic.minDateEntryId);
                }
            },
        });
    }
    if (analytic.maxDate) {
        allStats.push({
            label: "Latest",
            value: formatDateOnly(analytic.maxDate),
            onClick: () => {
                if (analytic.maxDateEntryId) {
                    setSelectedEntryId(analytic.maxDateEntryId);
                }
            },
        });
    }
    if (analytic.minTimeSpan) {
        allStats.push({
            label: "Shortest",
            value: formatTimeSpan(analytic.minTimeSpan),
            onClick: () => {
                if (analytic.minTimeSpanEntryId) {
                    setSelectedEntryId(analytic.minTimeSpanEntryId);
                }
            },
        });
    }
    if (analytic.maxTimeSpan) {
        allStats.push({
            label: "Longest",
            value: formatTimeSpan(analytic.maxTimeSpan),
            onClick: () => {
                if (analytic.maxTimeSpanEntryId) {
                    setSelectedEntryId(analytic.maxTimeSpanEntryId);
                }
            },
        });
    }
    if (analytic.averageTimeSpan) {
        allStats.push({
            label: "Average",
            value: formatTimeSpan(analytic.averageTimeSpan),
        });
    }
    if (analytic.sumTimeSpan) {
        allStats.push({
            label: "Sum",
            value: formatTimeSpan(analytic.sumTimeSpan),
        });
    }
    if (analytic.trueCount != null) {
        allStats.push({ label: "True Count", value: analytic.trueCount });
    }
    if (analytic.falseCount != null) {
        allStats.push({ label: "False Count", value: analytic.falseCount });
    }
    if (analytic.truePercentage != null) {
        allStats.push({
            label: "True Percentage",
            value: `${(analytic.truePercentage * 100).toFixed(2)}%`,
        });
    }

    if (allStats.length === 0) {
        return (
            <Paper withBorder p="lg" radius="md">
                <Stack gap="md" align="center">
                    <Title c={tracker.color} order={4}>
                        {analytic.fieldName}
                    </Title>
                    <Text c="dimmed" ta="center">
                        No analytics data available for this field yet.
                    </Text>
                </Stack>
            </Paper>
        );
    }

    return (
        <>
            <Paper withBorder p="lg" radius="md">
                <Stack gap="lg">
                    <UnstyledButton
                        onClick={() => setOpened((o) => !o)}
                        style={{
                            display: "flex",
                            justifyContent: "space-between",
                            alignItems: "center",
                            width: "100%",
                        }}
                    >
                        <Group align="center">
                            <Title c={tracker.color} order={4}>
                                {analytic.fieldName}
                            </Title>
                            <Badge variant="outline" color={tracker.color}>
                                {analytic.fieldType}
                            </Badge>
                            <Badge variant="light" color={tracker.color}>
                                {allStats.length} stat
                                {allStats.length !== 1 ? "s" : ""}
                            </Badge>
                        </Group>
                        {opened ? (
                            <MdExpandLess size={24} />
                        ) : (
                            <MdExpandMore size={24} />
                        )}
                    </UnstyledButton>

                    <Collapse in={opened}>
                        <SimpleGrid
                            cols={{ base: 1, xs: 2, sm: 2, md: 3, lg: 4 }}
                            spacing="md"
                            verticalSpacing="md"
                        >
                            {allStats.map((stat, index) => (
                                <StatCard
                                    tracker={tracker}
                                    key={`${stat.label}-${index}`}
                                    label={stat.label}
                                    value={stat.value}
                                    onClick={stat.onClick}
                                />
                            ))}
                        </SimpleGrid>
                    </Collapse>
                </Stack>
            </Paper>
            {selectedEntryId && (
                <EntryDetailsDialog
                    entryId={selectedEntryId}
                    tracker={tracker}
                    onClose={() => setSelectedEntryId(undefined)}
                />
            )}
        </>
    );
};

enum OpenDialogType {
    ConfigureAnalytics,
}

export default function AnalyticsList() {
    const { analytics, refreshAnalyticsIfDirty, selectedViewId, tracker } =
        useTracker();
    const [isLoadingData, setIsLoadingData] = useState(false);
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    useEffect(() => {
        const loadData = async () => {
            setIsLoadingData(true);
            await refreshAnalyticsIfDirty();
            setIsLoadingData(false);
        };

        loadData();
    }, [selectedViewId]);

    if (!analytics || analytics.length === 0) {
        return (
            <Skeleton visible={isLoadingData} h={"100%"}>
                <Paper withBorder p="xl" radius="md">
                    <Stack gap="md" align="center">
                        <Text size="lg" fw={500} c="dimmed">
                            No Analytics Available
                        </Text>
                        <Text ta="center" c="dimmed">
                            Analytics will appear here once you have data
                            entries for your tracker fields.
                        </Text>
                    </Stack>
                </Paper>
            </Skeleton>
        );
    }

    const sortedAnalytics = [...analytics].sort((a, b) => {
        const aHasStats = Object.keys(a).some(
            (key) => key !== "fieldName" && key !== "fieldType"
        );
        const bHasStats = Object.keys(b).some(
            (key) => key !== "fieldName" && key !== "fieldType"
        );
        return aHasStats === bHasStats ? 0 : aHasStats ? -1 : 1;
    });

    return (
        <>
            <Skeleton visible={isLoadingData} h={"100%"}>
                <Stack gap="md" h={"100%"}>
                    {globalStore.currentUser?.id === tracker.ownerId && (
                        <Group justify="flex-end" w="100%">
                            <ActionIcon
                                color={tracker.color}
                                onClick={() =>
                                    setOpenDialogType(
                                        OpenDialogType.ConfigureAnalytics
                                    )
                                }
                                size={"lg"}
                                variant="outline"
                            >
                                <FiSettings size={18} />
                            </ActionIcon>
                        </Group>
                    )}
                    <ScrollArea flex={1} mih={0}>
                        <Stack gap="md">
                            {sortedAnalytics.map((analytic, index) => (
                                <AnalyticsSection
                                    key={`${analytic.fieldName}-${index}`}
                                    analytic={analytic}
                                    tracker={tracker}
                                />
                            ))}
                        </Stack>
                    </ScrollArea>
                </Stack>
            </Skeleton>
            {openDialogType === OpenDialogType.ConfigureAnalytics && (
                <TrackerAnalyticFormDialog
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
        </>
    );
}
