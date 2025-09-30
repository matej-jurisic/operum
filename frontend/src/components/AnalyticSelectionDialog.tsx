import {
    Accordion,
    Badge,
    Button,
    Divider,
    Group,
    Modal,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { AnalyticDto } from "../model/AnalyticDto";
import { DataTypeColor, FieldType } from "../model/constants/DataTypes";

interface Props {
    onSelect: (analytic: AnalyticDto) => void;
    onClose: () => void;
}

function isFieldType(value: string): value is FieldType {
    return value in DataTypeColor;
}

export default function AnalyticSelectionDialog({ onSelect, onClose }: Props) {
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const { tracker } = useTracker();

    const fetchAnalytics = async () => {
        const response = await api.get("/analytics");
        setAnalytics(response.data.data);
    };

    useEffect(() => {
        fetchAnalytics();
    }, []);

    // Group analytics by name
    const groupedAnalytics = useMemo(() => {
        return analytics.reduce((acc, analytic) => {
            if (!acc[analytic.name]) {
                acc[analytic.name] = [];
            }
            acc[analytic.name].push(analytic);
            return acc;
        }, {} as Record<string, AnalyticDto[]>);
    }, [analytics]);

    return (
        <Modal
            opened
            onClose={onClose}
            title="Select an Analytic"
            centered
            size="lg"
        >
            <Stack>
                {Object.keys(groupedAnalytics).length === 0 ? (
                    <Text c="dimmed" ta="center" py="xl">
                        No analytics found
                    </Text>
                ) : (
                    <Accordion variant="contained" chevronPosition="left">
                        {Object.entries(groupedAnalytics).map(
                            ([name, analyticGroup]) => (
                                <Accordion.Item key={name} value={name}>
                                    <Accordion.Control>
                                        <Group justify="space-between">
                                            <Text fw={500}>{name}</Text>
                                            <Badge size="sm" variant="light">
                                                {analyticGroup.length}{" "}
                                                {analyticGroup.length === 1
                                                    ? "variant"
                                                    : "variants"}
                                            </Badge>
                                        </Group>
                                    </Accordion.Control>
                                    <Accordion.Panel>
                                        <Stack gap="md">
                                            {analyticGroup.map((analytic) => (
                                                <>
                                                    <Divider />
                                                    <Group
                                                        w={"100%"}
                                                        justify="space-between"
                                                    >
                                                        <Stack gap="sm">
                                                            <Title order={4}>
                                                                {`${analytic.name}`}
                                                            </Title>
                                                            {analytic.description && (
                                                                <Text
                                                                    c="dimmed"
                                                                    size="sm"
                                                                    lineClamp={
                                                                        2
                                                                    }
                                                                >
                                                                    {
                                                                        analytic.description
                                                                    }
                                                                </Text>
                                                            )}
                                                            <Group
                                                                justify="flex-start"
                                                                w="100%"
                                                                align="flex-end"
                                                            >
                                                                {analytic.analyticRequiredDataTypes.map(
                                                                    (r) => (
                                                                        <Badge
                                                                            color={
                                                                                isFieldType(
                                                                                    r.type
                                                                                )
                                                                                    ? DataTypeColor[
                                                                                          r
                                                                                              .type
                                                                                      ]
                                                                                    : "gray"
                                                                            }
                                                                            key={
                                                                                r.id
                                                                            }
                                                                            variant="outline"
                                                                        >
                                                                            {
                                                                                r.purpose
                                                                            }{" "}
                                                                            (
                                                                            {
                                                                                r.type
                                                                            }
                                                                            )
                                                                        </Badge>
                                                                    )
                                                                )}
                                                            </Group>
                                                        </Stack>
                                                        <Button
                                                            size="xs"
                                                            variant="outline"
                                                            color={
                                                                tracker.color
                                                            }
                                                            onClick={() =>
                                                                onSelect(
                                                                    analytic
                                                                )
                                                            }
                                                        >
                                                            Select
                                                        </Button>
                                                    </Group>
                                                </>
                                            ))}
                                        </Stack>
                                    </Accordion.Panel>
                                </Accordion.Item>
                            )
                        )}
                    </Accordion>
                )}
            </Stack>
        </Modal>
    );
}
