import { Center, Paper, Text } from "@mantine/core";
import { AnalyticCard } from "../../analytics/components/AnalyticCard";
import {
    DashboardWidgetDto,
    QuickAddWidgetConfig,
    TextWidgetConfig,
    WidgetTypes,
} from "../types/DashboardDto";
import { DividerWidgetCard } from "./DividerWidgetCard";
import { EntriesWidgetCard } from "./EntriesWidgetCard";
import { HeaderWidgetCard } from "./HeaderWidgetCard";
import { NoteWidgetCard } from "./NoteWidgetCard";
import { QuickAddWidgetCard } from "./QuickAddWidgetCard";
import { ViewWidgetCard } from "./ViewWidgetCard";

interface Props {
    widget: DashboardWidgetDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the widget's edit dialog. Analytic, Header and Note widgets only: a View
        widget's dropdown and a QuickAdd widget's tracker are instead fixed at add time or
        changed on the card itself, and a Divider has nothing to edit at all. */
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

// Shared by Header and Note, whose Config is nothing but this one string.
function parseTextConfig(config: string | undefined): TextWidgetConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return typeof parsed?.text === "string" ? parsed : null;
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
        case WidgetTypes.Header:
            return (
                <HeaderWidgetCard
                    widgetId={widget.id}
                    config={parseTextConfig(widget.config)}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        case WidgetTypes.Divider:
            return (
                <DividerWidgetCard
                    widgetId={widget.id}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                />
            );
        case WidgetTypes.Note:
            return (
                <NoteWidgetCard
                    widgetId={widget.id}
                    config={parseTextConfig(widget.config)}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                    onEdit={onEdit}
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
