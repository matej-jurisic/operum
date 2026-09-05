import {
  Button,
  Center,
  Indicator,
  Stack,
  Text,
  ThemeIcon,
} from "@mantine/core";
import { createElement, useState } from "react";
import { TbPlus } from "react-icons/tb";
import { useCardLayout } from "../../analytics/components/cardSizing";
import { WidgetShell } from "../../analytics/components/WidgetShell";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import QuickAddEntryDialog from "../../entries/components/QuickAddEntryDialog";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useDashboard } from "../context/DashboardContext";
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
  const { refreshWidgets } = useDashboard();

  const trackerColor = tracker?.color || color;

  const handleOpen = async () => {
    const res = await trackersController.getTracker(config.trackerId);
    setDialogTracker(res.data);
  };

  return (
    <WidgetShell
      layout={layout}
      fillHeight
      isConfiguring={isConfiguring}
      color={trackerColor}
      itemId={widgetId}
      onRemove={onRemove}
      title={tracker?.name ?? "Quick add"}
      compactHeader
      accent
      padding={0}
      bodyProps={{
        justify: "center",
        h: "100%",
        style: { position: "relative" },
      }}
      after={
        dialogTracker && (
          <QuickAddEntryDialog
            tracker={dialogTracker}
            onClose={() => setDialogTracker(undefined)}
            onCreated={refreshWidgets}
          />
        )
      }
    >
      <Center style={{ flex: 1, minHeight: 0, zIndex: 1 }}>
        {tracker ? (
          <Button
            color={trackerColor}
            disabled={isConfiguring}
            variant="subtle"
            radius="md"
            w={"100%"}
            h={"100%"}
            style={{
              pointerEvents: isConfiguring ? "none" : "all",
            }}
            onClick={handleOpen}
          >
            <Stack align="center" gap={6}>
              <Indicator
                label={<TbPlus size={10} />}
                color={trackerColor}
                size={16}
                offset={2}
              >
                <ThemeIcon
                  size={40}
                  radius="xl"
                  variant="light"
                  color={trackerColor}
                >
                  {createElement(resolveTrackerIcon(tracker.icon), {
                    size: 20,
                  })}
                </ThemeIcon>
              </Indicator>
              <Text
                fw={500}
                size="sm"
                ta="center"
                lineClamp={2}
                style={{ lineHeight: 1.3 }}
              >
                {tracker.name}
              </Text>
            </Stack>
          </Button>
        ) : (
          <Text size="sm" c="dimmed" ta="center">
            This tracker is no longer available.
          </Text>
        )}
      </Center>
    </WidgetShell>
  );
}
