import { ActionIcon, Group, Paper, Stack, Text } from "@mantine/core";
import { MdDelete, MdLink } from "react-icons/md";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { SingleValueAnalyticResultDto } from "../types/AnalyticDto";

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
    const { tracker } = useTracker();
    const { removeAnalytic } = useTrackerOperations();
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
                                onClick={() => removeAnalytic(analytic.id)}
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
