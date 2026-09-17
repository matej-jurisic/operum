import { ActionIcon, Button, Card, Group, Menu, Stack, Text, ThemeIcon } from "@mantine/core";
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
    TbTargetArrow,
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

/** Falls back to the generic histogram glyph for an unrecognized result type. */
function resultTypeIcon(resultType: string): IconType {
    switch (resultType) {
        case "Single Value":
            return TbNumbers;
        case "Goal":
            return TbTargetArrow;
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

/** No calculated preview here; see DashboardWidget for the actual chart once placed on a board. */
export function WidgetCard({ widget, color, isMobile, onAdd, onEdit, onDelete }: Props) {
    const { hovered, ref } = useHover<HTMLDivElement>();
    const trackerNames = [...new Set(widget.sources.map((s) => s.trackerName))];
    const Icon = resultTypeIcon(widget.resultType);

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
                        <Icon size={20} />
                    </ThemeIcon>
                    <Menu position="bottom-end" withinPortal width={190}>
                        <Menu.Target>
                            <ActionIcon
                                variant="subtle"
                                color="gray"
                                aria-label="Widget actions"
                                onClick={(event) => event.stopPropagation()}
                            >
                                <MdMoreVert size={18} />
                            </ActionIcon>
                        </Menu.Target>
                        <Menu.Dropdown onClick={(event) => event.stopPropagation()}>
                            <Menu.Item leftSection={<MdAdd size={16} />} onClick={onAdd}>
                                Add to board
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
                    <Text fw={600} lineClamp={2} title={widget.name}>
                        {widget.name}
                    </Text>
                    <Text size="xs" c="dimmed" lineClamp={2} mt={2}>
                        {[
                            widget.resultType,
                            ...(widget.goalTarget ? [`target ${widget.goalTarget}`] : []),
                            ...trackerNames,
                        ].join("  ·  ")}
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
                    Add to board
                </Button>
            </Stack>
        </Card>
    );
}
