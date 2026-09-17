import { Divider } from "@mantine/core";
import { WidgetShell } from "../../analytics/components/WidgetShell";
import { useCardLayout } from "../../analytics/components/cardSizing";

interface Props {
    widgetId: string;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
}

/** Carries no config and, unlike every other widget, nothing to edit. */
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
