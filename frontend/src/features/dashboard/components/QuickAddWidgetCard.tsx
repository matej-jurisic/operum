import { Button, Center, Paper, Stack, Text } from "@mantine/core";
import { createElement, useState } from "react";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
  cardBodyProps,
  cardShellProps,
  useCardLayout,
} from "../../analytics/components/cardSizing";
import QuickAddEntryDialog from "../../entries/components/QuickAddEntryDialog";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import {
  QuickAddTrackerDto,
  QuickAddWidgetConfig,
} from "../types/DashboardDto";

interface Props {
  widgetId: string;
  config: QuickAddWidgetConfig;
  /** The name/color/icon to render the button with, resolved by the board itself —
      the card never fetches this just to draw its own button. */
  tracker: QuickAddTrackerDto | undefined;
  /** The board's color, used only if the tracker carries none of its own. */
  color: string | undefined;
  isConfiguring: boolean;
  onRemove?: (itemId: string) => void;
}

/**
 * A board widget that is a shortcut rather than a chart: pressing it opens the same
 * QuickAddEntryDialog the tracker's own page uses. The button itself renders from the
 * tracker summary the board already fetched; only the fields the dialog needs to build
 * its form are fetched, and only once the button is actually pressed.
 */
export function QuickAddWidgetCard({
  widgetId,
  config,
  tracker,
  color,
  isConfiguring,
  onRemove,
}: Props) {
  const layout = useCardLayout(true);
  const [dialogTracker, setDialogTracker] = useState<TrackerDto>();
  const [isOpening, setIsOpening] = useState(false);

  const trackerColor = tracker?.color || color;
  const Icon = resolveTrackerIcon(tracker?.icon);

  const handleOpen = async () => {
    setIsOpening(true);
    try {
      const res = await trackersController.getTracker(config.trackerId);
      setDialogTracker(res.data);
    } finally {
      setIsOpening(false);
    }
  };

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
          title={tracker?.name ?? "Quick add"}
          layout={layout}
          color={trackerColor}
          isConfiguring={isConfiguring}
          analyticId={widgetId}
          onRemove={onRemove}
          compact
        />
        <Center style={{ flex: 1, minHeight: 0, zIndex: 1 }}>
          {tracker ? (
            <Button
              color={trackerColor}
              disabled={isConfiguring}
              variant="light"
              radius="md"
              w={"100%"}
              h={"100%"}
              style={{
                pointerEvents: isConfiguring ? "none" : "all",
              }}
              loading={isOpening}
              leftSection={createElement(Icon, { size: 18 })}
              onClick={handleOpen}
            >
              Add entry
            </Button>
          ) : (
            <Text size="sm" c="dimmed" ta="center">
              This tracker is no longer available.
            </Text>
          )}
        </Center>
      </Stack>

      {dialogTracker && (
        <QuickAddEntryDialog
          tracker={dialogTracker}
          onClose={() => setDialogTracker(undefined)}
        />
      )}
    </Paper>
  );
}
