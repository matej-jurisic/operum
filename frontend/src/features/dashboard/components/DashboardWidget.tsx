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
    /** Decides whether an Analytic/Entries widget reads its display mode from layout or
        mobileLayout: the two are set (and can differ) independently. */
    variant: LayoutVariant;
    color: string | undefined;
    isConfiguring: boolean;
    /** Inside a container, whose own shape already reads as the group's border: sheds
        the widget's own border/background in read mode so the two don't double up. */
    flat?: boolean;
    onRemove?: (itemId: string) => void;
    /** QuickAdd's tracker is fixed at add time and Divider has nothing to edit, so neither
        uses this. */
    onEdit?: (itemId: string) => void;
    onEntryClick?: (entryId: string) => void;
    onFilterSetValues?: (
        itemId: string,
        values: Record<string, string | null>,
    ) => void;
}

// Only Analytic/Entries widgets carry a display mode; Hidden is filtered upstream (see
// DashboardGrid), so the switch below only ever sees Full or Expandable in practice.
const displayModeHere = (
    widget: DashboardWidgetDto,
    variant: LayoutVariant,
): DashboardItemDisplayMode =>
    variant === LayoutVariants.Mobile
        ? widget.mobileLayout.displayMode
        : widget.layout.displayMode;

function parseQuickAddConfig(config: string | undefined): QuickAddWidgetConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return typeof parsed?.trackerId === "string" ? parsed : null;
    } catch {
        return null;
    }
}

/** Renders one cell of the dashboard grid; the card below is sized by its cell. */
export function DashboardWidget({
    widget,
    variant,
    color,
    isConfiguring,
    flat,
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

            // Precedence: placement override (single-source only) > tracker color > board color.
            const chartColor = widget.color ?? widget.trackerColor ?? color;

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
                        flat={flat}
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
                    flat={flat}
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
                    colorOverride={widget.color}
                    isConfiguring={isConfiguring}
                    flat={flat}
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
                    flat={flat}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    onSetValues={onFilterSetValues ?? (() => {})}
                />
            );
        case WidgetTypes.Entries: {
            if (displayModeHere(widget, variant) === DashboardItemDisplayMode.Hidden)
                return null;

            const entriesColor = widget.color ?? widget.entriesWidget?.color ?? color;

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
                        flat={flat}
                        onRemove={onRemove}
                        onEdit={onEdit}
                        renderExpanded={() => (
                            // EntriesWidgetCard fills its container's height; the grid cell
                            // normally supplies that, so the modal must here.
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
                    flat={flat}
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
                    flat={flat}
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
                    flat={flat}
                    onRemove={onRemove}
                />
            );
        case WidgetTypes.Container:
        case WidgetTypes.TabsContainer:
            // Container: drawn as a DashboardGroupOverlay behind its members instead of a
            // tile of its own. TabsContainer: drawn by TabsContainerTile instead.
            return null;
        case WidgetTypes.Note:
            return (
                <NoteWidgetCard
                    widgetId={widget.id}
                    config={parseTextWidgetConfig(widget.config)}
                    color={color}
                    isConfiguring={isConfiguring}
                    flat={flat}
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
                    flat={flat}
                    onRemove={onRemove}
                />
            );
    }
}
