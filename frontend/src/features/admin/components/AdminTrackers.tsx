import {
    Badge,
    Group,
    Paper,
    ScrollArea,
    Skeleton,
    Stack,
    Table,
    Text,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { adminController } from "../api/adminController";
import { AdminTrackerDto } from "../types/AdminTrackerDto";

export default function AdminTrackers() {
    const [trackers, setTrackers] = useState<AdminTrackerDto[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const load = async () => {
            setLoading(true);
            const response = await adminController.getAllTrackers();
            setTrackers(response.data);
            setLoading(false);
        };
        load();
    }, []);

    return (
        <Skeleton visible={loading} h="100%">
            <Stack gap="md" h="100%">
                <ScrollArea flex={1}>
                    {!loading && trackers.length === 0 && (
                        <Paper withBorder p="xl" radius="md">
                            <Stack gap="md" align="center">
                                <Text size="lg" fw={500} c="dimmed">
                                    No Trackers
                                </Text>
                                <Text ta="center" c="dimmed">
                                    No trackers have been created yet.
                                </Text>
                            </Stack>
                        </Paper>
                    )}
                    {!loading && trackers.length > 0 && (
                        <Table.ScrollContainer minWidth={0}>
                            <Table
                                striped
                                highlightOnHover
                                withColumnBorders
                                withTableBorder
                                verticalSpacing="sm"
                            >
                                <Table.Thead>
                                    <Table.Tr>
                                        <Table.Th>Tracker</Table.Th>
                                        <Table.Th>Owner</Table.Th>
                                        <Table.Th>Fields</Table.Th>
                                        <Table.Th>Entries</Table.Th>
                                        <Table.Th>Collaborators</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {trackers.map((tracker) => (
                                        <Table.Tr key={tracker.id}>
                                            <Table.Td>
                                                <Group gap="sm" wrap="nowrap">
                                                    {tracker.color && (
                                                        <div
                                                            style={{
                                                                width: 4,
                                                                minHeight: 32,
                                                                borderRadius: 2,
                                                                background:
                                                                    tracker.color,
                                                                flexShrink: 0,
                                                            }}
                                                        />
                                                    )}
                                                    <div>
                                                        <Text fw={500}>
                                                            {tracker.name}
                                                        </Text>
                                                        {tracker.description && (
                                                            <Text
                                                                size="xs"
                                                                c="dimmed"
                                                                lineClamp={1}
                                                            >
                                                                {
                                                                    tracker.description
                                                                }
                                                            </Text>
                                                        )}
                                                    </div>
                                                </Group>
                                            </Table.Td>
                                            <Table.Td>
                                                <Text size="sm">
                                                    {tracker.ownerName}
                                                </Text>
                                            </Table.Td>
                                            <Table.Td>
                                                <Badge variant="light" color="blue">
                                                    {tracker.fieldCount}
                                                </Badge>
                                            </Table.Td>
                                            <Table.Td>
                                                <Badge variant="light" color="teal">
                                                    {tracker.entryCount}
                                                </Badge>
                                            </Table.Td>
                                            <Table.Td>
                                                <Badge
                                                    variant="light"
                                                    color="violet"
                                                >
                                                    {tracker.collaboratorCount}
                                                </Badge>
                                            </Table.Td>
                                        </Table.Tr>
                                    ))}
                                </Table.Tbody>
                            </Table>
                        </Table.ScrollContainer>
                    )}
                </ScrollArea>
            </Stack>
        </Skeleton>
    );
}
