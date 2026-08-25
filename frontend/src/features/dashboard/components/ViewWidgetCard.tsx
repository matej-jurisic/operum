import { Center, Paper, Select, Stack, Text } from "@mantine/core";
import { createElement } from "react";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
  cardBodyProps,
  cardShellProps,
  useCardLayout,
} from "../../analytics/components/cardSizing";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { ViewWidgetDto } from "../types/DashboardDto";

const ALL_ENTRIES_VALUE = "";

interface Props {
  widgetId: string;
  /** The tracker/views/current selection this dropdown renders from, resolved by the
      board itself — the card never fetches this just to draw its own options. */
  viewWidget: ViewWidgetDto | undefined;
  color: string | undefined;
  isConfiguring: boolean;
  onRemove?: (itemId: string) => void;
  /** Persists the new selection and recomputes every widget linked to this one. */
  onSelect: (itemId: string, viewId: string | null) => void;
}

/**
 * A board widget that is a live filter rather than a chart: its dropdown picks one of a
 * tracker's views, and any analytic widget whose source was linked to it (at add time,
 * see CustomAnalyticForm) is recalculated against whatever it's set to. The selection is
 * saved on the widget itself, so it's what every viewer sees on the next load too, not
 * just a client-side toggle.
 *
 * The card is the dropdown and nothing else: its tracker is already named by the option
 * showing in it, so the header is compact — present only for the remove button in edit
 * mode — and the tracker's own icon stands in for a title.
 */
export function ViewWidgetCard({
  widgetId,
  viewWidget,
  color,
  isConfiguring,
  onRemove,
  onSelect,
}: Props) {
  const layout = useCardLayout(true);
  const trackerColor = viewWidget?.color || color;
  const Icon = resolveTrackerIcon(viewWidget?.icon);

  const options = [
    { value: ALL_ENTRIES_VALUE, label: "All entries" },
    ...(viewWidget?.views.map((v) => ({ value: v.id, label: v.name })) ?? []),
  ];

  return (
    <Paper
      ref={layout.ref}
      withBorder={isConfiguring}
      p={0}
      radius="md"
      w="100%"
      {...cardShellProps(true)}
    >
      <Stack
        gap="xs"
        justify="center"
        {...cardBodyProps(true)}
        h={"100%"}
        style={{ position: "relative" }}
      >
        <AnalyticCardHeader
          title={viewWidget?.trackerName ?? "View"}
          layout={layout}
          color={trackerColor}
          isConfiguring={isConfiguring}
          analyticId={widgetId}
          onRemove={onRemove}
          compact
        />
        <Center
          style={{
            flex: 1,
            minHeight: 0,
            zIndex: 1,
            // While the board is being arranged the dropdown is not a control any
            // more, it is part of the widget being dragged. Disabling it is not
            // enough: the grid refuses to start a drag on an input (its cancel
            // selector), so the card would have nothing left to grab it by.
            pointerEvents: isConfiguring ? "none" : "auto",
          }}
        >
          {viewWidget ? (
            <Select
              w="100%"
              leftSection={createElement(Icon, { size: 16 })}
              data={options}
              value={viewWidget.viewId ?? ALL_ENTRIES_VALUE}
              onChange={(value) =>
                onSelect(widgetId, value && value !== ALL_ENTRIES_VALUE ? value : null)
              }
              disabled={isConfiguring}
              allowDeselect={false}
              comboboxProps={{ withinPortal: true }}
            />
          ) : (
            <Text size="sm" c="dimmed" ta="center">
              This tracker is no longer available.
            </Text>
          )}
        </Center>
      </Stack>
    </Paper>
  );
}
