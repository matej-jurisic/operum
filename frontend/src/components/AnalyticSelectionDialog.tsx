import {
    Accordion,
    Badge,
    Button,
    Divider,
    Group,
    Modal,
    Skeleton,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { AnalyticDto } from "../model/AnalyticDto";
import { DataTypeColor, FieldType } from "../model/constants/DataTypes";
import TrackerAnalyticFormDialog from "./TrackerAnalyticFormDialog";

interface Props {
    onClose: () => void;
}

function isFieldType(value: string): value is FieldType {
    return value in DataTypeColor;
}

export default function AnalyticSelectionDialog({ onClose }: Props) {
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const [selectedAnalytic, setSelectedAnalytic] = useState<AnalyticDto>();
    const [loading, setLoading] = useState(true);
    const { tracker } = useTracker();

    const fetchAnalytics = async () => {
        try {
            setLoading(true);
            const response = await api.get("/analytics");
            setAnalytics(response.data.data);
        } finally {
            setLoading(false);
        }
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
                {loading ? (
                    <Skeleton height={50} radius="sm" />
                ) : (
                    <Accordion
                        variant="contained"
                        chevronPosition="left"
                        transitionDuration={0}
                    >
                        {Object.entries(groupedAnalytics).map(
                            ([name, analyticGroup]) => (
                                <Accordion.Item key={name} value={name}>
                                    <Accordion.Control>
                                        <Group gap="md">
                                            <Text fw={500}>{name}</Text>
                                            <Badge
                                                size="sm"
                                                variant="light"
                                                color={tracker.color}
                                            >
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
                                                        wrap="nowrap"
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
                                                                        <Group
                                                                            gap={
                                                                                5
                                                                            }
                                                                            wrap="nowrap"
                                                                        >
                                                                            <Text>
                                                                                {
                                                                                    r.purpose
                                                                                }{" "}
                                                                                -
                                                                            </Text>
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
                                                                                    r.type
                                                                                }
                                                                            </Badge>
                                                                        </Group>
                                                                    )
                                                                )}
                                                            </Group>
                                                        </Stack>
                                                        <Button
                                                            miw={"max-content"}
                                                            size="xs"
                                                            variant="outline"
                                                            color={
                                                                tracker.color
                                                            }
                                                            onClick={() =>
                                                                setSelectedAnalytic(
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

            {selectedAnalytic && (
                <TrackerAnalyticFormDialog
                    onClose={() => setSelectedAnalytic(undefined)}
                    selectedAnalytic={selectedAnalytic}
                />
            )}
        </Modal>
    );
}
