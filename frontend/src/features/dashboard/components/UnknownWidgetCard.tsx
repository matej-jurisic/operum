import { Center, Paper, Text } from "@mantine/core";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import { cardShellProps, useCardLayout } from "../../analytics/components/cardSizing";

interface Props {
    widgetId: string;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
}

/**
 * The fallback for a widget whose stored type this build no longer knows — a leftover of a
 * widget kind that was removed or merged away before its rows were cleaned up. It can't be
 * rendered, but while the board is being arranged it still needs a remove button so the
 * user has a way to clear it.
 */
export function UnknownWidgetCard({ widgetId, color, isConfiguring, onRemove }: Props) {
    const layout = useCardLayout(true);

    return (
        <Paper
            ref={layout.ref}
            withBorder
            p="md"
            radius="md"
            w="100%"
            {...cardShellProps(true)}
        >
            <AnalyticCardHeader
                title="Unavailable widget"
                layout={layout}
                color={color}
                isConfiguring={isConfiguring}
                analyticId={widgetId}
                onRemove={onRemove}
            />
            <Center style={{ flex: 1, minHeight: 0 }}>
                <Text size="sm" c="dimmed">
                    This widget cannot be displayed
                </Text>
            </Center>
        </Paper>
    );
}
