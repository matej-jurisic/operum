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
  /** From the container's own grid tile. Attached to the header so the panel is dragged
      by its header alone, leaving pointer drags inside its sub-grid to move the widgets
      in it rather than the whole panel. */
  handleRef: (element: Element | null) => void;
  childWidgets: DashboardWidgetDto[];
  color: string | undefined;
  isConfiguring: boolean;
  onChildrenArranged: (layout: Layout) => void;
}

/**
 * A panel that holds a sub-grid of other widgets. It is itself a tile on the board's grid
 * (moved and resized like any other), and its body is a second grid sharing the board's
 * drag context, so a widget can be dragged straight from the board into it or back out.
 */
export function DashboardContainerTile({
  widget,
  handleRef,
  childWidgets,
  color,
  isConfiguring,
  onChildrenArranged,
  ...callbacks
}: Props) {
  const { width, containerRef, mounted } = useContainerWidth();
  const isEmpty = childWidgets.length === 0;
  const name = parseTextWidgetConfig(widget.config)?.text.trim() || "";
  const hasName = name.length > 0;
  // A named container keeps its header in the layout at all times. A nameless one has no
  // header when the board is at rest and grows one, in the layout, while arranging. It
  // stays in flow rather than floating over the sub-grid so it never sits on top of the
  // widgets in it and makes them harder to grab.
  const showHeader = hasName || isConfiguring;

  return (
    <Paper
      withBorder
      radius="md"
      className="dashboard-container"
      data-editing={isConfiguring || undefined}
    >
      {showHeader && (
        <div ref={handleRef} className="dashboard-container-header">
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
