import { Paper, SimpleGrid, Skeleton, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { adminController } from "../api/adminController";
import { AdminStatsDto } from "../types/AdminStatsDto";

const statCards = (stats: AdminStatsDto) => [
    { label: "Total Users", value: stats.totalUsers },
    { label: "Total Trackers", value: stats.totalTrackers },
    { label: "Total Entries", value: stats.totalEntries },
    { label: "Entries (Last 30 Days)", value: stats.entriesLast30Days },
];

export default function AdminStats() {
    const [stats, setStats] = useState<AdminStatsDto | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const load = async () => {
            setLoading(true);
            const response = await adminController.getStats();
            setStats(response.data);
            setLoading(false);
        };
        load();
    }, []);

    return (
        <Skeleton visible={loading}>
            <SimpleGrid cols={{ base: 1, sm: 2, md: 4 }}>
                {stats &&
                    statCards(stats).map((card) => (
                        <Paper key={card.label} withBorder p="md" radius="md">
                            <Stack gap="xs">
                                <Text size="sm" c="dimmed" fw={500}>
                                    {card.label}
                                </Text>
                                <Text size="xl" fw={600} style={{ lineHeight: 1.2 }}>
                                    {card.value.toLocaleString()}
                                </Text>
                            </Stack>
                        </Paper>
                    ))}
            </SimpleGrid>
        </Skeleton>
    );
}
