import { BarChart } from "@mantine/charts";
import { ActionIcon, em, Group, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdDelete } from "react-icons/md";
import { BarChartAnalyticDto } from "../types/AnalyticDto";
import {
    createBarChartTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";
import { cardBodyProps, cardShellProps, chartHeight } from "./cardSizing";

interface Props {
    analytic: BarChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

export function BarChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    fillHeight,
}: Props) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);

    const subtitle = analytic.valueField
        ? `${analytic.nameField.name} - ${analytic.valueField.name}`
        : analytic.nameField.name;

    return (
        <Paper withBorder p="md" radius="md" {...cardShellProps(fillHeight)}>
            <Stack gap="xs" {...cardBodyProps(fillHeight)}>
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Text size="sm" mb="sm">
                        {`${analytic.name}: ${subtitle}`}
                    </Text>
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
                <BarChart
                    h={chartHeight(fillHeight, isMobile)}
                    {...cardBodyProps(fillHeight)}
                    data={analytic.points}
                    dataKey="name"
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
