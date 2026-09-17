import { useEffect, useMemo, useState } from "react";
import { ActionIcon, Paper, Text, TextInput } from "@mantine/core";
import {
  MdAdd,
  MdChevronLeft,
  MdChevronRight,
  MdClose,
  MdDelete,
  MdDragIndicator,
  MdEdit,
} from "react-icons/md";
import { Layout, useContainerWidth } from "@snapgridjs/react";
import {
  DashboardWidgetDto,
  LayoutVariants,
  parseTabsContainerConfig,
  SaveTabsContainerDto,
  TabDef,
} from "../types/DashboardDto";
import { BoardSubGrid } from "./DashboardGrid";
import {
  CONTAINER_MARGIN,
  CONTAINER_PADDING,
  DashboardTileCallbacks,
} from "./dashboardGridLayout";
import { DashboardWidget } from "./DashboardWidget";
import "./TabsContainerTile.css";

interface Props extends DashboardTileCallbacks {
  widget: DashboardWidgetDto;
  /** From the container's own grid tile, attached to the header so the panel is dragged by
      its header alone. */
  handleRef: (element: Element | null) => void;
  /** Every child of this container, across all tabs. Filtered to the active tab here. */
  childWidgets: DashboardWidgetDto[];
  color: string | undefined;
  isConfiguring: boolean;
  onChildrenArranged: (tabId: string, layout: Layout) => void;
  onSaveTabs: (dto: SaveTabsContainerDto) => void;
  /** The measured width of the tab sub-grid, so the board can rescale a widget dragged in
      from a grid of a different width and keep its on-screen size. */
  onBodyWidth?: (width: number) => void;
}

/** Only the active tab's widgets render; each tab's sub-grid shares the board's drag
 *  context so a widget can be dragged in from the board or another tab. */
