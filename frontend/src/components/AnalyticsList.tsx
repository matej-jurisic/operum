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
import { SingleFieldNumericAnalyticsDto } from "../model/SingleFieldNumericAnalyticsDto";
import { TrackerDto } from "../model/TrackerDto";

interface AnalyticsListProps {
    tracker: TrackerDto;
}

const GetSingleFieldAnalytics = async (trackerId: string, fieldId: string) => {
    const response = await api.get(
        `/trackers/${trackerId}/fields/${fieldId}/analytics/numeric`
    );
    return response.data.data;
};

export default function AnalyticsList(props: AnalyticsListProps) {
    const [numericAnalytics, setNumericAnalytics] =
        useState<SingleFieldNumericAnalyticsDto>();
    const [selectedField, setSelectedField] = useState<string | null>(null);

    useEffect(() => {
        const GetData = async () => {
            if (!selectedField) return;
            setNumericAnalytics(
                await GetSingleFieldAnalytics(props.tracker.id, selectedField)
            );
        };

        if (selectedField) {
            GetData();
        } else {
            setNumericAnalytics(undefined);
        }
    }, [selectedField, props.tracker.id]);

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
                    onChange={(value) => {
                        setSelectedField(value);
                    }}
                    placeholder="Select a field to analyze"
                    searchable
                    clearable
                />
            </Stack>

            {numericAnalytics && (
                <Stack gap="lg">
                    <Group justify="space-between" align="center">
                        <Title c={props.tracker.color} order={4}>
                            Numeric Statistics
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
                        <StatCard
                            label="Minimum"
                            value={Number(numericAnalytics.min).toFixed(2)}
                        />
                        <StatCard
                            label="Average"
                            value={Number(numericAnalytics.average).toFixed(2)}
                        />
                        <StatCard
                            label="Maximum"
                            value={Number(numericAnalytics.max).toFixed(2)}
                        />
                        <StatCard
                            label="Total Count"
                            value={Number(numericAnalytics.count).toFixed(2)}
                        />
                        <StatCard
                            label="Standard Deviation"
                            value={Number(numericAnalytics.stdDev).toFixed(2)}
                        />
                    </SimpleGrid>
                </Stack>
            )}
        </Stack>
    );
}
