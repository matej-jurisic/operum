import { Select } from "@mantine/core";
import { DashboardWidgetDto, WidgetTypes } from "../types/DashboardDto";

// Prefixes a "follow a view widget" option's value so the same Select can offer both a
// fixed view and a live link without the two id spaces colliding.
const LINK_PREFIX = "link:";

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
        const label = `${viewWidget.trackerName} — ${currentView?.name ?? "All entries"}`;

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
