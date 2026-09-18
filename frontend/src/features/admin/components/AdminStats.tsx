import { Card, SimpleGrid, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { adminController } from "../api/adminController";
import { AdminStatsDto } from "../types/AdminStatsDto";

// Colours follow the profile page's stat cards, the app's existing treatment for a row
// of headline numbers.
const statCards = (stats: AdminStatsDto) => [
    { label: "Users", value: stats.totalUsers, color: "blue" },
    { label: "Trackers", value: stats.totalTrackers, color: "teal" },
    { label: "Entries", value: stats.totalEntries, color: "grape" },
    { label: "Entries, Last 30 Days", value: stats.entriesLast30Days, color: "indigo" },
];

export default function AdminStats() {
    const [stats, setStats] = useState<AdminStatsDto | null>(null);

    useEffect(() => {
        const load = async () => {
            const response = await adminController.getStats();
            setStats(response.data);
        };
        load();
    }, []);

    // The global request loader already covers the first fetch, so there is nothing to
    // draw here until the numbers arrive.
    if (!stats) {
        return null;
    }

    return (
        <SimpleGrid cols={{ base: 1, sm: 2, md: 4 }}>
            {statCards(stats).map((card) => (
                <Card
                    key={card.label}
                    withBorder
                    radius="md"
                    p="lg"
                    style={{
                        borderTop: `3px solid var(--mantine-color-${card.color}-5)`,
                    }}
                >
                    <Stack gap={4}>
                        <Text
                            size="xs"
                            c="dimmed"
                            fw={600}
                            tt="uppercase"
                            style={{ letterSpacing: "0.05em" }}
                        >
                            {card.label}
                        </Text>
                        <Text fw={700} size="xl">
                            {card.value.toLocaleString()}
                        </Text>
                    </Stack>
                </Card>
            ))}
        </SimpleGrid>
    );
}
