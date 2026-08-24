import { useMemo } from "react";
import {
    getBreakpointFromWidth,
    Layout,
    LayoutItem,
    Responsive,
    useContainerWidth,
    verticalCompactor,
} from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import {
    DashboardLayoutItemDto,
    DashboardWidgetDto,
} from "../types/DashboardDto";
import "./DashboardGrid.css";
import { DashboardWidget } from "./DashboardWidget";

/** Kept in step with DashboardGrid.Columns on the backend: a stored x/w is in these. */
export const DASHBOARD_GRID_COLUMNS = 12;

const ROW_HEIGHT = 40;
const MARGIN: [number, number] = [16, 16];
const MIN_WIDTH = 2;
const MIN_HEIGHT = 2;

// A placement is stored once, in full-width columns. Narrower screens fold it down to
// fewer columns for reading, which is why they are not allowed to save (see canArrange):
// a board rearranged on a phone would otherwise overwrite the desktop one.
const BREAKPOINTS = { lg: 1200, md: 900, sm: 600, xs: 0 };
const COLS = {
    lg: DASHBOARD_GRID_COLUMNS,
    md: DASHBOARD_GRID_COLUMNS,
    sm: 6,
    xs: 1,
};

// Where a widget goes when it has no placement yet, i.e. one added before the board had a
// grid and never arranged since. Two to a row, in the order the server returned them.
const FALLBACK_WIDTH = 6;
const FALLBACK_HEIGHT = 6;

const toLayoutItem = (
    widget: DashboardWidgetDto,
    index: number,
): LayoutItem => {
    const placed = widget.layout && widget.layout.w > 0 && widget.layout.h > 0;

    return {
        i: widget.id,
        x: placed ? widget.layout.x : (index % 2) * FALLBACK_WIDTH,
        y: placed ? widget.layout.y : Math.floor(index / 2) * FALLBACK_HEIGHT,
        w: placed ? widget.layout.w : FALLBACK_WIDTH,
        h: placed ? widget.layout.h : FALLBACK_HEIGHT,
        minW: MIN_WIDTH,
        minH: MIN_HEIGHT,
    };
};

const toLayoutDto = (layout: Layout): DashboardLayoutItemDto[] =>
    layout.map((item) => ({
        itemId: item.i,
        x: item.x,
        y: item.y,
        w: item.w,
        h: item.h,
    }));

interface Props {
    widgets: DashboardWidgetDto[];
    color: string | undefined;
    isConfiguring: boolean;
    onLayoutSave: (layout: DashboardLayoutItemDto[]) => void;
    onRemove?: (itemId: string) => void;
    onEntryClick?: (entryId: string) => void;
}

export function DashboardGrid({
    widgets,
    color,
    isConfiguring,
    onLayoutSave,
    onRemove,
    onEntryClick,
}: Props) {
    // Measured before the first render, so the grid never lays itself out at the hook's
    // assumed default width and overflows a narrower container for a frame.
    const { width, containerRef, mounted } = useContainerWidth({
        measureBeforeMount: true,
    });

    const layout = useMemo(() => widgets.map(toLayoutItem), [widgets]);

    // Only the breakpoints that render the full set of columns show the layout as it is
    // stored, so only those may write it back.
    const breakpoint = getBreakpointFromWidth(BREAKPOINTS, width);
    const layouts = useMemo(
        () => ({ [breakpoint]: layout }),
        [breakpoint, layout],
    );
    const canArrange =
        isConfiguring && COLS[breakpoint] === DASHBOARD_GRID_COLUMNS;

    // Saving on drag/resize stop rather than on every layout change: the change callback
    // also fires when the grid first lays itself out or folds to another breakpoint, which
    // would write back a placement the user never made.
    const handleArranged = (newLayout: Layout) => {
        if (!canArrange) return;
        onLayoutSave(toLayoutDto(newLayout));
    };

    return (
        <div ref={containerRef}>
            {mounted && (
                <Responsive
                    className={`dashboard-grid${canArrange ? " is-editing" : ""}`}
                    width={width}
                    layouts={layouts}
                    breakpoints={BREAKPOINTS}
                    cols={COLS}
                    rowHeight={ROW_HEIGHT}
                    margin={MARGIN}
                    containerPadding={[0, 0]}
                    // Widgets stay exactly where they are dropped: no pulling up into the
                    // gap above them, so a board can be laid out with deliberate empty
                    // space. Collisions are still resolved, they just are not compacted
                    // away afterwards.
                    compactor={verticalCompactor}
                    dragConfig={{
                        enabled: canArrange,
                        // Keep the dragged widget inside the board. Without this it
                        // follows the pointer past the right edge and the page picks up
                        // a horizontal scrollbar mid-drag.
                        bounded: true,
                        // Everything a user can press inside a card stays pressable while
                        // the board is being arranged.
                        cancel: "button, a, input, .mantine-ActionIcon-root",
                    }}
                    resizeConfig={{ enabled: canArrange }}
                    onDragStop={handleArranged}
                    onResizeStop={handleArranged}
                >
                    {widgets.map((widget) => (
                        <div key={widget.id} className="dashboard-widget">
                            <DashboardWidget
                                widget={widget}
                                color={color}
                                isConfiguring={isConfiguring}
                                onRemove={onRemove}
                                onEntryClick={onEntryClick}
                            />
                        </div>
                    ))}
                </Responsive>
            )}
        </div>
    );
}
