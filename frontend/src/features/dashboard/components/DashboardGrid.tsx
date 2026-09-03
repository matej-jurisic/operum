import { ReactNode, useMemo, useRef } from "react";
import { MdDragIndicator } from "react-icons/md";
import { DragDropProvider } from "@dnd-kit/react";
import {
  GridLayout,
  Layout,
  useContainerWidth,
  useGridContainer,
  useGridItem,
  useGridPlaceholder,
  useGridResizeHandle,
  verticalCompactor,
} from "@snapgridjs/react";
import {
  DashboardItemDisplayMode,
  DashboardLayoutItemDto,
  DashboardWidgetDto,
  LayoutVariant,
  LayoutVariants,
  WidgetTypes,
} from "../types/DashboardDto";
import {
  COLS,
  DASHBOARD_GRID_COLUMNS,
  DRAG_CANCEL_SELECTOR,
  DRAG_HANDLE_CLASS,
  DashboardTileCallbacks,
  ROOT_KEY,
  ROW_HEIGHT,
  VARIANTS,
  toLayoutDto,
  toLayoutItem,
  variantForWidth,
} from "./dashboardGridLayout";
import "./DashboardGrid.css";
import { DashboardWidget } from "./DashboardWidget";
import { DashboardContainerTile } from "./DashboardContainerTile";

interface Props extends DashboardTileCallbacks {
  widgets: DashboardWidgetDto[];
  color: string | undefined;
  isConfiguring: boolean;
  onLayoutSave: (
    variant: LayoutVariant,
    layout: DashboardLayoutItemDto[],
  ) => void;
}

export function DashboardGrid({
  widgets,
  color,
  isConfiguring,
  onLayoutSave,
  ...callbacks
}: Props) {
  // Measured with a ResizeObserver. The grid renders only once `mounted` is true, so it
  // never lays itself out at the hook's assumed default width and overflows a narrower
  // container for a frame.
  const { width, containerRef, mounted } = useContainerWidth();
  const variant = variantForWidth(width);

  return (
    <div ref={containerRef}>
      {mounted &&
        (variant === LayoutVariants.Mobile ? (
          <FlatBoard
            widgets={widgets}
            width={width}
            color={color}
            isConfiguring={isConfiguring}
            onLayoutSave={onLayoutSave}
            {...callbacks}
          />
        ) : (
          <NestedBoard
            widgets={widgets}
            width={width}
            color={color}
            isConfiguring={isConfiguring}
            onLayoutSave={onLayoutSave}
            {...callbacks}
          />
        ))}
    </div>
  );
}

/** snapgrid renders the layout it is handed as-is and only compacts during a drag. The
    desktop board is never recompacted server-side, so a stored stack with gaps -- a
    widget deleted, resized smaller, set Hidden, or moved into a container -- shows those
    gaps as dead space, and a freshly added widget (the backend seeds it below the lowest
    row any item ever reached) floats far below the real content. Close the gaps on the
    way in; the first drag in arrange mode then persists the compacted stack. minW/minH
    aren't carried through the compactor, so re-attach them. */
function compactLayout(items: Layout, cols: number): Layout {
  const constraintsById = new Map(items.map((it) => [it.i, it]));
  return verticalCompactor.compact(items, cols).map((it) => {
    const src = constraintsById.get(it.i);
    return src ? { ...it, minW: src.minW, minH: src.minH } : it;
  });
}

interface BoardProps extends DashboardTileCallbacks {
  widgets: DashboardWidgetDto[];
  width: number;
  color: string | undefined;
  isConfiguring: boolean;
  onLayoutSave: (
    variant: LayoutVariant,
    layout: DashboardLayoutItemDto[],
  ) => void;
}

// -- The narrow grid --------------------------------------------------------------------
// A phone flattens containers away: every widget sits on one four-column grid in reading
// order, so the turnkey component is enough. A container item itself draws nothing here.

