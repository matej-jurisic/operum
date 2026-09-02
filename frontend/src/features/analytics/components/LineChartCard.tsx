import { LineChart } from "@mantine/charts";
import { em, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LineChartAnalyticDto } from "../types/AnalyticDto";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    chartHeight,
    chartTooltipTrigger,
    useCardLayout,
} from "./cardSizing";
import { createTooltipContent, getAxisFormatter } from "./ChartFormatters";

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
            </Stack>
        </Paper>
    );
}
