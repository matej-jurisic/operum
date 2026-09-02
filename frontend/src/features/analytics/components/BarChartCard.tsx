import { BarChart } from "@mantine/charts";
import { em } from "@mantine/core";
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
}

export function BarChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    fillHeight,
}: Props) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);

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
                    tickFormatter: getAxisFormatter(analytic.nameField.type),
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
        </WidgetShell>
    );
}
