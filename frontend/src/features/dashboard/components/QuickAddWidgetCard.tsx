import { Button, Center, Loader, Paper, Stack, Text } from "@mantine/core";
import { createElement, useEffect, useState } from "react";
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
import { QuickAddWidgetConfig } from "../types/DashboardDto";

interface Props {
  widgetId: string;
  config: QuickAddWidgetConfig;
  /** The board's color, used until the tracker itself has loaded. */
  color: string | undefined;
  isConfiguring: boolean;
  onRemove?: (itemId: string) => void;
}

/**
 * A board widget that is a shortcut rather than a chart: pressing it opens the same
 * QuickAddEntryDialog the tracker's own page uses. The widget only carries a tracker id
 * (see QuickAddWidgetConfig), so the tracker — and with it the icon, color and fields the
 * dialog needs — is fetched once the card mounts.
 */
export function QuickAddWidgetCard({
  widgetId,
  config,
  color,
  isConfiguring,
  onRemove,
}: Props) {
  const layout = useCardLayout(true);
  const [tracker, setTracker] = useState<TrackerDto>();
  const [isLoading, setIsLoading] = useState(true);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    trackersController
      .getTracker(config.trackerId)
      .then((res) => {
        if (!cancelled) setTracker(res.data);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [config.trackerId]);

  const trackerColor = tracker?.color || color;
  const Icon = resolveTrackerIcon(tracker?.icon);

  return (
    <Paper
      ref={layout.ref}
      withBorder
      p={"xs"}
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
        <Center style={{ flex: 1, minHeight: 0 }}>
          {isLoading ? (
            <Loader color={trackerColor} size="sm" />
          ) : tracker ? (
            <Button
              color={trackerColor}
              variant="light"
              radius="md"
              leftSection={createElement(Icon, { size: 18 })}
              onClick={() => setIsDialogOpen(true)}
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

      {isDialogOpen && tracker && (
        <QuickAddEntryDialog
          tracker={tracker}
          onClose={() => setIsDialogOpen(false)}
        />
      )}
    </Paper>
  );
}
