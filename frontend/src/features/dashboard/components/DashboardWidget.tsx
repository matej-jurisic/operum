import { Center, Paper, Text } from "@mantine/core";
import { AnalyticCard } from "../../analytics/components/AnalyticCard";
import {
    DashboardWidgetDto,
    QuickAddWidgetConfig,
    WidgetTypes,
} from "../types/DashboardDto";
import { EntriesWidgetCard } from "./EntriesWidgetCard";
import { QuickAddWidgetCard } from "./QuickAddWidgetCard";
import { ViewWidgetCard } from "./ViewWidgetCard";

interface Props {
    widget: DashboardWidgetDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the widget's edit dialog. Analytic widgets only: the other kinds are their
        own configuration, and a View widget's dropdown is changed on the card itself. */
    onEdit?: (itemId: string) => void;
    onEntryClick?: (entryId: string) => void;
    onViewSelect?: (itemId: string, viewId: string | null) => void;
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
    onEdit,
    onEntryClick,
    onViewSelect,
}: Props) {
    switch (widget.type) {
        case WidgetTypes.Analytic:
            return widget.analytic ? (
                <AnalyticCard
                    analytic={widget.analytic}
                    // A widget backed by a single tracker reads as that tracker; one
                    // combining several (a composed chart) has no single tracker to take
                    // the color from, so it keeps the board's own.
                    color={widget.trackerColor ?? color}
                    isConfiguring={isConfiguring}
                    fillHeight
                    onRemove={onRemove}
                    onEdit={onEdit}
                    onEntryClick={onEntryClick}
                />
            ) : null;
        case WidgetTypes.QuickAdd: {
            const config = parseQuickAddConfig(widget.config);
            return config ? (
                <QuickAddWidgetCard
                    widgetId={widget.id}
                    config={config}
                    tracker={widget.quickAddTracker}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                />
            ) : null;
        }
        case WidgetTypes.View:
            return (
                <ViewWidgetCard
                    widgetId={widget.id}
                    viewWidget={widget.viewWidget}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                    onSelect={onViewSelect ?? (() => {})}
                />
            );
        case WidgetTypes.Entries:
            return (
                <EntriesWidgetCard
                    widgetId={widget.id}
                    entriesWidget={widget.entriesWidget}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                />
            );
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
