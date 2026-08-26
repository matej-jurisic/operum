import { Paper, ScrollArea, Stack, Text } from "@mantine/core";
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
 * A board widget that draws no data at all: a free-form block of text for context that
 * isn't any tracker's own — a reminder, a link, a note to whoever else can see the board.
 * Editing it opens the same kind of dialog a Header widget does; the card itself only
 * ever displays it.
 */
export function NoteWidgetCard({
    widgetId,
    config,
    color,
    isConfiguring,
    onRemove,
    onEdit,
}: Props) {
    const layout = useCardLayout(true);

    return (
        <Paper ref={layout.ref} withBorder p={layout.padding} radius="md" {...cardShellProps(true)}>
            <Stack gap="xs" {...cardBodyProps(true)}>
                <AnalyticCardHeader
                    title="Note"
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={widgetId}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
                <ScrollArea
                    style={{
                        ...cardBodyProps(true).style,
                        // Not a control the board can drag by, but arranging the board
                        // still takes over every pointer gesture inside it — same as the
                        // Entries widget's table.
                        pointerEvents: isConfiguring ? "none" : "auto",
                    }}
                >
                    <Text size="sm" style={{ whiteSpace: "pre-wrap" }}>
                        {config?.text ?? "This note is empty."}
                    </Text>
                </ScrollArea>
            </Stack>
        </Paper>
    );
}
