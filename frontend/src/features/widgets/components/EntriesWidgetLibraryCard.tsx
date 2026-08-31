import { ActionIcon, Button, Group, Stack, Text, ThemeIcon } from "@mantine/core";
import { useHover } from "@mantine/hooks";
import { MdAdd, MdDelete, MdEdit } from "react-icons/md";
import { TbTable } from "react-icons/tb";
import { EntriesWidgetDefinitionDto } from "../types/WidgetDto";

interface Props {
    entriesWidget: EntriesWidgetDefinitionDto;
    color: string;
    onAdd: () => void;
    onEdit: () => void;
    onDelete: () => void;
}

/** One Entries table definition as a row in the Library list. The whole row adds it to the
    board; edit and delete are the quiet icons on the right. */
export function EntriesWidgetLibraryCard({ entriesWidget, color, onAdd, onEdit, onDelete }: Props) {
    const { hovered, ref } = useHover<HTMLDivElement>();
    const title = entriesWidget.name || entriesWidget.trackerName;
    const hasCustomName = Boolean(entriesWidget.name) && entriesWidget.name !== entriesWidget.trackerName;

    return (
        <Group
            ref={ref}
            wrap="nowrap"
            gap="sm"
            px="sm"
            py="xs"
            role="button"
            tabIndex={0}
            onClick={onAdd}
            onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    onAdd();
                }
            }}
            style={{
                cursor: "pointer",
                borderRadius: "var(--mantine-radius-sm)",
                backgroundColor: hovered ? "var(--mantine-color-default-hover)" : undefined,
            }}
        >
            <ThemeIcon size={34} radius="md" variant="light" color={color}>
                <TbTable size={18} />
            </ThemeIcon>
            <Stack gap={2} style={{ minWidth: 0, flex: 1 }}>
                <Text fw={500} truncate title={title}>
                    {title}
                </Text>
                <Text size="xs" c="dimmed" truncate>
                    {hasCustomName
                        ? `Entries table  ·  ${entriesWidget.trackerName}`
                        : "Entries table"}
                </Text>
            </Stack>

            <Button
                variant={hovered ? "light" : "subtle"}
                size="compact-sm"
                color={color}
                leftSection={<MdAdd size={14} />}
                onClick={(event) => {
                    event.stopPropagation();
                    onAdd();
                }}
            >
                Add
            </Button>
            <ActionIcon
                variant="subtle"
                color="gray"
                aria-label="Edit entries table"
                onClick={(event) => {
                    event.stopPropagation();
                    onEdit();
                }}
            >
                <MdEdit size={16} />
            </ActionIcon>
            <ActionIcon
                variant="subtle"
                color="gray"
                aria-label="Delete entries table"
                onClick={(event) => {
                    event.stopPropagation();
                    onDelete();
                }}
            >
                <MdDelete size={16} />
            </ActionIcon>
        </Group>
    );
}
