import { ActionIcon, Button, Group, Menu, Stack, Text, ThemeIcon } from "@mantine/core";
import { useHover } from "@mantine/hooks";
import { MdAdd, MdDelete, MdEdit, MdMoreVert } from "react-icons/md";
import { TbTable } from "react-icons/tb";
import { EntriesWidgetDefinitionDto } from "../types/WidgetDto";

interface Props {
    entriesWidget: EntriesWidgetDefinitionDto;
    color: string;
    isMobile?: boolean;
    onAdd: () => void;
    onEdit: () => void;
    onDelete: () => void;
}

/** One Entries table definition as a row in the Library list. The whole row adds it to the
    board; edit and delete are the quiet icons on the right. */
export function EntriesWidgetLibraryCard({
    entriesWidget,
    color,
    isMobile,
    onAdd,
    onEdit,
    onDelete,
}: Props) {
    const { hovered, ref } = useHover<HTMLDivElement>();
    const title = entriesWidget.name || entriesWidget.trackerName;
    const hasCustomName = Boolean(entriesWidget.name) && entriesWidget.name !== entriesWidget.trackerName;

    return (
        <Group
            ref={ref}
            wrap="nowrap"
            gap="sm"
            px="sm"
            py={isMobile ? "sm" : "xs"}
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

            {isMobile ? (
                // Tapping the row already adds; the actions collapse into one menu so the
                // name gets the width instead of three side-by-side controls. The trigger
                // is a full 44px touch target with its own margin so a near-miss doesn't
                // land on the row and fire "add" instead of opening the menu.
                <Menu position="bottom-end" withinPortal width={200}>
                    <Menu.Target>
                        <ActionIcon
                            variant="subtle"
                            color="gray"
                            size={44}
                            ml={4}
                            style={{ flexShrink: 0 }}
                            aria-label="Entries table actions"
                            onClick={(event) => event.stopPropagation()}
                        >
                            <MdMoreVert size={22} />
                        </ActionIcon>
                    </Menu.Target>
                    <Menu.Dropdown onClick={(event) => event.stopPropagation()}>
                        <Menu.Item py="sm" leftSection={<MdAdd size={16} />} onClick={onAdd}>
                            Add to board
                        </Menu.Item>
                        <Menu.Item py="sm" leftSection={<MdEdit size={16} />} onClick={onEdit}>
                            Edit
                        </Menu.Item>
                        <Menu.Item
                            py="sm"
                            color="red"
                            leftSection={<MdDelete size={16} />}
                            onClick={onDelete}
                        >
                            Delete
                        </Menu.Item>
                    </Menu.Dropdown>
                </Menu>
            ) : (
                <>
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
                </>
            )}
        </Group>
    );
}
