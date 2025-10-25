import { LineChart } from "@mantine/charts";
import { ActionIcon, em, Group, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdDelete } from "react-icons/md";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { LineChartAnalyticDto } from "../types/AnalyticDto";
import { createTooltipContent, getAxisFormatter } from "./ChartFormatters";

interface LineChartCardProps {
    analytic: LineChartAnalyticDto;
    isConfiguring: boolean;
}

export function LineChartCard({ analytic, isConfiguring }: LineChartCardProps) {
    const { tracker } = useTracker();
    const { removeAnalytic } = useTrackerOperations();
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Group align="flex-start">
                        <Text size="sm" mb="sm">
                            {`${analytic.name}: ${analytic.xField.name} - ${analytic.yField.name}`}
                        </Text>
                    </Group>
                    {isConfiguring && (
                        <ActionIcon
                            size="md"
                            color={tracker.color}
                            variant="outline"
                            onClick={() => removeAnalytic(analytic.id)}
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
                    h={isMobile ? 210 : 300}
                    series={[
                        {
                            name: "y",
                            color: tracker.color,
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
                        content: createTooltipContent(
                            analytic,
                            tracker.color ?? "blue"
                        ),
                    }}
                />
            </Stack>
        </Paper>
    );
}
