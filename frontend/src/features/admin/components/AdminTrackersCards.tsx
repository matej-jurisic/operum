import { Badge, Card, Group, Stack, Text } from "@mantine/core";
import { AdminTrackerDto } from "../types/AdminTrackerDto";

interface Props {
    trackers: AdminTrackerDto[];
}

export function AdminTrackersCards({ trackers }: Props) {
    return (
        <Stack gap="md">
            {trackers.map((tracker) => (
                <Card key={tracker.id} withBorder radius="md" p="md">
                    <Stack gap="sm">
                        <Group gap="sm" wrap="nowrap">
                            {tracker.color && (
                                <div
                                    style={{
                                        width: 4,
                                        minHeight: 32,
                                        borderRadius: 2,
                                        background: tracker.color,
                                        flexShrink: 0,
                                    }}
                                />
                            )}
                            <Stack gap={2} flex={1}>
                                <Text fw={500}>{tracker.name}</Text>
                                {tracker.description && (
                                    <Text size="xs" c="dimmed" lineClamp={1}>
                                        {tracker.description}
                                    </Text>
                                )}
                                <Text size="xs" c="dimmed">
                                    Owner: {tracker.ownerName}
                                </Text>
                            </Stack>
                        </Group>
                        <Group gap="xs" wrap="wrap">
                            <Badge variant="light" color="blue">
                                {tracker.fieldCount} fields
                            </Badge>
                            <Badge variant="light" color="teal">
                                {tracker.entryCount} entries
                            </Badge>
                            <Badge variant="light" color="violet">
                                {tracker.collaboratorCount} collaborators
                            </Badge>
                        </Group>
                    </Stack>
                </Card>
            ))}
        </Stack>
    );
}
