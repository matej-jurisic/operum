import { ScatterChart } from "@mantine/charts";
import { ActionIcon, em, Group, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdDelete } from "react-icons/md";
import { ScatterChartAnalyticDto } from "../types/AnalyticDto";
import {
    createScatterTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";
import { cardBodyProps, cardShellProps, chartHeight } from "./cardSizing";

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

    return (
        <Paper withBorder p="md" radius="md" {...cardShellProps(fillHeight)}>
            <Stack gap="xs" {...cardBodyProps(fillHeight)}>
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Group align="flex-start">
                        <Text size="sm" mb="sm">
                            {`${analytic.name}: ${analytic.xField.name} - ${analytic.yField.name}`}
                        </Text>
                    </Group>
                    {isConfiguring && onRemove && (
                        <ActionIcon
                            size="md"
                            color={color}
                            variant="outline"
                            onClick={() => onRemove(analytic.id)}
                        >
                            <MdDelete size={18} />
                        </ActionIcon>
                    )}
                </Group>
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
