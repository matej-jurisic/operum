import {
  DashboardLayoutItemDto,
  DashboardWidgetDto,
  LayoutVariant,
  LayoutVariants,
  SaveTabsContainerDto,
} from "../types/DashboardDto";
import type { Layout, LayoutItem } from "@snapgridjs/react";

/** Kept in step with DashboardGrid.Columns on the backend: a stored x/w is in these. */
export const DASHBOARD_GRID_COLUMNS = 24;

/** Kept in step with DashboardGrid.MobileColumns. */
export const DASHBOARD_MOBILE_GRID_COLUMNS = 4;

/** The outer width of the frame the board is boxed into when the mobile layout is arranged
    from a desktop-width screen. Well inside the 900px narrow breakpoint; the grid itself
    renders a 16px inset narrower, matching a real phone. */
export const MOBILE_PREVIEW_WIDTH = 400;

// Effective snap step is rowHeight + margin (18px). See migration HalveDashboardGridRowHeight,
// which doubled every stored y/h to keep existing boards on the same pixels.
export const ROW_HEIGHT = 2;
const MIN_WIDTH = 2;
const MIN_HEIGHT = 2;
const FALLBACK_HEIGHT = 24;

export const DRAG_HANDLE_CLASS = "dashboard-drag-handle";

// Everything a user can press inside a card stays pressable while the board is arranged.
export const DRAG_CANCEL_SELECTOR =
  "button, a, input, .mantine-ActionIcon-root";

// Must match VARIANTS[Desktop].margin, not just look reasonable: with a 2px row height a
// smaller gap here would render a dragged-in widget at a fraction of its board height.
export const CONTAINER_MARGIN: [number, number] = [16, 16];

// Matches CONTAINER_MARGIN and the board's margin so the gap is the same everywhere.
// Applied as the grid's own padding, not CSS, so the measured width matches what renders.
export const CONTAINER_PADDING: [number, number] = [16, 16];

/** The key a grid's pending layout is stashed under while a save is being assembled: a
    container's id, or this for the board itself. */
export const ROOT_KEY = "__root__";

// Breakpoint names double as the variant an arrangement is stored under, so the grid
// reported here is also the grid a placement gets saved to.
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
  /** Width of a widget with no placement yet (added before the board was ever arranged). */
  fallbackWidth: number;
  /** Selector for the only part of a widget a drag may start from. The narrow grid needs
      one so a touch-drag doesn't swallow the board's scroll gesture. */
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

/** Picks the widest breakpoint whose floor the width clears. */
export const variantForWidth = (width: number): LayoutVariant => {
  const byWidest = (
    Object.entries(BREAKPOINTS) as [LayoutVariant, number][]
  ).sort(([, a], [, b]) => b - a);
  const match = byWidest.find(([, floor]) => width >= floor);
  return (match ?? byWidest[byWidest.length - 1])[0];
};

/** `variant` decides which of the two stored placements is read. */
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
  parentTabId: string | null = null,
): DashboardLayoutItemDto[] =>
  layout.map((item) => ({
    itemId: item.i,
    parentItemId,
    parentTabId,
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
  /** Saves a tabs container's title + tab list (add / rename / reorder / delete). */
  onSaveTabsContainer?: (itemId: string, dto: SaveTabsContainerDto) => void;
}
