import { Select } from "@mantine/core";
import {
    DashboardItemDto,
    DashboardWidgetDto,
    WidgetTypes,
} from "../types/DashboardDto";

// Prefixes a "follow a view widget" option's value so the same Select can offer both a
// fixed view and a live link without the two id spaces colliding.
const LINK_PREFIX = "link:";

/** The filter half of a WidgetTypes.Entries item's Config — the only part a placement
    stores. Parsed defensively: Config is free-form JSON per widget type. */
interface EntriesItemConfig {
    viewId?: string | null;
    linkedViewWidgetId?: string | null;
}

export function parseEntriesItemConfig(config: string | undefined): EntriesItemConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return typeof parsed === "object" && parsed !== null ? parsed : null;
    } catch {
        return null;
    }
}

/** A board widget a View selector can pull into its own filter, as its edit/add form lists
    it: the Analytic and Entries items that read from the selector's tracker. `linked` is
    whether it already follows this selector; `note` explains what ticking it would replace. */
export interface ViewWidgetLinkTarget {
    itemId: string;
    label: string;
    linked: boolean;
    note?: string;
}

// Whether every source of `item` that reads from `trackerId` currently points at
// `viewWidgetId` (analytic), or the item's Config does (entries). An item with no matching
// source counts as not linked.
function itemFollowsViewWidget(
    item: DashboardItemDto,
    trackerId: string,
    viewWidgetId: string,
): boolean {
    if (item.type === WidgetTypes.Entries) {
        return parseEntriesItemConfig(item.config)?.linkedViewWidgetId === viewWidgetId;
    }

    const matching = item.sources.filter((s) => s.trackerId === trackerId);
    return matching.length > 0 && matching.every((s) => s.linkedViewWidgetId === viewWidgetId);
}

// Whether any source of `item` on `trackerId` is already filtered some other way — a fixed
// view, or a link to a different selector — so ticking it in the picker would replace that.
function itemHasOtherFilter(
    item: DashboardItemDto,
    trackerId: string,
    viewWidgetId: string | null,
): boolean {
    if (item.type === WidgetTypes.Entries) {
        const config = parseEntriesItemConfig(item.config);
        return (
            !!config?.viewId ||
            (!!config?.linkedViewWidgetId && config.linkedViewWidgetId !== viewWidgetId)
        );
    }

    return item.sources
        .filter((s) => s.trackerId === trackerId)
        .some(
            (s) =>
                !!s.viewId ||
                (!!s.linkedViewWidgetId && s.linkedViewWidgetId !== viewWidgetId),
        );
}

/**
 * The Analytic/Entries widgets on the board that a View selector for `trackerId` can link,
 * for its own add/edit form. `viewWidgetId` is the selector being edited (null while it's
 * still being added, so nothing reads as already linked). Two widgets can share a name, so
 * a repeat gets a tie-breaking count appended, the same as linkableViewWidgets.
 */
export function linkTargetsForViewWidget(
    items: DashboardItemDto[],
    trackerId: string | null | undefined,
    viewWidgetId: string | null,
): ViewWidgetLinkTarget[] {
    if (!trackerId) return [];

    const matches = items.filter(
        (i) =>
            (i.type === WidgetTypes.Analytic || i.type === WidgetTypes.Entries) &&
            i.trackerIds.includes(trackerId),
    );

    const seenCounts = new Map<string, number>();

    return matches.map((item) => {
        const baseLabel = item.name || "Untitled widget";
        const seen = (seenCounts.get(baseLabel) ?? 0) + 1;
        seenCounts.set(baseLabel, seen);

        const linked = !!viewWidgetId && itemFollowsViewWidget(item, trackerId, viewWidgetId);

        return {
            itemId: item.id,
            label: seen > 1 ? `${baseLabel} (${seen})` : baseLabel,
            linked,
            note:
                !linked && itemHasOtherFilter(item, trackerId, viewWidgetId)
                    ? "Currently filtered — linking replaces it"
                    : undefined,
        };
    });
}

/** How one source is filtered. At most one of the two is ever set. */
export interface ViewSelection {
    viewId: string | null;
    linkedViewWidgetId: string | null;
}

/**
 * The board's own View widgets built for a tracker, which a source reading from that
 * tracker may follow. A View widget carries no name of its own, so it's labeled by what
 * it's actually showing — its tracker and its current selection — rather than an
 * arbitrary position in the list. Two widgets can still land on the same label (both on
 * "All entries", say), so a repeat gets a tie-breaking count appended.
 */
export function linkableViewWidgets(
    widgets: DashboardWidgetDto[],
    trackerId: string | null | undefined,
): { id: string; label: string }[] {
    if (!trackerId) return [];

    const matches = widgets.filter(
        (w) => w.type === WidgetTypes.View && w.viewWidget?.trackerId === trackerId,
    );

    const seenCounts = new Map<string, number>();

    return matches.map((w) => {
        const viewWidget = w.viewWidget!;
        const currentView = viewWidget.views.find((v) => v.id === viewWidget.viewId);
        const label = `${viewWidget.trackerName} · ${currentView?.name ?? "All entries"}`;

        const seen = (seenCounts.get(label) ?? 0) + 1;
        seenCounts.set(label, seen);

        return { id: w.id, label: seen > 1 ? `${label} (${seen})` : label };
    });
}

interface Props {
    /** The source's own tracker's views, any of which can be fixed onto it. */
    views: { id: string; name: string }[];
    /** What linkableViewWidgets above found for the same tracker. */
    linkableWidgets: { id: string; label: string }[];
    value: ViewSelection;
    onChange: (value: ViewSelection) => void;
    disabled?: boolean;
    placeholder?: string;
}

/**
 * Picks how a source is filtered: one of its tracker's views, fixed onto the source, or a
 * View widget on the board whose dropdown it follows from then on. One control, because
 * the two are alternatives rather than separate settings.
 */
export function SourceViewSelect({
    views,
    linkableWidgets,
    value,
    onChange,
    disabled,
    placeholder,
}: Props) {
    const data = [
        {
            group: "Fixed view",
            items: views.map((v) => ({ value: v.id, label: v.name })),
        },
        ...(linkableWidgets.length > 0
            ? [
                  {
                      group: "Follow a view widget",
                      items: linkableWidgets.map((w) => ({
                          value: `${LINK_PREFIX}${w.id}`,
                          label: w.label,
                      })),
                  },
              ]
            : []),
    ];

    const handleChange = (selected: string | null) => {
        if (selected?.startsWith(LINK_PREFIX)) {
            onChange({
                viewId: null,
                linkedViewWidgetId: selected.slice(LINK_PREFIX.length),
            });
        } else {
            onChange({ viewId: selected, linkedViewWidgetId: null });
        }
    };

    return (
        <Select
            label="Filter by view (optional)"
            placeholder={placeholder ?? "All entries"}
            data={data}
            value={
                value.linkedViewWidgetId
                    ? `${LINK_PREFIX}${value.linkedViewWidgetId}`
                    : value.viewId
            }
            onChange={handleChange}
            disabled={disabled}
            clearable
        />
    );
}