function FlatBoard({
  widgets,
  width,
  color,
  isConfiguring,
  onLayoutSave,
  ...callbacks
}: BoardProps) {
  const config = VARIANTS[LayoutVariants.Mobile];
  const cols = COLS[LayoutVariants.Mobile];

  // Containers are flattened away on the narrow grid; a widget set Hidden on mobile is
  // dropped from it entirely (reachable from the board's hidden-widgets list instead).
  const shown = useMemo(
    () =>
      widgets.filter(
        (w) =>
          w.type !== WidgetTypes.Container &&
          w.mobileLayout.displayMode !== DashboardItemDisplayMode.Hidden,
      ),
    [widgets],
  );

  // Containers are flattened away here, but every widget still carries the mobileLayout.y
  // it was seeded with, and that stack is never recompacted server-side when the wide
  // grid's container tree changes -- a widget moved into a container on the desktop board
  // leaves a screen-tall hole on the phone. compactLayout closes it.
  const layout = useMemo(
    () =>
      compactLayout(
        shown.map((widget, index) =>
          toLayoutItem(widget, index, LayoutVariants.Mobile, cols),
        ),
        cols,
      ),
    [shown, cols],
  );

  const handleArranged = (newLayout: Layout) => {
    if (!isConfiguring) return;
    onLayoutSave(LayoutVariants.Mobile, toLayoutDto(newLayout, null));
  };

  return (
    <GridLayout
      className={`dashboard-grid${isConfiguring ? " is-editing" : ""}`}
      width={width}
      layout={layout}
      gridConfig={{
        cols,
        rowHeight: ROW_HEIGHT,
        margin: config.margin,
        containerPadding: [0, 0],
      }}
      compactor={verticalCompactor}
      isDraggable={isConfiguring}
      isResizable={isConfiguring}
      dragConfig={{
        enabled: isConfiguring,
        bounded: true,
        handle: config.dragHandle,
        cancel: DRAG_CANCEL_SELECTOR,
      }}
      resizeConfig={{ enabled: isConfiguring }}
      onLayoutChange={handleArranged}
    >
      {shown.map((widget) => (
        <div key={widget.id} className="dashboard-widget">
          {isConfiguring && config.dragHandle && (
            <div className={DRAG_HANDLE_CLASS} aria-hidden="true">
              <MdDragIndicator size={18} />
            </div>
          )}
          <DashboardWidget
            widget={widget}
            variant={LayoutVariants.Mobile}
            color={color}
            isConfiguring={isConfiguring}
            {...callbacks}
          />
        </div>
      ))}
    </GridLayout>
  );
}

// -- The wide grid ---------------------------------------------------------------------
// Containers each hold their own sub-grid. Every grid on the board -- the root one and
// one per container -- shares a single dnd-kit provider, which is what lets a widget be
// dragged from one into another.

function NestedBoard({
  widgets,
  width,
  color,
  isConfiguring,
  onLayoutSave,
  ...callbacks
}: BoardProps) {
  const containerIds = useMemo(
    () =>
      new Set(
        widgets
          .filter((w) => w.type === WidgetTypes.Container)
          .map((w) => w.id),
      ),
    [widgets],
  );

  const { topWidgets, childrenByContainer } = useMemo(() => {
    // A widget belongs to a container only if that container still exists; a stale
    // parent (its container was deleted out from under it) falls back to the board.
    const parentOf = (w: DashboardWidgetDto) =>
      w.parentItemId && containerIds.has(w.parentItemId)
        ? w.parentItemId
        : null;

    const top: DashboardWidgetDto[] = [];
    const byContainer = new Map<string, DashboardWidgetDto[]>();
    for (const w of widgets) {
      // A widget set Hidden on the wide grid is dropped from it entirely -- both from the
      // board and from whatever container it belongs to -- and reached from the board's
      // hidden-widgets list instead.
      if (w.layout.displayMode === DashboardItemDisplayMode.Hidden) continue;

      const parent = parentOf(w);
      if (parent === null) {
        top.push(w);
        continue;
      }
      const list = byContainer.get(parent) ?? [];
      list.push(w);
      byContainer.set(parent, list);
    }
    return { topWidgets: top, childrenByContainer: byContainer };
  }, [widgets, containerIds]);

  // A cross-grid drop reports the item leaving one grid and joining another as two
  // separate layout changes, both fired synchronously. Rather than persist each on its
  // own -- and race them -- each grid drops its latest layout here and one microtask
  // later they are assembled into a single whole-board save.
  const pending = useRef(new Map<string, Layout>());
  const flushQueued = useRef(false);

  const queueSave = (key: string, layout: Layout) => {
    if (!isConfiguring) return;
    pending.current.set(key, layout);
    if (flushQueued.current) return;
    flushQueued.current = true;
    queueMicrotask(() => {
      flushQueued.current = false;
      const items: DashboardLayoutItemDto[] = [];
      for (const [gridKey, gridLayout] of pending.current) {
        items.push(
          ...toLayoutDto(gridLayout, gridKey === ROOT_KEY ? null : gridKey),
        );
      }
      pending.current.clear();
      if (items.length > 0) onLayoutSave(LayoutVariants.Desktop, items);
    });
  };

  return (
    <DragDropProvider>
      <BoardSubGrid
        gridKey={ROOT_KEY}
        width={width}
        widgets={topWidgets}
        margin={VARIANTS[LayoutVariants.Desktop].margin}
        isConfiguring={isConfiguring}
        onArranged={(layout) => queueSave(ROOT_KEY, layout)}
        renderContent={(widget, handleRef) =>
          widget.type === WidgetTypes.Container ? (
            <DashboardContainerTile
              widget={widget}
              handleRef={handleRef}
              childWidgets={childrenByContainer.get(widget.id) ?? []}
              color={color}
              isConfiguring={isConfiguring}
              onChildrenArranged={(layout) => queueSave(widget.id, layout)}
              {...callbacks}
            />
          ) : (
            <DashboardWidget
              widget={widget}
              variant={LayoutVariants.Desktop}
              color={color}
              isConfiguring={isConfiguring}
              {...callbacks}
            />
          )
        }
      />
    </DragDropProvider>
  );
}

