import { useMemo } from "react";
import { MdDragIndicator } from "react-icons/md";
import {
  getBreakpointFromWidth,
  Layout,
  LayoutItem,
  noCompactor,
  Responsive,
  useContainerWidth,
  verticalCompactor,
} from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import {
  DashboardLayoutItemDto,
  DashboardWidgetDto,
  LayoutVariant,
  LayoutVariants,
} from "../types/DashboardDto";
import "./DashboardGrid.css";
import { DashboardWidget } from "./DashboardWidget";

/** Kept in step with DashboardGrid.Columns on the backend: a stored x/w is in these. */
export const DASHBOARD_GRID_COLUMNS = 24;

/** Kept in step with DashboardGrid.MobileColumns. */
export const DASHBOARD_MOBILE_GRID_COLUMNS = 4;

// 2px. A row unit is dwarfed by the 16px vertical margin baked into every widget's
// height, so what a drag or resize actually snaps to is rowHeight + margin: 18px here,
// half the 36px step it was at rowHeight 20. HalveDashboardGridRowHeight doubled every
// stored y/h to match, so (row + margin) * 2y and rowHeight * 2h land on the same pixels
// a board was already laid out on -- see that migration.
const ROW_HEIGHT = 2;
const MIN_WIDTH = 2;
// Two rows: 2*2 + 16 == a 20px sliver, the same floor as before HalveDashboardGridRowHeight.
// A divider or a header is a layout accent, not content, and a board full of them reads
// better when they can be squeezed right down. The arrange-mode controls that would
// overflow a cell this short are capped to it in DashboardGrid.css.
const MIN_HEIGHT = 2;

// Grabbed by the drag handle below, and by react-grid-layout to tell that handle apart
// from the rest of the card.
const DRAG_HANDLE_CLASS = "dashboard-drag-handle";

// Where the wide grid gives way to the narrow one. A twelfth of anything below this is
// about eighty pixels, which is narrower than the axis labels of the chart inside it.
//
// The breakpoint names are the variants an arrangement is stored under, so the breakpoint
// the grid reports is also the grid that gets saved: a placement can never be written back
// to the grid it was not made on.
const BREAKPOINTS: Record<LayoutVariant, number> = {
  [LayoutVariants.Desktop]: 900,
  [LayoutVariants.Mobile]: 0,
};

const COLS: Record<LayoutVariant, number> = {
  [LayoutVariants.Desktop]: DASHBOARD_GRID_COLUMNS,
  [LayoutVariants.Mobile]: DASHBOARD_MOBILE_GRID_COLUMNS,
};

/** Everything else the two grids do differently. */
interface VariantConfig {
  /** Gap between cells. A phone cannot spare 16px of it beside every widget. */
  margin: [number, number];
  /** How wide a widget with no placement yet is, i.e. one added before the board had a
        grid and never arranged since. The rest of the row is filled with the next ones. */
  fallbackWidth: number;
  /**
   * A selector for the only part of a widget a drag may start from, or nothing to drag
   * it from anywhere.
   *
   * A touch that starts a drag is swallowed by it, so on the narrow grid -- where a card
   * spans the full width -- a card draggable everywhere would leave the board with
   * nothing to scroll it by while it is being arranged. A mouse has no such conflict,
   * and dragging the card itself is the nicer gesture, so the wide grid keeps it.
   */
  dragHandle?: string;
}

const VARIANTS: Record<LayoutVariant, VariantConfig> = {
  [LayoutVariants.Desktop]: {
    margin: [16, 16],
    fallbackWidth: 12,
  },
  [LayoutVariants.Mobile]: {
    margin: [8, 8],
    fallbackWidth: DASHBOARD_MOBILE_GRID_COLUMNS,
    dragHandle: `.${DRAG_HANDLE_CLASS}`,
  },
};

const FALLBACK_HEIGHT = 24;

