import { Paper, SimpleGrid, Stack, Text, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import api from "../api/api";
import { FieldAnalyticsDto } from "../model/SingleFieldNumericAnalyticsDto";
import { TrackerDto } from "../model/TrackerDto";
import {
    formatDateOnly,
    formatDateTime,
    formatTimeSpan,
} from "../util/TypeFormatter";

interface AnalyticsListProps {
    tracker: TrackerDto;
    isActive: boolean;
}

const GetTrackerAnalytics = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/analytics`);
    return response.data.data;
};

const StatCard = ({
    label,
    value,
}: {
    label: string;
    value: number | string;
}) => (
    <Paper withBorder p="md" radius="md">
        <Stack gap="xs">
            <Text size="sm" c="dimmed" fw={500}>
                {label}
            </Text>
            <Text size="xl">
                {typeof value === "number" ? value.toLocaleString() : value}
            </Text>
        </Stack>
    </Paper>
);

export default function AnalyticsList(props: AnalyticsListProps) {
    const [analytics, setAnalytics] = useState<FieldAnalyticsDto[]>([]);

    useEffect(() => {
        const GetData = async () => {
            setAnalytics(await GetTrackerAnalytics(props.tracker.id));
        };

        GetData();
    }, [props.tracker.id]);

    return (
        <Stack gap="lg">
            {analytics.map((analytic) => (
                <Stack gap="lg">
                    <Title c={props.tracker.color} order={4}>
                        {analytic.fieldName}
                    </Title>
                    <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="md">
                        {analytic.min != null && (
                            <StatCard
                                label="Minimum"
                                value={analytic.min.toFixed(2)}
                            />
                        )}
                        {analytic.max != null && (
                            <StatCard
                                label="Maximum"
                                value={analytic.max.toFixed(2)}
                            />
                        )}
                        {analytic.average != null && (
                            <StatCard
                                label="Average"
                                value={analytic.average.toFixed(2)}
                            />
                        )}
                        {analytic.sum != null && (
                            <StatCard
                                label="Sum"
                                value={analytic.sum.toFixed(2)}
                            />
                        )}
                        {analytic.stdDev != null && (
                            <StatCard
                                label="Standard Deviation"
                                value={analytic.stdDev.toFixed(2)}
                            />
                        )}
                        {analytic.count != null && (
                            <StatCard
                                label="Total Count"
                                value={analytic.count}
                            />
                        )}

                        {analytic.minDateTime && (
                            <StatCard
                                label="Earliest"
                                value={formatDateTime(analytic.minDateTime)}
                            />
                        )}
                        {analytic.maxDateTime && (
                            <StatCard
                                label="Latest"
                                value={formatDateTime(analytic.maxDateTime)}
                            />
                        )}

                        {analytic.minDate && (
                            <StatCard
                                label="Earliest"
                                value={formatDateOnly(analytic.minDate)}
                            />
                        )}
                        {analytic.maxDate && (
                            <StatCard
                                label="Latest"
                                value={formatDateOnly(analytic.maxDate)}
                            />
                        )}

                        {analytic.minTimeSpan && (
                            <StatCard
                                label="Shortest"
                                value={formatTimeSpan(analytic.minTimeSpan)}
                            />
                        )}
                        {analytic.maxTimeSpan && (
                            <StatCard
                                label="Longest"
                                value={formatTimeSpan(analytic.maxTimeSpan)}
                            />
                        )}
                        {analytic.averageTimeSpan && (
                            <StatCard
                                label="Average"
                                value={formatTimeSpan(analytic.averageTimeSpan)}
                            />
                        )}

                        {analytic.trueCount != null && (
                            <StatCard
                                label="True Count"
                                value={analytic.trueCount}
                            />
                        )}
                        {analytic.falseCount != null && (
                            <StatCard
                                label="False Count"
                                value={analytic.falseCount}
                            />
                        )}
                        {analytic.truePercentage != null && (
                            <StatCard
                                label="True Percentage"
                                value={`${analytic.truePercentage.toFixed(2)}%`}
                            />
                        )}
                    </SimpleGrid>
                </Stack>
            ))}
        </Stack>
    );
}
