import { LineChart } from "@mantine/charts";
import { em, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LineChartAnalyticDto } from "../types/AnalyticDto";
import {
    cardBodyProps,
    chartHeight,
    chartTooltipTrigger,
    useCardLayout,
} from "./cardSizing";
import { createTooltipContent, getAxisFormatter } from "./ChartFormatters";
import { WidgetShell } from "./WidgetShell";

interface LineChartCardProps {
    analytic: LineChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

export function LineChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    fillHeight,
}: LineChartCardProps) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);

    // The backend returns the analytic with no axis fields when they can no longer be
    // resolved (e.g. a field was deleted). Nothing can be plotted in that case.
    const { xField, yField } = analytic;

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
            {xField && yField ? (
                <LineChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    data={analytic.points}
                    dataKey="x"
                    h={chartHeight(fillHeight, isMobile)}
                    {...cardBodyProps(fillHeight)}
                    withXAxis={layout.withXAxis}
                    withYAxis={layout.withYAxis}
                    // A dot per point is a reading aid at full size and a solid smear of
                    // them once the same series is drawn across a couple of cells.
                    withDots={!layout.isCompact}
                    series={[
                        {
                            name: "y",
                            color: color,
                            label: yField.name,
                        },
                    ]}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(xField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: getAxisFormatter(yField.type),
                        // Anchored at zero by default; fitted to the data's own range
                        // when the widget opts out, so a series that only ever moves
                        // between e.g. 1000 and 1100 isn't a flat line pinned to the top.
                        domain: analytic.yAxisFromZero
                            ? [0, "auto"]
                            : ["auto", "auto"],
                    }}
                    tooltipProps={{
                        trigger: chartTooltipTrigger(isMobile),
                        content: createTooltipContent(
                            analytic,
                            color ?? "blue",
                        ),
                    }}
                />
            ) : (
                <Text size="sm" c="dimmed" ta="center" py="xl">
                    This chart's fields are no longer available.
                </Text>
            )}
        </WidgetShell>
    );
}
