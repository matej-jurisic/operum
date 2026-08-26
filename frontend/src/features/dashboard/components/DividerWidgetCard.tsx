import { Divider, Paper, Stack } from "@mantine/core";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import { cardShellProps, useCardLayout } from "../../analytics/components/cardSizing";

interface Props {
    widgetId: string;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
}

/**
 * A board widget that draws nothing but a line. The grid already lets widgets leave
 * deliberate empty space; this is what turns a gap into something that reads as a
 * dividing line rather than unfinished layout. Carries no config and, unlike every other
 * widget, nothing to edit — only a remove button while the board is being arranged.
 */
export function DividerWidgetCard({ widgetId, color, isConfiguring, onRemove }: Props) {
    const layout = useCardLayout(true);

    return (
        <Paper
            ref={layout.ref}
            withBorder={isConfiguring}
            p={0}
            radius="md"
            w="100%"
            {...cardShellProps(true)}
        >
            <Stack justify="center" h="100%" px="md">
                <AnalyticCardHeader
                    title="Divider"
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={widgetId}
                    onRemove={onRemove}
                    compact
                />
                <Divider color={color} />
            </Stack>
        </Paper>
    );
}
