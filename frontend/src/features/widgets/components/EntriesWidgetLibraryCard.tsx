import { ActionIcon, Button, Card, Group, Menu, Stack, Text, ThemeIcon } from "@mantine/core";
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

/** Clicking the card or its Add button places it on the current board; edit/delete live in the corner menu. */
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
    const hasCustomName =
        Boolean(entriesWidget.name) && entriesWidget.name !== entriesWidget.trackerName;

    return (
        <Card
            ref={ref}
            withBorder
            radius="md"
            padding={isMobile ? "sm" : "md"}
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
                transition: "border-color 150ms ease",
                borderColor: hovered
                    ? `var(--mantine-color-${color}-filled)`
                    : undefined,
            }}
        >
            <Stack gap="xs" style={{ height: "100%" }}>
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <ThemeIcon size={38} radius="md" variant="light" color={color}>
                        <TbTable size={20} />
                    </ThemeIcon>
                    <Menu position="bottom-end" withinPortal width={190}>
                        <Menu.Target>
                            <ActionIcon
                                variant="subtle"
                                color="gray"
                                aria-label="Entries table actions"
                                onClick={(event) => event.stopPropagation()}
                            >
                                <MdMoreVert size={18} />
                            </ActionIcon>
                        </Menu.Target>
                        <Menu.Dropdown onClick={(event) => event.stopPropagation()}>
                            <Menu.Item leftSection={<MdAdd size={16} />} onClick={onAdd}>
                                Add to dashboard
                            </Menu.Item>
                            <Menu.Item leftSection={<MdEdit size={16} />} onClick={onEdit}>
                                Edit
                            </Menu.Item>
                            <Menu.Item
                                color="red"
                                leftSection={<MdDelete size={16} />}
                                onClick={onDelete}
                            >
                                Delete
                            </Menu.Item>
                        </Menu.Dropdown>
                    </Menu>
                </Group>

                <div style={{ flex: 1, minWidth: 0 }}>
                    <Text fw={600} lineClamp={2} title={title}>
                        {title}
                    </Text>
                    <Text size="xs" c="dimmed" lineClamp={2} mt={2}>
                        {hasCustomName
                            ? `Entries table  ·  ${entriesWidget.trackerName}`
                            : "Entries table"}
                    </Text>
                </div>

                <Button
                    variant={hovered ? "light" : "subtle"}
                    size="compact-sm"
                    color={color}
                    leftSection={<MdAdd size={14} />}
                    onClick={(event) => {
                        event.stopPropagation();
                        onAdd();
                    }}
                    style={{ alignSelf: "flex-start" }}
                >
                    Add to dashboard
                </Button>
            </Stack>
        </Card>
    );
}
