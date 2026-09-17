import { Center, Text } from "@mantine/core";
import { WidgetShell } from "../../analytics/components/WidgetShell";
import { useCardLayout } from "../../analytics/components/cardSizing";

interface Props {
    widgetId: string;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
}

/** Fallback for a widget whose stored type this build no longer knows. */
export function UnknownWidgetCard({
    widgetId,
    color,
    isConfiguring,
    onRemove,
}: Props) {
    const layout = useCardLayout(true);

    return (
        <WidgetShell
            layout={layout}
            fillHeight
            isConfiguring={isConfiguring}
            color={color}
            itemId={widgetId}
            onRemove={onRemove}
            title="Unavailable widget"
            padding="md"
        >
            <Center style={{ flex: 1, minHeight: 0 }}>
                <Text size="sm" c="dimmed">
                    This widget cannot be displayed
                </Text>
            </Center>
        </WidgetShell>
    );
}
