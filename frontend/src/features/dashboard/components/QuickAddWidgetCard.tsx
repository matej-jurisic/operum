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
import {
  useCardLayout,
  useSyncedElementSize,
} from "../../analytics/components/cardSizing";
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
  const buttonBox = useSyncedElementSize<HTMLDivElement>(true);
  const [dialogTracker, setDialogTracker] = useState<TrackerDto>();
  const { refreshWidgets } = useDashboard();

  const trackerColor = tracker?.color || color;

  // The button stacks an icon over the tracker name. As the widget is dragged
  // smaller the icon gives up room first, then its "+" badge, then the name
  // drops to a single line, so the content stays inside the cell instead of
  // spilling over the widget below.
  const measured = buttonBox.width > 0 && buttonBox.height > 0;
  const iconSize = measured
    ? Math.max(0, Math.min(40, buttonBox.height * 0.4, buttonBox.width * 0.55))
    : 40;
  const showIcon = !measured || iconSize >= 22;
  const showBadge = !measured || iconSize >= 34;
  const stackGap = iconSize >= 34 ? 6 : 4;
  const nameLines = measured && buttonBox.height < 48 ? 1 : 2;

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
      <Center
        ref={buttonBox.ref}
        style={{ flex: 1, minHeight: 0, width: "100%", zIndex: 1 }}
      >
        {tracker ? (
          <Button
            color={trackerColor}
            disabled={isConfiguring}
            variant="outline"
            radius="md"
            w={"100%"}
            h={"100%"}
            p={8}
            style={{
              pointerEvents: isConfiguring ? "none" : "all",
            }}
            onClick={handleOpen}
          >
            <Stack align="center" gap={stackGap} maw="100%">
              {showIcon &&
                (showBadge ? (
                  <Indicator
                    label={<TbPlus size={10} />}
                    color={trackerColor}
                    size={16}
                    offset={2}
                  >
                    <ThemeIcon
                      size={iconSize}
                      radius="xl"
                      variant="light"
                      color={trackerColor}
                    >
                      {createElement(resolveTrackerIcon(tracker.icon), {
                        size: iconSize * 0.5,
                      })}
                    </ThemeIcon>
                  </Indicator>
                ) : (
                  <ThemeIcon
                    size={iconSize}
                    radius="xl"
                    variant="light"
                    color={trackerColor}
                  >
                    {createElement(resolveTrackerIcon(tracker.icon), {
                      size: iconSize * 0.5,
                    })}
                  </ThemeIcon>
                ))}
              <Text
                fw={500}
                size="sm"
                ta="center"
                lineClamp={nameLines}
                style={{ lineHeight: 1.3, maxWidth: "100%" }}
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
