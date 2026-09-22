import { ReactNode, useCallback, useMemo, useRef } from "react";
import { MdDragIndicator } from "react-icons/md";
import { DragDropProvider } from "@dnd-kit/react";
import {
  GridLayout,
  Layout,
  LayoutItem,
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
  CONTAINER_PADDING,
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
import { TabsContainerTile } from "./TabsContainerTile";

interface Props extends DashboardTileCallbacks {
  widgets: DashboardWidgetDto[];
  color: string | undefined;
  isConfiguring: boolean;
  /** Boxes the board to a phone-width frame so the narrow grid renders and drags save to
      the Mobile arrangement, whatever the real viewport is. Arrange mode only. */
  previewMobile?: boolean;
  onLayoutSave: (
    variant: LayoutVariant,
    layout: DashboardLayoutItemDto[],
  ) => void;
}

export function DashboardGrid({
  widgets,
  color,
  isConfiguring,
  previewMobile = false,
  onLayoutSave,
  ...callbacks
}: Props) {
  // Renders only once `mounted`, so it never lays out at the hook's default width first.
  const { width, containerRef, mounted } = useContainerWidth();
  const variant = previewMobile
    ? LayoutVariants.Mobile
    : variantForWidth(width);

  const board = (
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

  // The measured element stays padding-free so the width the grid is handed matches what
  // it renders into; the frame carries the border and inset instead.
  return previewMobile ? (
    <div className="dashboard-mobile-frame">{board}</div>
  ) : (
    board
  );
}

/** The stored layout is never recompacted server-side, so gaps from deleted/hidden/moved
    widgets are closed here on the way in. minW/minH aren't carried through the compactor,
    so re-attach them. */
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
// A phone flattens containers away: every widget sits on one four-column grid in reading order.

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

  // Containers are flattened away (children join the flow); Hidden widgets are dropped
  // (reachable from the board's hidden-widgets list instead).
  const shown = useMemo(
    () =>
      widgets.filter(
        (w) =>
          w.type !== WidgetTypes.Container &&
          w.type !== WidgetTypes.TabsContainer &&
          w.mobileLayout.displayMode !== DashboardItemDisplayMode.Hidden,
      ),
    [widgets],
  );

  // mobileLayout.y is never recompacted server-side when the wide grid's container tree
  // changes, so a widget moved into a container there can leave a hole here. compactLayout closes it.
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
          <SizeDebugBadge
            w={widget.mobileLayout.w}
            h={widget.mobileLayout.h}
          />
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
// Every grid on the board (root + one per container) shares a single dnd-kit provider,
// which lets a widget be dragged from one into another.

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
          .filter(
            (w) =>
              w.type === WidgetTypes.Container ||
              w.type === WidgetTypes.TabsContainer,
          )
          .map((w) => w.id),
      ),
    [widgets],
  );

  const { topWidgets, childrenByContainer, parentById } = useMemo(() => {
    // A stale parent (container deleted out from under it) falls back to the board.
    const parentOf = (w: DashboardWidgetDto) =>
      w.parentItemId && containerIds.has(w.parentItemId)
        ? w.parentItemId
        : null;

    const top: DashboardWidgetDto[] = [];
    const byContainer = new Map<string, DashboardWidgetDto[]>();
    // Read just before a drop, to tell a widget that changed grids apart from one that
    // only moved within its own.
    const byId = new Map<string, string | null>();
    for (const w of widgets) {
      byId.set(w.id, parentOf(w));
      // Hidden widgets are dropped entirely; reached from the board's hidden-widgets list.
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
    return {
      topWidgets: top,
      childrenByContainer: byContainer,
      parentById: byId,
    };
  }, [widgets, containerIds]);

  // Each grid's measured inner width, so a widget crossing grids can be rescaled to keep
  // its on-screen size. Board width is known directly; containers report on mount.
  const gridWidths = useRef(new Map<string, number>());
  const reportGridWidth = useCallback((id: string, w: number) => {
    gridWidths.current.set(id, w);
  }, []);

  // Pixel span of one column-plus-gap on the given grid (null for the board).
  const colStepOf = (parentItemId: string | null): number | null => {
    const margin = VARIANTS[LayoutVariants.Desktop].margin[0];
    if (parentItemId === null) return (width + margin) / DASHBOARD_GRID_COLUMNS;
    const bodyWidth = gridWidths.current.get(parentItemId);
    if (!bodyWidth) return null;
    return (
      (bodyWidth + margin - CONTAINER_PADDING[0] * 2) / DASHBOARD_GRID_COLUMNS
    );
  };

  const clampCol = (v: number, max: number) => Math.max(0, Math.min(max, v));

  const keepSizeAcrossMove = (
    item: LayoutItem,
    from: string | null,
    to: string | null,
  ): LayoutItem => {
    if (from === to) return item;
    const fromStep = colStepOf(from);
    const toStep = colStepOf(to);
    if (!fromStep || !toStep) return item;
    const scale = fromStep / toStep;
    if (Math.abs(scale - 1) < 0.05) return item;
    const w = clampCol(Math.round(item.w * scale), DASHBOARD_GRID_COLUMNS) || 1;
    const x = clampCol(
      Math.round(item.x * scale),
      DASHBOARD_GRID_COLUMNS - w,
    );
    return { ...item, w, x };
  };

  // A cross-grid drop fires two synchronous layout changes (leaving + joining); each grid
  // queues its latest layout here and a microtask later assembles one whole-board save.
  // Keyed by grid: ROOT_KEY, a container id, or `${containerId}:${tabId}` for a tab.
  const pending = useRef(
    new Map<
      string,
      { parentItemId: string | null; parentTabId: string | null; layout: Layout }
    >(),
  );
  const flushQueued = useRef(false);

  const queueSave = (
    key: string,
    layout: Layout,
    parentItemId: string | null = null,
    parentTabId: string | null = null,
  ) => {
    if (!isConfiguring) return;
    pending.current.set(key, { parentItemId, parentTabId, layout });
    if (flushQueued.current) return;
    flushQueued.current = true;
    queueMicrotask(() => {
      flushQueued.current = false;
      const items: DashboardLayoutItemDto[] = [];
      for (const { parentItemId, parentTabId, layout } of pending.current.values()) {
        const sized = layout.map((item) =>
          keepSizeAcrossMove(item, parentById.get(item.i) ?? null, parentItemId),
        );
        items.push(...toLayoutDto(sized, parentItemId, parentTabId));
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
              onChildrenArranged={(layout) =>
                queueSave(widget.id, layout, widget.id)
              }
              onBodyWidth={(w) => reportGridWidth(widget.id, w)}
              {...callbacks}
            />
          ) : widget.type === WidgetTypes.TabsContainer ? (
            <TabsContainerTile
              widget={widget}
              handleRef={handleRef}
              childWidgets={childrenByContainer.get(widget.id) ?? []}
              color={color}
              isConfiguring={isConfiguring}
              onChildrenArranged={(tabId, layout) =>
                queueSave(`${widget.id}:${tabId}`, layout, widget.id, tabId)
              }
              onBodyWidth={(w) => reportGridWidth(widget.id, w)}
              onSaveTabs={(dto) =>
                callbacks.onSaveTabsContainer?.(widget.id, dto)
              }
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
  /** `handleRef`, when attached to an element, restricts a pointer drag of the tile to
      that element. */
  renderContent: (
    widget: DashboardWidgetDto,
    handleRef: (element: Element | null) => void,
  ) => ReactNode;
  /** Floor on the surface's height, so an empty grid still offers an area a widget can
      be dragged onto. */
  minHeight?: number;
  /** Grid's own padding, not CSS padding on the wrapper, so measured width matches the
      box it renders into. */
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
          w={widget.layout.w}
          h={widget.layout.h}
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
  w,
  h,
  children,
}: {
  id: string;
  group: string;
  isConfiguring: boolean;
  w: number;
  h: number;
  children: (handleRef: (element: Element | null) => void) => ReactNode;
}) {
  const { ref, handleRef, style, isDragging } = useGridItem({ id, group });

  return (
    <div
      ref={ref}
      style={style}
      className={`snapgrid-item${isDragging ? " is-dragging" : ""}`}
    >
      <div className="dashboard-widget">
        <SizeDebugBadge w={w} h={h} />
        {children(handleRef)}
      </div>
      {isConfiguring && <ResizeHandle id={id} group={group} />}
    </div>
  );
}

// TEMP DEBUG: remove once default widget sizes are settled.
function SizeDebugBadge({ w, h }: { w: number; h: number }) {
  return (
    <span
      aria-hidden="true"
      style={{
        position: "absolute",
        top: 2,
        right: 2,
        zIndex: 10,
        padding: "1px 5px",
        borderRadius: 4,
        background: "rgba(0,0,0,0.65)",
        color: "#fff",
        fontSize: 10,
        fontFamily: "monospace",
        lineHeight: "14px",
        pointerEvents: "none",
      }}
    >
      {w}×{h}
    </span>
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