const toLayoutItem = (
  widget: DashboardWidgetDto,
  index: number,
  variant: LayoutVariant,
): LayoutItem => {
  const placement =
    variant === LayoutVariants.Mobile ? widget.mobileLayout : widget.layout;
  const placed = placement && placement.w > 0 && placement.h > 0;

  const { fallbackWidth } = VARIANTS[variant];
  const perRow = Math.max(1, Math.floor(COLS[variant] / fallbackWidth));

  return {
    i: widget.id,
    x: placed ? placement.x : (index % perRow) * fallbackWidth,
    y: placed ? placement.y : Math.floor(index / perRow) * FALLBACK_HEIGHT,
    w: placed ? placement.w : fallbackWidth,
    h: placed ? placement.h : FALLBACK_HEIGHT,
    // On the narrow grid the smallest a widget can be squeezed to is half the screen
    // rather than a sixth of it. Mirrors DashboardGrid.MinWidthFor on the backend.
    minW: Math.min(MIN_WIDTH, COLS[variant]),
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
  onLayoutSave: (
    variant: LayoutVariant,
    layout: DashboardLayoutItemDto[],
  ) => void;
  onRemove?: (itemId: string) => void;
  onEdit?: (itemId: string) => void;
  onEntryClick?: (entryId: string) => void;
  onViewSelectorSelect?: (itemId: string, selectedId: string | null) => void;
}

export function DashboardGrid({
  widgets,
  color,
  isConfiguring,
  onLayoutSave,
  onRemove,
  onEdit,
  onEntryClick,
  onViewSelectorSelect,
}: Props) {
  // Measured before the first render, so the grid never lays itself out at the hook's
  // assumed default width and overflows a narrower container for a frame.
  const { width, containerRef, mounted } = useContainerWidth({
    measureBeforeMount: true,
  });

  // Which grid this screen is wide enough for, and so which arrangement is being both
  // rendered and written back.
  const variant = getBreakpointFromWidth(BREAKPOINTS, width);
  const config = VARIANTS[variant];

  const layout = useMemo(
    () => widgets.map((widget, index) => toLayoutItem(widget, index, variant)),
    [widgets, variant],
  );
  const layouts = useMemo(() => ({ [variant]: layout }), [variant, layout]);

  // Saving on drag/resize stop rather than on every layout change: the change callback
  // also fires when the grid first lays itself out or folds to the other grid, which
  // would write back a placement the user never made.
  const handleArranged = (newLayout: Layout) => {
    if (!isConfiguring) return;
    onLayoutSave(variant, toLayoutDto(newLayout));
  };

  return (
    <div ref={containerRef}>
      {mounted && (
        <Responsive
          className={`dashboard-grid${isConfiguring ? " is-editing" : ""}`}
          width={width}
          layouts={layouts}
          breakpoints={BREAKPOINTS}
          cols={COLS}
          rowHeight={ROW_HEIGHT}
          margin={config.margin}
          containerPadding={[0, 0]}
          // Widgets don't stay exactly where they are dropped: they get pullied up into the
          // gap above them.
          compactor={verticalCompactor}
          dragConfig={{
            enabled: isConfiguring,
            // Keep the dragged widget inside the board. Without this it
            // follows the pointer past the right edge and the page picks up
            // a horizontal scrollbar mid-drag.
            bounded: true,
            handle: config.dragHandle,
            // Everything a user can press inside a card stays pressable while
            // the board is being arranged.
            cancel: "button, a, input, .mantine-ActionIcon-root",
          }}
          resizeConfig={{ enabled: isConfiguring }}
          onDragStop={handleArranged}
          onResizeStop={handleArranged}
        >
          {widgets.map((widget) => (
            <div key={widget.id} className="dashboard-widget">
              {/* Not a button: react-draggable ignores anything matching
                                the cancel selector above, which every real control on a
                                card is meant to match. Dragging is a pointer gesture with
                                no keyboard equivalent here, so nothing is lost by hiding
                                it from assistive tech. */}
              {isConfiguring && config.dragHandle && (
                <div className={DRAG_HANDLE_CLASS} aria-hidden="true">
                  <MdDragIndicator size={18} />
                </div>
              )}
              <DashboardWidget
                widget={widget}
                variant={variant}
                color={color}
                isConfiguring={isConfiguring}
                onRemove={onRemove}
                onEdit={onEdit}
                onEntryClick={onEntryClick}
                onViewSelectorSelect={onViewSelectorSelect}
              />
            </div>
          ))}
        </Responsive>
      )}
    </div>
  );
}
