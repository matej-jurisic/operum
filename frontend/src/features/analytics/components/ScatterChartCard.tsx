import { ScatterChart } from "@mantine/charts";
import { Box, em, Text, Tooltip } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdWarningAmber } from "react-icons/md";
import { ScatterChartAnalyticDto } from "../types/AnalyticDto";
import {
    createScatterTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";
import {
    cardBodyProps,
    chartHeight,
    chartTooltipTrigger,
    useCardLayout,
} from "./cardSizing";
import { WidgetShell } from "./WidgetShell";

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
    const warnings = analytic.warnings ?? [];

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
            titleAdornment={
                warnings.length > 0 && (
                    <Tooltip label={warnings.join(" ")} multiline maw={280}>
                        <Box style={{ cursor: "default", display: "flex" }}>
                            <MdWarningAmber
                                size={16}
                                color="var(--mantine-color-yellow-6)"
                            />
                        </Box>
                    </Tooltip>
                )
            }
        >
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
        </WidgetShell>
    );
}
