import { BarChart } from "@mantine/charts";
import { em, Paper, Stack } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { BarChartAnalyticDto } from "../types/AnalyticDto";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import {
    createBarChartTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";
import {
    cardBodyProps,
    cardShellProps,
    chartHeight,
    useCardLayout,
} from "./cardSizing";

interface Props {
    analytic: BarChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onRename?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

export function BarChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onRename,
    fillHeight,
}: Props) {
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
                    title={analytic.name}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={analytic.id}
                    onRemove={onRemove}
                    onRename={onRename}
                />
                <BarChart
                    h={chartHeight(fillHeight, isMobile)}
                    {...cardBodyProps(fillHeight)}
                    data={analytic.points}
                    dataKey="name"
                    withXAxis={layout.withXAxis}
                    withYAxis={layout.withYAxis}
                    series={[
                        {
                            name: "value",
                            color: color ?? "blue",
                            label: analytic.valueField?.name ?? "Count",
                        },
                    ]}
                    tooltipAnimationDuration={200}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(
                            analytic.nameField.type,
                        ),
                    }}
                    yAxisProps={{
                        tickFormatter: analytic.valueField
                            ? getAxisFormatter(analytic.valueField.type)
                            : undefined,
                    }}
                    tooltipProps={{
                        content: createBarChartTooltipContent(
                            analytic,
                            color ?? "blue",
                        ),
                    }}
                />
            </Stack>
        </Paper>
    );
}
