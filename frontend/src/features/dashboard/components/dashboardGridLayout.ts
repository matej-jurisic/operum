import {
  DashboardLayoutItemDto,
  DashboardWidgetDto,
  LayoutVariant,
  LayoutVariants,
} from "../types/DashboardDto";
import type { Layout, LayoutItem } from "@snapgridjs/react";

/** The shared geometry the board's grids are laid out with: column counts, the row and
    margin units, the fallback placement for a widget never arranged, and the conversions
    between a stored placement and a snapgrid layout item. Pulled out of DashboardGrid so
    the container tile can lay out its own sub-grid the same way. */

/** Kept in step with DashboardGrid.Columns on the backend: a stored x/w is in these. */
export const DASHBOARD_GRID_COLUMNS = 24;

/** Kept in step with DashboardGrid.MobileColumns. */
export const DASHBOARD_MOBILE_GRID_COLUMNS = 4;

// 2px. A row unit is dwarfed by the 16px vertical margin baked into every widget's
// height, so what a drag or resize actually snaps to is rowHeight + margin: 18px here,
// half the 36px step it was at rowHeight 20. HalveDashboardGridRowHeight doubled every
// stored y/h to match, so (row + margin) * 2y and rowHeight * 2h land on the same pixels
// a board was already laid out on -- see that migration.
export const ROW_HEIGHT = 2;
const MIN_WIDTH = 2;
// Two rows: 2*2 + 16 == a 20px sliver, the same floor as before HalveDashboardGridRowHeight.
const MIN_HEIGHT = 2;
const FALLBACK_HEIGHT = 24;

// Grabbed by the drag handle, and by snapgrid to tell that handle apart from the rest of
// the card.
export const DRAG_HANDLE_CLASS = "dashboard-drag-handle";

// Everything a user can press inside a card stays pressable while the board is arranged.
export const DRAG_CANCEL_SELECTOR =
  "button, a, input, .mantine-ActionIcon-root";

// The gap between cells inside a container: tighter than the board's own, since a
// container is already a framed region.
export const CONTAINER_MARGIN: [number, number] = [8, 8];

/** The key a grid's pending layout is stashed under while a save is being assembled: a
    container's id, or this for the board itself. */
export const ROOT_KEY = "__root__";

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

export const COLS: Record<LayoutVariant, number> = {
  [LayoutVariants.Desktop]: DASHBOARD_GRID_COLUMNS,
  [LayoutVariants.Mobile]: DASHBOARD_MOBILE_GRID_COLUMNS,
};

/** Everything else the two grids do differently. */
export interface VariantConfig {
  /** Gap between cells. A phone cannot spare 16px of it beside every widget. */
  margin: [number, number];
  /** How wide a widget with no placement yet is, i.e. one added before the board had a
        grid and never arranged since. The rest of the row is filled with the next ones. */
  fallbackWidth: number;
  /** A selector for the only part of a widget a drag may start from, or nothing to drag
      it from anywhere. The narrow grid needs one so a touch-drag doesn't swallow the
      gesture that scrolls the board. */
  dragHandle?: string;
}

export const VARIANTS: Record<LayoutVariant, VariantConfig> = {
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

/** The grid variant a container this wide renders, i.e. which arrangement is both drawn
    and written back. Picks the widest breakpoint whose floor the width clears. */
export const variantForWidth = (width: number): LayoutVariant => {
  const byWidest = (
    Object.entries(BREAKPOINTS) as [LayoutVariant, number][]
  ).sort(([, a], [, b]) => b - a);
  const match = byWidest.find(([, floor]) => width >= floor);
  return (match ?? byWidest[byWidest.length - 1])[0];
};

/** A widget's placement on one grid as a snapgrid layout item. `variant` decides which of
    the two stored placements is read; `cols` is the column count of the grid it lands on
    (the board's own, or a container's identical sub-grid). */
export const toLayoutItem = (
  widget: DashboardWidgetDto,
  index: number,
  variant: LayoutVariant,
  cols: number,
): LayoutItem => {
  const placement =
    variant === LayoutVariants.Mobile ? widget.mobileLayout : widget.layout;
  const placed = placement && placement.w > 0 && placement.h > 0;

  const fallbackWidth = Math.min(VARIANTS[variant].fallbackWidth, cols);
  const perRow = Math.max(1, Math.floor(cols / fallbackWidth));

  return {
    i: widget.id,
    x: placed ? placement.x : (index % perRow) * fallbackWidth,
    y: placed ? placement.y : Math.floor(index / perRow) * FALLBACK_HEIGHT,
    w: placed ? placement.w : fallbackWidth,
    h: placed ? placement.h : FALLBACK_HEIGHT,
    minW: Math.min(MIN_WIDTH, cols),
    minH: MIN_HEIGHT,
  };
};

export const toLayoutDto = (
  layout: Layout,
  parentItemId: string | null,
): DashboardLayoutItemDto[] =>
  layout.map((item) => ({
    itemId: item.i,
    parentItemId,
    x: item.x,
    y: item.y,
    w: item.w,
    h: item.h,
  }));

export interface DashboardTileCallbacks {
  onRemove?: (itemId: string) => void;
  onEdit?: (itemId: string) => void;
  onEntryClick?: (entryId: string) => void;
  onFilterSetValues?: (
    itemId: string,
    values: Record<string, string | null>,
  ) => void;
}
