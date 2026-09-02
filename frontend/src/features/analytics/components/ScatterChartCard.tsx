import { ScatterChart } from "@mantine/charts";
import { em, Paper, Stack, Text } from "@mantine/core";
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
    chartTooltipTrigger,
    useCardLayout,
} from "./cardSizing";

interface ScatterChartCardProps {
    analytic: ScatterChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

export function ScatterChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    fillHeight,
}: ScatterChartCardProps) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);

    // The backend returns the analytic with no axis fields when they can no longer be
    // resolved (e.g. a field was deleted). Nothing can be plotted in that case.
    const { xField, yField } = analytic;

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
                    title={analytic.name}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={analytic.id}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
                {xField && yField ? (
                    <ScatterChart
                        tooltipAnimationDuration={200}
                        gridAxis="x"
                        data={[
                            {
                                name: yField.name,
                                color: color ?? "blue",
                                data: analytic.points,
                            },
                        ]}
                        h={chartHeight(fillHeight, isMobile)}
                        {...cardBodyProps(fillHeight)}
                        withXAxis={layout.withXAxis}
                        withYAxis={layout.withYAxis}
                        xAxisProps={{
                            tickFormatter: getAxisFormatter(xField.type),
                        }}
                        yAxisProps={{
                            tickFormatter: getAxisFormatter(yField.type),
                        }}
                        tooltipProps={{
                            trigger: chartTooltipTrigger(isMobile),
                            content: createScatterTooltipContent(
                                analytic,
                                color ?? "blue",
                            ),
                        }}
                        dataKey={{ x: "x", y: "y" }}
                    />
                ) : (
                    <Text size="sm" c="dimmed" ta="center" py="xl">
                        This chart's fields are no longer available.
                    </Text>
                )}
            </Stack>
        </Paper>
    );
}
