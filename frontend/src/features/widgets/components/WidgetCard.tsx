import { ActionIcon, Button, Group, Menu, Stack, Text, ThemeIcon } from "@mantine/core";
import { useHover } from "@mantine/hooks";
import { IconType } from "react-icons";
import { MdAdd, MdDelete, MdEdit, MdMoreVert } from "react-icons/md";
import {
    TbCalendar,
    TbChartBar,
    TbChartDonut,
    TbChartDots,
    TbChartHistogram,
    TbChartLine,
    TbNumbers,
} from "react-icons/tb";
import { WidgetDto } from "../types/WidgetDto";

interface Props {
    widget: WidgetDto;
    color: string;
    isMobile?: boolean;
    onAdd: () => void;
    onEdit: () => void;
    onDelete: () => void;
}

/** Maps a chart's result type to the icon for its shape, so the list can be skimmed by
    chart kind. Falls back to the generic histogram glyph for anything unrecognised. */
function resultTypeIcon(resultType: string): IconType {
    switch (resultType) {
        case "Single Value":
            return TbNumbers;
        case "Line Chart":
            return TbChartLine;
        case "Scatter Chart":
            return TbChartDots;
        case "Calendar":
            return TbCalendar;
        case "Donut Chart":
            return TbChartDonut;
        case "Bar Chart":
            return TbChartBar;
        default:
            return TbChartHistogram;
    }
}

/** One chart Widget as a row in the Library list. There's no calculated preview here (the
    Library manages definitions, not renders) -- see DashboardWidget for the actual chart,
    drawn once a widget is placed on a board. The whole row adds the widget to the board;
    edit and delete are the outlined action icons on the right. */
export function WidgetCard({ widget, color, isMobile, onAdd, onEdit, onDelete }: Props) {
    const { hovered, ref } = useHover<HTMLDivElement>();
    const trackerNames = [...new Set(widget.sources.map((s) => s.trackerName))];
    const Icon = resultTypeIcon(widget.resultType);

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
                <Icon size={18} />
            </ThemeIcon>
            <Stack gap={2} style={{ minWidth: 0, flex: 1 }}>
                <Text fw={500} truncate title={widget.name}>
                    {widget.name}
                </Text>
                <Text size="xs" c="dimmed" truncate>
                    {[widget.resultType, ...trackerNames].join("  ·  ")}
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
                            variant="outline"
                            color="gray"
                            size={44}
                            ml={4}
                            style={{ flexShrink: 0 }}
                            aria-label="Widget actions"
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
                        variant="outline"
                        color="gray"
                        aria-label="Edit widget"
                        onClick={(event) => {
                            event.stopPropagation();
                            onEdit();
                        }}
                    >
                        <MdEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                        variant="outline"
                        color="gray"
                        aria-label="Delete widget"
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
