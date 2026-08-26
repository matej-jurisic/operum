import { ActionIcon, Badge, Group, Paper, Stack, Text, ThemeIcon } from "@mantine/core";
import { MdDelete, MdEdit } from "react-icons/md";
import { TbChartHistogram } from "react-icons/tb";
import { WidgetDto } from "../types/WidgetDto";

interface Props {
    widget: WidgetDto;
    color: string;
    onEdit: () => void;
    onDelete: () => void;
}

/** One chart Widget in the Library grid. There's no calculated preview here (the Library
    manages definitions, not renders) -- see DashboardWidget for the actual chart, drawn
    once a widget is placed on a board. */
export function WidgetCard({ widget, color, onEdit, onDelete }: Props) {
    const trackerNames = [...new Set(widget.sources.map((s) => s.trackerName))];

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="sm">
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Group gap="sm" wrap="nowrap" style={{ minWidth: 0, flex: 1 }}>
                        <ThemeIcon size={36} radius="md" variant="light" color={color}>
                            <TbChartHistogram size={20} />
                        </ThemeIcon>
                        <Stack gap={0} style={{ minWidth: 0 }}>
                            <Text fw={600} truncate title={widget.name}>
                                {widget.name}
                            </Text>
                            <Text size="xs" c="dimmed">
                                {widget.resultType}
                            </Text>
                        </Stack>
                    </Group>
                    <Group gap={4} wrap="nowrap">
                        <ActionIcon size="md" color={color} variant="outline" onClick={onEdit}>
                            <MdEdit size={16} />
                        </ActionIcon>
                        <ActionIcon size="md" color="red" variant="outline" onClick={onDelete}>
                            <MdDelete size={16} />
                        </ActionIcon>
                    </Group>
                </Group>
                <Group gap={4} wrap="wrap">
                    {trackerNames.map((name) => (
                        <Badge key={name} variant="light" color={color} size="sm">
                            {name}
                        </Badge>
                    ))}
                </Group>
            </Stack>
        </Paper>
    );
}
