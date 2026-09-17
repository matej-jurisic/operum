import { Box, Group, Progress, Text, em } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { GoalAnalyticDto, GoalDirections } from "../types/AnalyticDto";
import { useCardLayout } from "./cardSizing";
import { TrendSparkline } from "./TrendSparkline";
import { WidgetShell } from "./WidgetShell";

interface Props {
    analytic: GoalAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

export function GoalCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    fillHeight,
}: Props) {
    const layout = useCardLayout(fillHeight);
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const compact = isMobile || layout.isCompact;

    const type = analytic.valueField?.type;
    const hasProgress =
        analytic.progress !== undefined && analytic.progress !== null;
    const percent = hasProgress ? Math.round(analytic.progress! * 100) : null;
    // Backend already inverts the ratio for LowerIsBetter, so "goal met" is progress >= 1 either way.
    const achieved = hasProgress && analytic.progress! >= 1;
    const isLowerIsBetter = analytic.direction === GoalDirections.LowerIsBetter;
    const statusColor = hasProgress && isLowerIsBetter && !achieved ? "red" : color;

    return (
        <WidgetShell
            layout={layout}
            fillHeight={fillHeight}
            isConfiguring={isConfiguring}
            color={color}
            itemId={analytic.id}
            onRemove={onRemove}
            onEdit={onEdit}
            title={analytic.name}
        >
            <Box
                style={
                    fillHeight
                        ? {
                              flex: 1,
                              minHeight: 0,
                              display: "flex",
                              flexDirection: "column",
                              justifyContent: "center",
                              gap: compact ? 6 : 10,
                          }
                        : {
                              display: "flex",
                              flexDirection: "column",
                              gap: compact ? 6 : 10,
                          }
                }
            >
                <Group justify="space-between" align="baseline" gap="xs" wrap="nowrap">
                    <Text fw={700} size={compact ? "lg" : "xl"} style={{ lineHeight: 1.1 }}>
                        {renderValue(type, analytic.value)}
                    </Text>
                    {percent !== null && (
                        <Text
                            fw={600}
                            size="sm"
                            c={achieved || isLowerIsBetter ? statusColor : "dimmed"}
                        >
                            {percent}%
                        </Text>
                    )}
                </Group>

                <Progress
                    value={hasProgress ? Math.min(100, Math.max(0, percent!)) : 0}
                    color={statusColor}
                    size={compact ? "sm" : "lg"}
                    radius="xl"
                    striped={achieved}
                />

                <Text size="xs" c="dimmed">
                    {analytic.target
                        ? isLowerIsBetter
                            ? achieved
                                ? `Under target: ${renderValue(type, analytic.target)}`
                                : `Over target: ${renderValue(type, analytic.target)}`
                            : achieved
                              ? `Target reached: ${renderValue(type, analytic.target)}`
                              : `Target: ${renderValue(type, analytic.target)}`
                        : "No target set"}
                </Text>

                {analytic.trend && (
                    <TrendSparkline
                        trend={analytic.trend}
                        currentValue={analytic.value}
                        valueFieldType={type}
                        color={statusColor}
                        direction={
                            isLowerIsBetter ? "lowerIsBetter" : "higherIsBetter"
                        }
                        compact={compact}
                    />
                )}
            </Box>
        </WidgetShell>
    );
}
