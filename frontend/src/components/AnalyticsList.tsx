import {
    Badge,
    Divider,
    Group,
    Paper,
    SimpleGrid,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect } from "react";
import { useTracker } from "../context/TrackerContext";
import { TrackerDto } from "../model/TrackerDto";
import {
    formatDateOnly,
    formatDateTime,
    formatTimeSpan,
} from "../util/TypeFormatter";

interface AnalyticsListProps {
    tracker: TrackerDto;
}

const StatCard = ({
    label,
    value,
}: {
    label: string;
    value: number | string;
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
            onMouseEnter={(e) => {
                e.currentTarget.style.transform = "translateY(-2px)";
                e.currentTarget.style.boxShadow = "0 4px 8px rgba(0,0,0,0.1)";
            }}
            onMouseLeave={(e) => {
                e.currentTarget.style.transform = "translateY(0)";
                e.currentTarget.style.boxShadow = "";
            }}
        >
            <Stack gap="xs">
                <Text size="sm" c="dimmed" fw={500}>
                    {label}
                </Text>
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
    trackerColor,
}: {
    analytic: any;
    trackerColor: string | undefined;
}) => {
    const allStats = [];

    // Add all available stats
    if (analytic.min != null) {
        allStats.push({ label: "Minimum", value: analytic.min.toFixed(2) });
    }
    if (analytic.max != null) {
        allStats.push({ label: "Maximum", value: analytic.max.toFixed(2) });
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
        });
    }
    if (analytic.maxDateTime) {
        allStats.push({
            label: "Latest",
            value: formatDateTime(analytic.maxDateTime),
        });
    }
    if (analytic.minDate) {
        allStats.push({
            label: "Earliest",
            value: formatDateOnly(analytic.minDate),
        });
    }
    if (analytic.maxDate) {
        allStats.push({
            label: "Latest",
            value: formatDateOnly(analytic.maxDate),
        });
    }
    if (analytic.minTimeSpan) {
        allStats.push({
            label: "Shortest",
            value: formatTimeSpan(analytic.minTimeSpan),
        });
    }
    if (analytic.maxTimeSpan) {
        allStats.push({
            label: "Longest",
            value: formatTimeSpan(analytic.maxTimeSpan),
        });
    }
    if (analytic.averageTimeSpan) {
        allStats.push({
            label: "Average",
            value: formatTimeSpan(analytic.averageTimeSpan),
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
            value: `${(analytic.truePercentage * 100).toFixed(1)}%`,
        });
    }

    if (allStats.length === 0) {
        return (
            <Paper withBorder p="lg" radius="md">
                <Stack gap="md" align="center">
                    <Title c={trackerColor} order={4}>
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
        <Paper withBorder p="lg" radius="md">
            <Stack gap="lg">
                <Group align="center">
                    <Title c={trackerColor} order={4}>
                        {analytic.fieldName}
                    </Title>
                    <Badge variant="outline" color={trackerColor}>
                        {analytic.fieldType}
                    </Badge>
                    <Badge variant="light" color={trackerColor}>
                        {allStats.length} stat
                        {allStats.length !== 1 ? "s" : ""}
                    </Badge>
                </Group>

                <Divider />

                <SimpleGrid
                    cols={{ base: 1, xs: 2, sm: 2, md: 3, lg: 4 }}
                    spacing="md"
                    verticalSpacing="md"
                >
                    {allStats.map((stat, index) => (
                        <StatCard
                            key={`${stat.label}-${index}`}
                            label={stat.label}
                            value={stat.value}
                        />
                    ))}
                </SimpleGrid>
            </Stack>
        </Paper>
    );
};

export default function AnalyticsList(props: AnalyticsListProps) {
    const { analytics, refreshAnalyticsIfDirty } = useTracker();

    useEffect(() => {
        refreshAnalyticsIfDirty();
    }, []);

    if (!analytics || analytics.length === 0) {
        return (
            <Paper withBorder p="xl" radius="md">
                <Stack gap="md" align="center">
                    <Text size="lg" fw={500} c="dimmed">
                        No Analytics Available
                    </Text>
                    <Text ta="center" c="dimmed">
                        Analytics will appear here once you have data entries
                        for your tracker fields.
                    </Text>
                </Stack>
            </Paper>
        );
    }

    return (
        <Stack gap="xl">
            {analytics.map((analytic, index) => (
                <AnalyticsSection
                    key={`${analytic.fieldName}-${index}`}
                    analytic={analytic}
                    trackerColor={props.tracker.color}
                />
            ))}
        </Stack>
    );
}
