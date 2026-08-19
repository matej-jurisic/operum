import { BarChart } from "@mantine/charts";
import { ActionIcon, em, Group, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdDelete } from "react-icons/md";
import { BarChartAnalyticDto } from "../types/AnalyticDto";
import {
    createBarChartTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";

interface Props {
    analytic: BarChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
}

export function BarChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
}: Props) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);

    const subtitle = analytic.valueField
        ? `${analytic.nameField.name} - ${analytic.valueField.name}`
        : analytic.nameField.name;

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
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
                    h={isMobile ? 210 : 300}
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
