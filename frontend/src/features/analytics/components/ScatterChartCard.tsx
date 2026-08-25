import { ScatterChart } from "@mantine/charts";
import { em, Paper, Stack } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { ScatterChartAnalyticDto } from "../types/AnalyticDto";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import {
    createScatterTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";
import {
    cardBodyProps,
    cardShellProps,
    chartHeight,
    useCardLayout,
} from "./cardSizing";

interface ScatterChartCardProps {
    analytic: ScatterChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

export function ScatterChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    fillHeight,
}: ScatterChartCardProps) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);

    return (
        <Paper
            ref={layout.ref}
            withBorder
            p={layout.padding}
            radius="md"
            {...cardShellProps(fillHeight)}
        >
            <Stack gap="xs" {...cardBodyProps(fillHeight)}>
                <AnalyticCardHeader
                    title={`${analytic.name}: ${analytic.xField.name} - ${analytic.yField.name}`}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={analytic.id}
                    onRemove={onRemove}
                />
                <ScatterChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    data={[
                        {
                            name: analytic.yField.name,
                            color: color ?? "blue",
                            data: analytic.points,
                        },
                    ]}
                    h={chartHeight(fillHeight, isMobile)}
                    {...cardBodyProps(fillHeight)}
                    withXAxis={layout.withXAxis}
                    withYAxis={layout.withYAxis}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.xField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.yField.type),
                    }}
                    tooltipProps={{
                        content: createScatterTooltipContent(
                            analytic,
                            color ?? "blue",
                        ),
                    }}
                    dataKey={{ x: "x", y: "y" }}
                />
            </Stack>
        </Paper>
    );
}
