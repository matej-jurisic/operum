import { Center, Paper, Text } from "@mantine/core";
import { AnalyticCard } from "../../analytics/components/AnalyticCard";
import {
    DashboardWidgetDto,
    QuickAddWidgetConfig,
    WidgetTypes,
} from "../types/DashboardDto";
import { QuickAddWidgetCard } from "./QuickAddWidgetCard";

interface Props {
    widget: DashboardWidgetDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    onEntryClick?: (entryId: string) => void;
}

// Config is free-form JSON per widget type, so it only ever parses to what the widget
// itself expects — never trusted further than that.
function parseQuickAddConfig(config: string | undefined): QuickAddWidgetConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return typeof parsed?.trackerId === "string" ? parsed : null;
    } catch {
        return null;
    }
}

/**
 * Renders one cell of the dashboard grid. Everything a widget needs to know about the
 * grid stops here: the card below is sized by its cell, and anything added to the switch
 * inherits the same placement and edit mode.
 */
export function DashboardWidget({
    widget,
    color,
    isConfiguring,
    onRemove,
    onEntryClick,
}: Props) {
    switch (widget.type) {
        case WidgetTypes.Analytic:
            return widget.analytic ? (
                <AnalyticCard
                    analytic={widget.analytic}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight
                    onRemove={onRemove}
                    onEntryClick={onEntryClick}
                />
            ) : null;
        case WidgetTypes.QuickAdd: {
            const config = parseQuickAddConfig(widget.config);
            return config ? (
                <QuickAddWidgetCard
                    widgetId={widget.id}
                    config={config}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                />
            ) : null;
        }
        default:
            return (
                <Paper withBorder p="md" radius="md" h="100%">
                    <Center h="100%">
                        <Text size="sm" c="dimmed">
                            This widget cannot be displayed
                        </Text>
                    </Center>
                </Paper>
            );
    }
}
