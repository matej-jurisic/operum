import { ScatterChart } from "@mantine/charts";
import { ActionIcon, em, Group, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdDelete } from "react-icons/md";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { ScatterChartAnalyticDto } from "../types/AnalyticDto";
import {
    createScatterTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";

interface ScatterChartCardProps {
    analytic: ScatterChartAnalyticDto;
    isConfiguring: boolean;
}

export function ScatterChartCard({
    analytic,
    isConfiguring,
}: ScatterChartCardProps) {
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
                <ScatterChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    data={[
                        {
                            name: analytic.yField.name,
                            color: tracker.color ?? "blue",
                            data: analytic.points,
                        },
                    ]}
                    h={isMobile ? 210 : 300}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.xField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.yField.type),
                    }}
                    tooltipProps={{
                        content: createScatterTooltipContent(
                            analytic,
                            tracker.color ?? "blue"
                        ),
                    }}
                    dataKey={{ x: "x", y: "y" }}
                />
            </Stack>
        </Paper>
    );
}
