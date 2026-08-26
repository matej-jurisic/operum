import { ActionIcon, Group, Paper, Stack, Text, ThemeIcon } from "@mantine/core";
import { MdDelete, MdEdit } from "react-icons/md";
import { TbTable } from "react-icons/tb";
import { EntriesWidgetDefinitionDto } from "../types/WidgetDto";

interface Props {
    entriesWidget: EntriesWidgetDefinitionDto;
    color: string;
    onEdit: () => void;
    onDelete: () => void;
}

/** One Entries table definition in the Library grid. */
export function EntriesWidgetLibraryCard({ entriesWidget, color, onEdit, onDelete }: Props) {
    const title = entriesWidget.name || entriesWidget.trackerName;

    return (
        <Paper withBorder p="md" radius="md">
            <Group justify="space-between" wrap="nowrap" align="flex-start">
                <Group gap="sm" wrap="nowrap" style={{ minWidth: 0, flex: 1 }}>
                    <ThemeIcon size={36} radius="md" variant="light" color={color}>
                        <TbTable size={20} />
                    </ThemeIcon>
                    <Stack gap={0} style={{ minWidth: 0 }}>
                        <Text fw={600} truncate title={title}>
                            {title}
                        </Text>
                        <Text size="xs" c="dimmed">
                            {entriesWidget.trackerName}
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
        </Paper>
    );
}
