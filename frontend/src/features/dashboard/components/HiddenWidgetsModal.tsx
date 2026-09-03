import { Button, Group, Modal, Stack, Text, ThemeIcon } from "@mantine/core";
import { TbChartHistogram, TbTable } from "react-icons/tb";
import {
    DashboardItemDisplayMode,
    DashboardWidgetDto,
    WidgetTypes,
} from "../types/DashboardDto";

interface Props {
    widgets: DashboardWidgetDto[];
    color: string;
    /** Opens the widget's normal edit dialog, the one place its display mode is changed
        back. */
    onEdit: (itemId: string) => void;
    onClose: () => void;
}

const isHidden = (mode: DashboardItemDisplayMode) =>
    mode === DashboardItemDisplayMode.Hidden;

function widgetName(widget: DashboardWidgetDto): string {
    if (widget.type === WidgetTypes.Analytic)
        return widget.analytic?.name || "Untitled chart";
    if (widget.type === WidgetTypes.Entries)
        return widget.entriesWidget?.trackerName ?? "Entries";
    return "Widget";
}

function hiddenWhere(widget: DashboardWidgetDto): string {
    const onDesktop = isHidden(widget.layout.displayMode);
    const onMobile = isHidden(widget.mobileLayout.displayMode);
    if (onDesktop && onMobile) return "Hidden on desktop and mobile";
    if (onDesktop) return "Hidden on desktop";
    return "Hidden on mobile";
}

/**
 * The board's Analytic/Entries widgets currently set to Hidden on at least one grid.
 * Those widgets are dropped from the grid entirely (see DashboardGrid), so this list is
 * the only way back to them: Edit opens the same dialog the widget's own controls would,
 * where the display mode is switched back.
 */
export function HiddenWidgetsModal({ widgets, color, onEdit, onClose }: Props) {
    const hidden = widgets.filter(
        (w) =>
            (w.type === WidgetTypes.Analytic || w.type === WidgetTypes.Entries) &&
            (isHidden(w.layout.displayMode) ||
                isHidden(w.mobileLayout.displayMode)),
    );

    return (
        <Modal opened onClose={onClose} title="Hidden widgets" size="md" centered>
            {hidden.length === 0 ? (
                <Text size="sm" c="dimmed">
                    No widgets are hidden.
                </Text>
            ) : (
                <Stack gap="xs">
                    {hidden.map((widget) => (
                        <Group
                            key={widget.id}
                            justify="space-between"
                            wrap="nowrap"
                            gap="sm"
                        >
                            <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                                <ThemeIcon variant="light" color={color} radius="md">
                                    {widget.type === WidgetTypes.Entries ? (
                                        <TbTable size={16} />
                                    ) : (
                                        <TbChartHistogram size={16} />
                                    )}
                                </ThemeIcon>
                                <Stack gap={0} style={{ minWidth: 0 }}>
                                    <Text size="sm" fw={500} lineClamp={1}>
                                        {widgetName(widget)}
                                    </Text>
                                    <Text size="xs" c="dimmed">
                                        {hiddenWhere(widget)}
                                    </Text>
                                </Stack>
                            </Group>
                            <Button
                                size="xs"
                                variant="light"
                                color={color}
                                style={{ flexShrink: 0 }}
                                onClick={() => {
                                    onEdit(widget.id);
                                    onClose();
                                }}
                            >
                                Edit
                            </Button>
                        </Group>
                    ))}
                </Stack>
            )}
        </Modal>
    );
}
