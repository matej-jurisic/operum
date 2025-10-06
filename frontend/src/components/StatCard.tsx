import { ActionIcon, Group, Paper, Stack, Text } from "@mantine/core";
import { MdDelete, MdLink } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import { SingleValueAnalyticResultDto } from "../model/AnalyticResultDto";

interface StatCardProps {
    analytic: SingleValueAnalyticResultDto;
    onEntryClick: (entryId: string) => void;
    isConfiguring: boolean;
}

export function StatCard({
    analytic,
    onEntryClick,
    isConfiguring,
}: StatCardProps) {
    const { tracker, RemoveAnalyticFromTracker } = useTracker();
    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group
                    justify="space-between"
                    align="center"
                    mih={28}
                    wrap="nowrap"
                >
                    <Text size="sm" c="dimmed" fw={500}>
                        {`${analytic.name}: ${analytic.fieldName}`}
                    </Text>
                    <Group>
                        {analytic.entryId && (
                            <ActionIcon
                                color={tracker.color}
                                onClick={() => onEntryClick(analytic.entryId!)}
                            >
                                <MdLink size={18} />
                            </ActionIcon>
                        )}
                        {isConfiguring && (
                            <ActionIcon
                                size="md"
                                color={tracker.color}
                                variant="outline"
                                onClick={() =>
                                    RemoveAnalyticFromTracker(
                                        analytic.trackerAnalyticId
                                    )
                                }
                            >
                                <MdDelete size={18} />
                            </ActionIcon>
                        )}
                    </Group>
                </Group>
                <Text
                    size="xl"
                    fw={600}
                    style={{ wordBreak: "break-word", lineHeight: 1.2 }}
                >
                    {analytic.value}
                </Text>
            </Stack>
        </Paper>
    );
}
