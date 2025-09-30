import {
    Badge,
    Card,
    Grid,
    Group,
    Modal,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import api from "../api/api";
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
    const [searchQuery, setSearchQuery] = useState("");

    const fetchAnalytics = async () => {
        const response = await api.get("/analytics");
        setAnalytics(response.data.data);
    };

    useEffect(() => {
        fetchAnalytics();
    }, []);

    const filteredAnalytics = useMemo(() => {
        if (!searchQuery) return analytics;
        const query = searchQuery.toLowerCase();
        return analytics.filter(
            (analytic) =>
                analytic.name.toLowerCase().includes(query) ||
                analytic.description?.toLowerCase().includes(query) ||
                analytic.analyticTypeName.toLowerCase().includes(query)
        );
    }, [analytics, searchQuery]);

    return (
        <Modal
            opened
            onClose={onClose}
            title="Select an Analytic"
            centered
            size="lg"
        >
            <Stack>
                <TextInput
                    placeholder="Search analytics..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.currentTarget.value)}
                />
                <Grid>
                    {filteredAnalytics.map((analytic) => (
                        <Grid.Col span={6} key={analytic.id}>
                            <Card
                                radius="sm"
                                withBorder
                                onClick={() => onSelect(analytic)}
                                style={{ cursor: "pointer" }}
                            >
                                <Stack gap="sm">
                                    <Group
                                        justify="space-between"
                                        wrap="nowrap"
                                    >
                                        <Text flex={1} fw={500}>
                                            {analytic.name}
                                        </Text>
                                    </Group>
                                    <Text c="dimmed" size="sm" lineClamp={2}>
                                        {analytic.description ||
                                            "No description"}
                                    </Text>
                                    <Group
                                        justify="flex-start"
                                        w="100%"
                                        align="flex-end"
                                    >
                                        {analytic.analyticRequiredDataTypes.map(
                                            (r) => (
                                                <Badge
                                                    color={
                                                        isFieldType(r.type)
                                                            ? DataTypeColor[
                                                                  r.type
                                                              ]
                                                            : "gray"
                                                    }
                                                    key={r.id}
                                                    variant="outline"
                                                >
                                                    {r.purpose} ({r.type})
                                                </Badge>
                                            )
                                        )}
                                    </Group>
                                </Stack>
                            </Card>
                        </Grid.Col>
                    ))}
                </Grid>
                {filteredAnalytics.length === 0 && (
                    <Text c="dimmed" ta="center" py="xl">
                        No analytics found
                    </Text>
                )}
            </Stack>
        </Modal>
    );
}
