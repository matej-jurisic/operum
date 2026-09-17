import { useEffect } from "react";
import { ActionIcon, Paper, Text } from "@mantine/core";
import { MdDelete, MdDragIndicator, MdEdit } from "react-icons/md";
import { Layout, useContainerWidth } from "@snapgridjs/react";
import {
  DashboardWidgetDto,
  LayoutVariants,
  parseTextWidgetConfig,
} from "../types/DashboardDto";
import { BoardSubGrid } from "./DashboardGrid";
import {
  CONTAINER_MARGIN,
  CONTAINER_PADDING,
  DashboardTileCallbacks,
} from "./dashboardGridLayout";
import { DashboardWidget } from "./DashboardWidget";
import "./DashboardContainerTile.css";

interface Props extends DashboardTileCallbacks {
  widget: DashboardWidgetDto;
  /** Attached to the header so only it drags the panel; pointer drags in the sub-grid move
      the widgets inside instead. */
  handleRef: (element: Element | null) => void;
  childWidgets: DashboardWidgetDto[];
  color: string | undefined;
  isConfiguring: boolean;
  onChildrenArranged: (layout: Layout) => void;
  /** Measured sub-grid width, so a widget dragged in from a grid of a different width can
      be rescaled to keep its on-screen size. */
  onBodyWidth?: (width: number) => void;
}

/** A tile on the board's grid whose body is a second grid sharing the board's drag
 *  context, so widgets can be dragged into or out of it. */
export function DashboardContainerTile({
  widget,
  handleRef,
  childWidgets,
  color,
  isConfiguring,
  onChildrenArranged,
  onBodyWidth,
  ...callbacks
}: Props) {
  const { width, containerRef, mounted } = useContainerWidth();

  useEffect(() => {
    if (mounted && width > 0) onBodyWidth?.(width);
  }, [mounted, width, onBodyWidth]);

  const isEmpty = childWidgets.length === 0;
  const name = parseTextWidgetConfig(widget.config)?.text.trim() || "";
  const hasName = name.length > 0;
  // A nameless container's header only appears while arranging, and floats over the
  // corner instead of sitting in flow, so entering arrange mode never resizes the sub-grid.
  const showHeader = hasName || isConfiguring;
  const floatingHeader = isConfiguring && !hasName;

  return (
    <Paper
      withBorder
      radius="md"
      className="dashboard-container"
      data-editing={isConfiguring || undefined}
    >
      {showHeader && (
        <div
          ref={handleRef}
          className="dashboard-container-header"
          data-floating={floatingHeader || undefined}
        >
          {isConfiguring && (
            <MdDragIndicator
              size={16}
              className="dashboard-container-grip"
              aria-hidden="true"
            />
          )}
          <Text
            size="sm"
            fw={600}
            c={hasName ? undefined : "dimmed"}
            className="dashboard-container-title"
            title={name || "Container"}
          >
            {name || "Container"}
          </Text>
          {isConfiguring && callbacks.onEdit && (
            <ActionIcon
              size="md"
              color={color}
              variant="outline"
              aria-label="Rename container"
              onClick={() => callbacks.onEdit?.(widget.id)}
            >
              <MdEdit size={18} />
            </ActionIcon>
          )}
          {isConfiguring && callbacks.onRemove && (
            <ActionIcon
              size="md"
              color={color}
              variant="outline"
              aria-label="Remove container"
              onClick={() => callbacks.onRemove?.(widget.id)}
            >
              <MdDelete size={18} />
            </ActionIcon>
          )}
        </div>
      )}

      <div ref={containerRef} className="dashboard-container-body">
        {mounted && (
          <BoardSubGrid
            gridKey={widget.id}
            width={width}
            widgets={childWidgets}
            margin={CONTAINER_MARGIN}
            containerPadding={CONTAINER_PADDING}
            isConfiguring={isConfiguring}
            onArranged={onChildrenArranged}
            minHeight={96}
            renderContent={(child) => (
              <DashboardWidget
                widget={child}
                variant={LayoutVariants.Desktop}
                color={color}
                isConfiguring={isConfiguring}
                {...callbacks}
              />
            )}
          />
        )}
        {isEmpty && (
          <Text size="xs" c="dimmed" className="dashboard-container-hint">
            {isConfiguring
              ? "Drag widgets here to group them."
              : "Empty container"}
          </Text>
        )}
      </div>
    </Paper>
  );
}
