import { LineChart } from "@mantine/charts";
import { ActionIcon, em, Group, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdDelete } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import { NumericChartAnalyticResultDto } from "../model/AnalyticResultDto";
import { createTooltipContent, getAxisFormatter } from "./ChartFormatters";

interface LineChartCardProps {
    analytic: NumericChartAnalyticResultDto;
    isConfiguring: boolean;
}

export function LineChartCard({ analytic, isConfiguring }: LineChartCardProps) {
    const { tracker, RemoveAnalyticFromTracker } = useTracker();
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Text size="sm" mb="sm">
                        {`${analytic.name}: ${analytic.xFieldName} - ${analytic.yFieldName}`}
                    </Text>
                    {isConfiguring && (
                        <ActionIcon
                            size="md"
                            color={tracker.color}
                            variant="outline"
                            onClick={() =>
                                RemoveAnalyticFromTracker(analytic.analyticId)
                            }
                        >
                            <MdDelete size={18} />
                        </ActionIcon>
                    )}
                </Group>
                <LineChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    data={analytic.points}
                    dataKey="x"
                    h={isMobile ? 210 : 280}
                    series={[
                        {
                            name: "y",
                            color: tracker.color,
                            label: analytic.yFieldName,
                        },
                    ]}
                    yAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.yFieldType),
                    }}
                    tooltipProps={{
                        content: createTooltipContent(
                            analytic.yFieldType,
                            analytic.yFieldName,
                            tracker.color ?? "blue"
                        ),
                    }}
                />
            </Stack>
        </Paper>
    );
}