export function TabsContainerTile({
  widget,
  handleRef,
  childWidgets,
  color,
  isConfiguring,
  onChildrenArranged,
  onSaveTabs,
  onBodyWidth,
  ...callbacks
}: Props) {
  const { width, containerRef, mounted } = useContainerWidth();

  useEffect(() => {
    if (mounted && width > 0) onBodyWidth?.(width);
  }, [mounted, width, onBodyWidth]);

  const config = useMemo(
    () => parseTabsContainerConfig(widget.config),
    [widget.config],
  );
  const title = config?.title?.trim() ?? "";
  const hasTitle = title.length > 0;
  const tabs = useMemo(() => config?.tabs ?? [], [config]);

  const [activeTabId, setActiveTabId] = useState(tabs[0]?.id);
  const [renamingId, setRenamingId] = useState<string | null>(null);
  const [renameDraft, setRenameDraft] = useState("");

  // Keep the active tab valid as tabs are added and removed.
  useEffect(() => {
    if (tabs.length > 0 && !tabs.some((t) => t.id === activeTabId)) {
      setActiveTabId(tabs[0].id);
    }
  }, [tabs, activeTabId]);

  const toDto = (next: TabDef[], nextTitle = title): SaveTabsContainerDto => ({
    title: nextTitle || undefined,
    tabs: next.map((t) => ({ id: t.id, name: t.name })),
  });

  const addTab = () => {
    onSaveTabs(toDto([...tabs, { id: "", name: `Tab ${tabs.length + 1}` }]));
  };

  const removeTab = (id: string) => {
    if (tabs.length <= 1) return;
    onSaveTabs(toDto(tabs.filter((t) => t.id !== id)));
  };

  const moveTab = (id: string, delta: -1 | 1) => {
    const index = tabs.findIndex((t) => t.id === id);
    const target = index + delta;
    if (index < 0 || target < 0 || target >= tabs.length) return;
    const next = [...tabs];
    [next[index], next[target]] = [next[target], next[index]];
    onSaveTabs(toDto(next));
  };

  const commitRename = () => {
    if (!renamingId) return;
    const name = renameDraft.trim();
    if (name && name !== tabs.find((t) => t.id === renamingId)?.name) {
      onSaveTabs(
        toDto(tabs.map((t) => (t.id === renamingId ? { ...t, name } : t))),
      );
    }
    setRenamingId(null);
  };

  // A child whose tab no longer exists (only reachable from stale data -- the server
  // repoints on tab delete) is shown in the first tab rather than vanishing.
  const isFirstTab = tabs.length > 0 && activeTabId === tabs[0].id;
  const knownTabIds = new Set(tabs.map((t) => t.id));
  const activeChildren = childWidgets.filter(
    (w) =>
      w.parentTabId === activeTabId ||
      (isFirstTab && !knownTabIds.has(w.parentTabId ?? "")),
  );
  const isEmpty = activeChildren.length === 0;
  // An untitled container's header only appears while arranging, and floats over the
  // corner instead of sitting in flow, so entering arrange mode never pushes the tab strip down.
  const showHeader = hasTitle || isConfiguring;
  const floatingHeader = isConfiguring && !hasTitle;

  return (
    <Paper
      withBorder
      radius="md"
      className="tabs-container"
      data-editing={isConfiguring || undefined}
    >
      {showHeader && (
        <div
          ref={handleRef}
          className="tabs-container-header"
          data-floating={floatingHeader || undefined}
        >
          {isConfiguring && (
            <MdDragIndicator
              size={16}
              className="tabs-container-grip"
              aria-hidden="true"
            />
          )}
          <Text
            size="sm"
            fw={600}
            c={hasTitle ? undefined : "dimmed"}
            className="tabs-container-title"
            title={title || "Tabs container"}
          >
            {title || "Tabs container"}
          </Text>
          {isConfiguring && callbacks.onEdit && (
            <ActionIcon
              size="md"
              color={color}
              variant="outline"
              aria-label="Rename tabs container"
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
              aria-label="Remove tabs container"
              onClick={() => callbacks.onRemove?.(widget.id)}
            >
              <MdDelete size={18} />
            </ActionIcon>
          )}
        </div>
      )}

      <div className="tabs-container-tabstrip" role="tablist">
        {tabs.map((tab, index) => {
          const active = tab.id === activeTabId;
          if (isConfiguring && renamingId === tab.id) {
            return (
              <TextInput
                key={tab.id}
                size="xs"
                autoFocus
                value={renameDraft}
                maxLength={40}
                onChange={(e) => setRenameDraft(e.currentTarget.value)}
                onBlur={commitRename}
                onKeyDown={(e) => {
                  if (e.key === "Enter") commitRename();
                  if (e.key === "Escape") setRenamingId(null);
                }}
                className="tabs-container-rename"
              />
            );
          }
          return (
            <div
              key={tab.id}
              className="tabs-container-tab"
              data-active={active || undefined}
            >
              {isConfiguring && (
                <ActionIcon
                  size="xs"
                  variant="subtle"
                  color="gray"
                  aria-label={`Move "${tab.name}" left`}
                  disabled={index === 0}
                  onClick={() => moveTab(tab.id, -1)}
                >
                  <MdChevronLeft size={14} />
                </ActionIcon>
              )}
              <button
                type="button"
                role="tab"
                aria-selected={active}
                className="tabs-container-tab-label"
                onClick={() => setActiveTabId(tab.id)}
                onDoubleClick={() => {
                  if (!isConfiguring) return;
                  setRenamingId(tab.id);
                  setRenameDraft(tab.name);
                }}
              >
                {tab.name}
              </button>
              {isConfiguring && (
                <>
                  <ActionIcon
                    size="xs"
                    variant="subtle"
                    color="gray"
                    aria-label={`Move "${tab.name}" right`}
                    disabled={index === tabs.length - 1}
                    onClick={() => moveTab(tab.id, 1)}
                  >
                    <MdChevronRight size={14} />
                  </ActionIcon>
                  <ActionIcon
                    size="xs"
                    variant="subtle"
                    color="gray"
                    aria-label={`Delete "${tab.name}"`}
                    disabled={tabs.length <= 1}
                    onClick={() => removeTab(tab.id)}
                  >
                    <MdClose size={14} />
                  </ActionIcon>
                </>
              )}
            </div>
          );
        })}
        {isConfiguring && (
          <ActionIcon
            size="sm"
            variant="light"
            color={color}
            aria-label="Add tab"
            onClick={addTab}
          >
            <MdAdd size={16} />
          </ActionIcon>
        )}
      </div>

      <div ref={containerRef} className="tabs-container-body">
        {mounted && activeTabId && (
          <BoardSubGrid
            gridKey={`${widget.id}:${activeTabId}`}
            width={width}
            widgets={activeChildren}
            margin={CONTAINER_MARGIN}
            containerPadding={CONTAINER_PADDING}
            isConfiguring={isConfiguring}
            onArranged={(layout) => onChildrenArranged(activeTabId, layout)}
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
          <Text size="xs" c="dimmed" className="tabs-container-hint">
            {isConfiguring
              ? "Drag widgets here to group them under this tab."
              : "Empty tab"}
          </Text>
        )}
      </div>
    </Paper>
  );
}
