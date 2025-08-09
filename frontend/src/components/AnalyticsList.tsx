import {
    Group,
    Paper,
    Select,
    SimpleGrid,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import api from "../api/api";
import { FieldDto } from "../model/FieldDto";
import { SingleFieldAnalyticsDto } from "../model/SingleFieldNumericAnalyticsDto";
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

const GetSingleFieldAnalytics = async (trackerId: string, fieldId: string) => {
    const response = await api.get(
        `/trackers/${trackerId}/fields/${fieldId}/analytics/numeric`
    );
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
    const [analytics, setAnalytics] = useState<SingleFieldAnalyticsDto>();
    const [selectedField, setSelectedField] = useState<string | null>(null);

    useEffect(() => {
        if (props.isActive) setSelectedField(null);
    }, [props.isActive]);

    useEffect(() => {
        const GetData = async () => {
            if (!selectedField) return;
            setAnalytics(
                await GetSingleFieldAnalytics(props.tracker.id, selectedField)
            );
        };

        if (selectedField) {
            GetData();
        } else {
            setAnalytics(undefined);
        }
    }, [selectedField, props.tracker.id]);

    return (
        <Stack gap="lg">
            <Stack gap="md">
                <Title c={props.tracker.color} order={4}>
                    Field Analytics
                </Title>
                <Select
                    data={props.tracker.fields.map((field: FieldDto) => ({
                        value: field.id,
                        label: field.name,
                    }))}
                    value={selectedField}
                    onChange={setSelectedField}
                    placeholder="Select a field to analyze"
                    searchable
                    clearable
                />
            </Stack>

            {analytics && (
                <Stack gap="lg">
                    <Group justify="space-between" align="center">
                        <Title c={props.tracker.color} order={4}>
                            Statistics
                        </Title>
                        <Text size="sm" c="dimmed">
                            {
                                props.tracker.fields.find(
                                    (f) => f.id === selectedField
                                )?.name
                            }
                        </Text>
                    </Group>

                    <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="md">
                        {analytics.min != null && (
                            <StatCard
                                label="Minimum"
                                value={analytics.min.toFixed(2)}
                            />
                        )}
                        {analytics.max != null && (
                            <StatCard
                                label="Maximum"
                                value={analytics.max.toFixed(2)}
                            />
                        )}
                        {analytics.average != null && (
                            <StatCard
                                label="Average"
                                value={analytics.average.toFixed(2)}
                            />
                        )}
                        {analytics.stdDev != null && (
                            <StatCard
                                label="Standard Deviation"
                                value={analytics.stdDev.toFixed(2)}
                            />
                        )}
                        {analytics.count != null && (
                            <StatCard
                                label="Total Count"
                                value={analytics.count}
                            />
                        )}

                        {analytics.minDateTime && (
                            <StatCard
                                label="Earliest"
                                value={formatDateTime(analytics.minDateTime)}
                            />
                        )}
                        {analytics.maxDateTime && (
                            <StatCard
                                label="Latest"
                                value={formatDateTime(analytics.maxDateTime)}
                            />
                        )}

                        {analytics.minDate && (
                            <StatCard
                                label="Earliest"
                                value={formatDateOnly(analytics.minDate)}
                            />
                        )}
                        {analytics.maxDate && (
                            <StatCard
                                label="Latest"
                                value={formatDateOnly(analytics.maxDate)}
                            />
                        )}

                        {analytics.minTimeSpan && (
                            <StatCard
                                label="Shortest"
                                value={formatTimeSpan(analytics.minTimeSpan)}
                            />
                        )}
                        {analytics.maxTimeSpan && (
                            <StatCard
                                label="Longest"
                                value={formatTimeSpan(analytics.maxTimeSpan)}
                            />
                        )}
                        {analytics.averageTimeSpan && (
                            <StatCard
                                label="Average"
                                value={formatTimeSpan(
                                    analytics.averageTimeSpan
                                )}
                            />
                        )}

                        {analytics.trueCount != null && (
                            <StatCard
                                label="True Count"
                                value={analytics.trueCount}
                            />
                        )}
                        {analytics.falseCount != null && (
                            <StatCard
                                label="False Count"
                                value={analytics.falseCount}
                            />
                        )}
                        {analytics.truePercentage != null && (
                            <StatCard
                                label="True Percentage"
                                value={`${analytics.truePercentage.toFixed(
                                    2
                                )}%`}
                            />
                        )}
                    </SimpleGrid>
                </Stack>
            )}
        </Stack>
    );
}
