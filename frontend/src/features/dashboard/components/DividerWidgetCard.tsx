import { Divider } from "@mantine/core";
import { WidgetShell } from "../../analytics/components/WidgetShell";
import { useCardLayout } from "../../analytics/components/cardSizing";

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
export function DividerWidgetCard({
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
            title="Divider"
            compactHeader
            accent
            padding={0}
            bodyProps={{ justify: "center", h: "100%", px: "md", gap: 0 }}
        >
            <Divider color={color} />
        </WidgetShell>
    );
}
