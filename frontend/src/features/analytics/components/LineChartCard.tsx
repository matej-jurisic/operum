import { LineChart } from "@mantine/charts";
import { em, Paper, Stack } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LineChartAnalyticDto } from "../types/AnalyticDto";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    cardTitle,
    chartHeight,
    useCardLayout,
} from "./cardSizing";
import { createTooltipContent, getAxisFormatter } from "./ChartFormatters";

interface LineChartCardProps {
    analytic: LineChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onRename?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

export function LineChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onRename,
    fillHeight,
}: LineChartCardProps) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);

    const subtitle = `${analytic.xField.name} - ${analytic.yField.name}`;

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
                    title={cardTitle(layout, analytic.name, subtitle)}
                    fullTitle={`${analytic.name}: ${subtitle}`}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={analytic.id}
                    onRemove={onRemove}
                    onRename={onRename}
                />
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
                            label: analytic.yField.name,
                        },
                    ]}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.xField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.yField.type),
                    }}
                    tooltipProps={{
                        content: createTooltipContent(analytic, color ?? "blue"),
                    }}
                />
            </Stack>
        </Paper>
    );
}
