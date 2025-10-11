import { ScatterChart } from "@mantine/charts";
import { ActionIcon, em, Group, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdDelete } from "react-icons/md";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { ScatterPlotAnalyticResultDto } from "../types/AnalyticDto";
import {
    createScatterTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";

interface ScatterPlotCardProps {
    analytic: ScatterPlotAnalyticResultDto;
    isConfiguring: boolean;
}

export function ScatterPlotCard({
    analytic,
    isConfiguring,
}: ScatterPlotCardProps) {
    const { tracker } = useTracker();
    const { removeAnalytic } = useTrackerOperations();
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
                            name: analytic.yFieldName,
                            color: tracker.color ?? "blue",
                            data: analytic.points,
                        },
                    ]}
                    h={isMobile ? 210 : 280}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.xFieldType),
                    }}
                    yAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.yFieldType),
                    }}
                    tooltipProps={{
                        content: createScatterTooltipContent(
                            analytic.xFieldType,
                            analytic.yFieldType,
                            analytic.xFieldName,
                            analytic.yFieldName,
                            tracker.color ?? "blue"
                        ),
                    }}
                    dataKey={{ x: "x", y: "y" }}
                />
            </Stack>
        </Paper>
    );
}
