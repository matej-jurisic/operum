import { Center, Paper, Text } from "@mantine/core";
import { AnalyticCard } from "../../analytics/components/AnalyticCard";
import { DashboardWidgetDto, WidgetTypes } from "../types/DashboardDto";

interface Props {
    widget: DashboardWidgetDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    onEntryClick?: (entryId: string) => void;
}

/**
 * Renders one cell of the dashboard grid. Analytics are the only widget kind today, but
 * everything a widget needs to know about the grid stops here: the card below is sized by
 * its cell, and anything added to the switch inherits the same placement and edit mode.
 */
export function DashboardWidget({
    widget,
    color,
    isConfiguring,
    onRemove,
    onEntryClick,
}: Props) {
    switch (widget.type) {
        case WidgetTypes.Analytic:
            return widget.analytic ? (
                <AnalyticCard
                    analytic={widget.analytic}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight
                    onRemove={onRemove}
                    onEntryClick={onEntryClick}
                />
            ) : null;
        default:
            return (
                <Paper withBorder p="md" radius="md" h="100%">
                    <Center h="100%">
                        <Text size="sm" c="dimmed">
                            This widget cannot be displayed
                        </Text>
                    </Center>
                </Paper>
            );
    }
}