// -- One grid surface, headless ------------------------------------------------------

interface BoardSubGridProps {
  gridKey: string;
  width: number;
  widgets: DashboardWidgetDto[];
  margin: [number, number];
  isConfiguring: boolean;
  onArranged: (layout: Layout) => void;
  /** The tile body for a widget. `handleRef`, when attached to an element, restricts a
      pointer drag of the tile to that element (used by a container's header so a drag
      that starts inside its sub-grid doesn't move the whole panel). */
  renderContent: (
    widget: DashboardWidgetDto,
    handleRef: (element: Element | null) => void,
  ) => ReactNode;
  /** Floor on the surface's height, so an empty grid still offers an area a widget can
      be dragged onto. */
  minHeight?: number;
  /** Inset between the grid's edge and its cells. Given as the grid's own padding rather
      than CSS padding on the wrapper, so the measured width the grid is handed matches
      the box it actually renders into. */
  containerPadding?: [number, number];
}

export function BoardSubGrid({
  gridKey,
  width,
  widgets,
  margin,
  isConfiguring,
  onArranged,
  renderContent,
  minHeight,
  containerPadding = [0, 0],
}: BoardSubGridProps) {
  const layout = useMemo(
    () =>
      compactLayout(
        widgets.map((widget, index) =>
          toLayoutItem(
            widget,
            index,
            LayoutVariants.Desktop,
            DASHBOARD_GRID_COLUMNS,
          ),
        ),
        DASHBOARD_GRID_COLUMNS,
      ),
    [widgets],
  );

  const { containerProps, group } = useGridContainer({
    id: gridKey,
    width,
    layout,
    onLayoutChange: onArranged,
    gridConfig: {
      cols: DASHBOARD_GRID_COLUMNS,
      rowHeight: ROW_HEIGHT,
      margin,
      containerPadding,
    },
    compactor: verticalCompactor,
    isDraggable: isConfiguring,
    isResizable: isConfiguring,
    dragConfig: {
      enabled: isConfiguring,
      bounded: true,
      cancel: DRAG_CANCEL_SELECTOR,
    },
    resizeConfig: { enabled: isConfiguring },
  });

  return (
    <div
      {...containerProps}
      style={
        minHeight
          ? { ...containerProps.style, minHeight }
          : containerProps.style
      }
      className={`dashboard-grid${isConfiguring ? " is-editing" : ""}`}
    >
      {widgets.map((widget) => (
        <BoardTile
          key={widget.id}
          id={widget.id}
          group={group}
          isConfiguring={isConfiguring}
        >
          {(handleRef) => renderContent(widget, handleRef)}
        </BoardTile>
      ))}
      <BoardPlaceholder group={group} />
    </div>
  );
}

function BoardTile({
  id,
  group,
  isConfiguring,
  children,
}: {
  id: string;
  group: string;
  isConfiguring: boolean;
  children: (handleRef: (element: Element | null) => void) => ReactNode;
}) {
  const { ref, handleRef, style, isDragging } = useGridItem({ id, group });

  return (
    <div
      ref={ref}
      style={style}
      className={`snapgrid-item${isDragging ? " is-dragging" : ""}`}
    >
      <div className="dashboard-widget">{children(handleRef)}</div>
      {isConfiguring && <ResizeHandle id={id} group={group} />}
    </div>
  );
}

function ResizeHandle({ id, group }: { id: string; group: string }) {
  const { ref, handleProps } = useGridResizeHandle({
    id,
    handle: "se",
    group,
  });
  return (
    <span
      ref={ref}
      {...handleProps}
      aria-hidden="true"
      className="snapgrid-resize-handle snapgrid-resize-handle--se"
    />
  );
}

function BoardPlaceholder({ group }: { group: string }) {
  const placeholder = useGridPlaceholder(group);
  if (!placeholder) return null;
  return (
    <div
      aria-hidden="true"
      className="snapgrid-placeholder"
      style={placeholder.style}
    />
  );
}
