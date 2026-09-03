import { TbChartHistogram, TbTable } from "react-icons/tb";
import { AnalyticCard } from "../../analytics/components/AnalyticCard";
import {
    DashboardItemDisplayMode,
    DashboardWidgetDto,
    LayoutVariant,
    LayoutVariants,
    QuickAddWidgetConfig,
    parseTextWidgetConfig,
    WidgetTypes,
} from "../types/DashboardDto";
import { DividerWidgetCard } from "./DividerWidgetCard";
import { EntriesWidgetCard } from "./EntriesWidgetCard";
import { ExpandableWidgetCard } from "./ExpandableWidgetCard";
import { HeaderWidgetCard } from "./HeaderWidgetCard";
import { NoteWidgetCard } from "./NoteWidgetCard";
import { FilterWidgetCard } from "./FilterWidgetCard";
import { QuickAddWidgetCard } from "./QuickAddWidgetCard";
import { UnknownWidgetCard } from "./UnknownWidgetCard";

interface Props {
    widget: DashboardWidgetDto;
    /** Which of the board's two grids this is being rendered on -- decides whether an
        Analytic/Entries widget reads its display mode from layout or from mobileLayout,
        since the two are set (and can differ) independently. */
    variant: LayoutVariant;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the widget's edit dialog. Analytic, Entries, Header, Note and Filter widgets:
        a Filter widget's edit dialog sets its clauses, its presets and which widgets
        follow it. A QuickAdd widget's tracker is fixed at add time and a Divider has
        nothing to edit. */
    onEdit?: (itemId: string) => void;
    onEntryClick?: (entryId: string) => void;
    onFilterSetValues?: (
        itemId: string,
        values: Record<string, string | null>,
    ) => void;
}

// How the current grid draws this widget — see DashboardItemDisplayMode. Only
// Analytic/Entries widgets carry it; a Hidden widget is filtered out of the grid upstream
// (see DashboardGrid), so the switch below only ever sees Full or Expandable in practice.
const displayModeHere = (
    widget: DashboardWidgetDto,
    variant: LayoutVariant,
): DashboardItemDisplayMode =>
    variant === LayoutVariants.Mobile
        ? widget.mobileLayout.displayMode
        : widget.layout.displayMode;

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
    variant,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    onEntryClick,
    onFilterSetValues,
}: Props) {
    switch (widget.type) {
        case WidgetTypes.Analytic: {
            if (!widget.analytic) return null;

            // Filtered out of the grid upstream; the null is a guard, not the real path.
            if (displayModeHere(widget, variant) === DashboardItemDisplayMode.Hidden)
                return null;

            // A widget backed by a single tracker reads as that tracker; one combining
            // several (a composed chart) has no single tracker to take the color from, so
            // it keeps the board's own.
            const chartColor = widget.trackerColor ?? color;

            if (
                displayModeHere(widget, variant) ===
                DashboardItemDisplayMode.Expandable
            ) {
                return (
                    <ExpandableWidgetCard
                        widgetId={widget.id}
                        title={widget.analytic.name || "Untitled chart"}
                        icon={TbChartHistogram}
                        color={chartColor}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
                        onEdit={onEdit}
                        renderExpanded={() => (
                            <AnalyticCard
                                analytic={widget.analytic!}
                                color={chartColor}
                                isConfiguring={false}
                                onEntryClick={onEntryClick}
                            />
                        )}
                    />
                );
            }

            return (
                <AnalyticCard
                    analytic={widget.analytic}
                    color={chartColor}
                    isConfiguring={isConfiguring}
                    fillHeight
                    onRemove={onRemove}
                    onEdit={onEdit}
                    onEntryClick={onEntryClick}
                />
            );
        }
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
        case WidgetTypes.Filter:
            return (
                <FilterWidgetCard
                    widgetId={widget.id}
                    filter={widget.filter}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    onSetValues={onFilterSetValues ?? (() => {})}
                />
            );
        case WidgetTypes.Entries: {
            if (displayModeHere(widget, variant) === DashboardItemDisplayMode.Hidden)
                return null;

            const entriesColor = widget.entriesWidget?.color ?? color;

            if (
                displayModeHere(widget, variant) ===
                DashboardItemDisplayMode.Expandable
            ) {
                return (
                    <ExpandableWidgetCard
                        widgetId={widget.id}
                        title={widget.entriesWidget?.trackerName ?? "Entries"}
                        icon={TbTable}
                        color={entriesColor}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
                        onEdit={onEdit}
                        renderExpanded={() => (
                            // EntriesWidgetCard always fills its container's height; the
                            // grid cell normally supplies that, so the modal has to here.
                            <div style={{ height: "70vh" }}>
                                <EntriesWidgetCard
                                    widgetId={widget.id}
                                    entriesWidget={widget.entriesWidget}
                                    color={entriesColor}
                                    isConfiguring={false}
                                />
                            </div>
                        )}
                    />
                );
            }

            return (
                <EntriesWidgetCard
                    widgetId={widget.id}
                    entriesWidget={widget.entriesWidget}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        }
        case WidgetTypes.Header:
            return (
                <HeaderWidgetCard
                    widgetId={widget.id}
                    config={parseTextWidgetConfig(widget.config)}
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
        case WidgetTypes.Container:
            // A container's sub-grid is drawn by DashboardContainerTile, which the grid
            // renders in place of this card. Nothing to draw here, and on the narrow grid
            // a container is dropped entirely.
            return null;
        case WidgetTypes.Note:
            return (
                <NoteWidgetCard
                    widgetId={widget.id}
                    config={parseTextWidgetConfig(widget.config)}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        default:
            return (
                <UnknownWidgetCard
                    widgetId={widget.id}
                    color={color}
                    isConfiguring={isConfiguring}
                    onRemove={onRemove}
                />
            );
    }
}
