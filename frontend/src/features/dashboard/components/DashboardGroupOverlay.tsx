import { useMemo } from "react";
import { ActionIcon, Text } from "@mantine/core";
import { MdDelete, MdEdit } from "react-icons/md";
import type { Layout } from "@snapgridjs/react";
import {
  DashboardWidgetDto,
  LayoutVariant,
  parseTextWidgetConfig,
} from "../types/DashboardDto";
import {
  COLS,
  DashboardTileCallbacks,
  ROW_HEIGHT,
  VARIANTS,
  layoutToPixelRects,
} from "./dashboardGridLayout";
import { huggingShapePath, ShapeRect } from "./containerShape";
import "./DashboardGroupOverlay.css";

// A touch bigger than the widgets' own radius="md" corners (8px) so the hull reads as
// wrapping them, not competing with them.
const HUG_CORNER_RADIUS = 10;

interface GeometryProps {
  /** This group's members' own entries from the grid's own layout -- not recomputed here.
      Re-running compaction on just the members in isolation would pack them as if nothing
      else on the board existed, drifting from where they actually render (see NestedBoard/
      FlatBoard, which each compute one shared layout for every top-level widget and slice
      each group's members out of it). */
  layout: Layout;
  /** The grid's measured width -- members render on it directly now, so this overlay
      shares its coordinate space instead of measuring one of its own. */
  width: number;
  /** Which grid this group is rendered on: its column count and margin (desktop's root
      grid vs. the mobile flat board) come from this. */
  variant: LayoutVariant;
}

function boundingCorner(rects: ShapeRect[]): { x: number; y: number } | null {
  if (rects.length === 0) return null;
  return {
    x: Math.min(...rects.map((r) => r.x)),
    y: Math.min(...rects.map((r) => r.y)),
  };
}

/** The pixel geometry shared by a group's shape and its header: computed once so drawing
    the hull and anchoring the header to its corner never drift apart. */
function useGroupHugGeometry({ layout, width, variant }: GeometryProps) {
  const margin = VARIANTS[variant].margin;

  const rects = useMemo(() => {
    if (width <= 0) return [];
    return [
      ...layoutToPixelRects(
        layout,
        width,
        COLS[variant],
        ROW_HEIGHT,
        margin,
        [0, 0],
      ).values(),
    ];
    // eslint-disable-next-line react-hooks/exhaustive-deps -- margin is derived from variant
  }, [layout, width, variant]);

  const hugPath = useMemo(
    // +2px slack absorbs layoutToPixelRects's own Math.round, so two widgets exactly one
    // grid gap apart still bridge even if rounding nudged their edges a pixel closer.
    () => huggingShapePath(rects, margin[0] + 2, HUG_CORNER_RADIUS),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- margin is derived from variant
    [rects, variant],
  );

  const corner = useMemo(() => boundingCorner(rects), [rects]);

  return { hugPath, corner };
}

/** The shape that hugs a group's members (see containerShape.ts): rendered *before* the
    grid's own tiles in DOM order, so it paints behind them rather than covering their
    content (an opaque SVG fill painted after its siblings would otherwise sit on top). */
export function DashboardGroupHug({ layout, width, variant }: GeometryProps) {
  const { hugPath } = useGroupHugGeometry({ layout, width, variant });
  if (!hugPath) return null;

  return (
    <svg className="dashboard-group-hug" aria-hidden="true">
      <path d={hugPath} />
    </svg>
  );
}

interface HeaderProps extends DashboardTileCallbacks {
  /** The Container item itself: carries the group's id/name, but no placement of its own
      (a Container owns no X/Y/W/H -- its footprint is wherever its members are). */
  group: DashboardWidgetDto;
  layout: Layout;
  width: number;
  variant: LayoutVariant;
  isConfiguring: boolean;
  color: string | undefined;
}

/** The floating name/edit/remove header, anchored above the group's top-left corner:
    rendered *after* the grid's tiles, so it (and its clickable buttons) sit above them,
    unlike the hug shape. Renaming and membership are both handled by the edit dialog
    `onEdit` opens (EditGroupModal), not here. */
export function DashboardGroupHeader({
  group,
  layout,
  width,
  variant,
  isConfiguring,
  color,
  ...callbacks
}: HeaderProps) {
  const name = parseTextWidgetConfig(group.config)?.text.trim() || "";
  const hasName = name.length > 0;
  const showHeader = hasName || isConfiguring;

  const { corner } = useGroupHugGeometry({ layout, width, variant });
  if (!showHeader || !corner) return null;

  return (
    <div
      className="dashboard-group-header"
      // Floats above the shape's top edge (translateY in the stylesheet) rather than
      // inside it: anchoring inside would land it right on top of the first member's own
      // header/edit controls, since members no longer have a reserved header strip above
      // them the way a container's old nested sub-grid did.
      style={{ top: corner.y, left: corner.x + 4 }}
    >
      {/* No drag grip yet: grabbing the whole group by its header is a later addition
          (moving members individually already works, same as any other widget). */}
      <Text
        size="sm"
        fw={600}
        c={hasName ? undefined : "dimmed"}
        className="dashboard-group-title"
        title={name || "Group"}
      >
        {name || "Group"}
      </Text>
      {isConfiguring && callbacks.onEdit && (
        <ActionIcon
          size="md"
          color={color}
          variant="outline"
          aria-label="Rename group"
          onClick={() => callbacks.onEdit?.(group.id)}
        >
          <MdEdit size={18} />
        </ActionIcon>
      )}
      {isConfiguring && callbacks.onRemove && (
        <ActionIcon
          size="md"
          color={color}
          variant="outline"
          aria-label="Remove group"
          onClick={() => callbacks.onRemove?.(group.id)}
        >
          <MdDelete size={18} />
        </ActionIcon>
      )}
    </div>
  );
}
