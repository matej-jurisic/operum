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

    // xField/yField are undefined when their source fields were deleted; a Count calculation legitimately has no yField.
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
            {xField ? (
                <LineChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    data={analytic.points}
                    dataKey="x"
                    h={chartHeight(fillHeight, isMobile)}
                    {...cardBodyProps(fillHeight)}
                    withXAxis={layout.withXAxis}
                    withYAxis={layout.withYAxis}
                    withDots={!layout.isCompact}
                    series={[
                        {
                            name: "y",
                            color: color,
                            label: yField?.name ?? "Count",
                        },
                    ]}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(xField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: yField
                            ? getAxisFormatter(yField.type)
                            : undefined,
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
