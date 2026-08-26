import { Center, Paper, Stack, Text } from "@mantine/core";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";
import { TextWidgetConfig } from "../types/DashboardDto";

interface Props {
    widgetId: string;
    config: TextWidgetConfig | null;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    onEdit?: (itemId: string) => void;
}

/**
 * A board widget that draws no data at all: a short line of text meant to read as a
 * section title, the way a heading breaks up the board's deliberate empty space into
 * named regions instead of looking like unfinished layout.
 *
 * The text is the whole card, so — like QuickAdd and View — its header is compact: present
 * only to carry the edit/remove icons while the board is being arranged.
 */
export function HeaderWidgetCard({
    widgetId,
    config,
    color,
    isConfiguring,
    onRemove,
    onEdit,
}: Props) {
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
            <Stack gap="xs" justify="center" {...cardBodyProps(true)} h="100%">
                <AnalyticCardHeader
                    title={config?.text ?? "Header"}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={widgetId}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    compact
                />
                <Center px="md" style={{ flex: 1, minHeight: 0 }}>
                    <Text
                        fw={700}
                        size={layout.isCompact ? "md" : "xl"}
                        ta="center"
                        style={{ wordBreak: "break-word" }}
                    >
                        {config?.text ?? "Untitled header"}
                    </Text>
                </Center>
            </Stack>
        </Paper>
    );
}
