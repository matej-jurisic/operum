import { BarChart } from "@mantine/charts";
import { em, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { BarChartAnalyticDto } from "../types/AnalyticDto";
import {
    createBarChartTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";
import {
    cardBodyProps,
    chartHeight,
    chartTooltipTrigger,
    useCardLayout,
} from "./cardSizing";
import { WidgetShell } from "./WidgetShell";

interface Props {
    analytic: BarChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
    flat?: boolean;
}

export function BarChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    fillHeight,
    flat,
}: Props) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);

    // nameField is undefined when its source field was deleted.
    const { nameField } = analytic;

    return (
        <WidgetShell
            layout={layout}
            fillHeight={fillHeight}
            flat={flat}
            isConfiguring={isConfiguring}
            color={color}
            itemId={analytic.id}
            onRemove={onRemove}
            onEdit={onEdit}
            title={analytic.name}
        >
            {nameField ? (
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
                        tickFormatter: getAxisFormatter(nameField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: analytic.valueField
                            ? getAxisFormatter(analytic.valueField.type)
                            : undefined,
                    }}
                    tooltipProps={{
                        trigger: chartTooltipTrigger(isMobile),
                        content: createBarChartTooltipContent(
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
