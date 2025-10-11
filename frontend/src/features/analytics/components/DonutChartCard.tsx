import { DonutChart } from "@mantine/charts";
import { ActionIcon, em, Group, Paper, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useMemo } from "react";
import { MdDelete } from "react-icons/md";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { DonutChartAnaylticDto } from "../types/AnalyticDto";
import { createDonutTooltipContent } from "./ChartFormatters";

interface Props {
    analytic: DonutChartAnaylticDto;
    isConfiguring: boolean;
}

export function DonutChartCard({ analytic, isConfiguring }: Props) {
    const { tracker } = useTracker();
    const { removeAnalytic } = useTrackerOperations();
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);

    const coloredPoints = useMemo(() => {
        const baseColor = tracker.color ?? "blue";

        return analytic.points.map((x, index) => {
            const opacity = 0.2 + (index / analytic.points.length) * 0.8;

            return {
                name: x.name,
                value: x.value,
                color: `color-mix(in srgb, var(--mantine-color-${baseColor}-6) ${
                    opacity * 100
                }%, white)`,
            };
        });
    }, [analytic.points, tracker.color]);

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Text size="sm" mb="sm">
                        {`${analytic.name}: ${analytic.nameField.name} - ${analytic.valueField.name}`}
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
                <Group justify="center">
                    <DonutChart
                        withLabelsLine
                        w={"100%"}
                        withLabels
                        size={isMobile ? 150 : 200}
                        thickness={20}
                        paddingAngle={2}
                        tooltipDataSource="segment"
                        tooltipProps={{
                            content: createDonutTooltipContent(analytic),
                        }}
                        labelsType="percent"
                        tooltipAnimationDuration={200}
                        data={coloredPoints}
                        h={isMobile ? 210 : 300}
                    />
                </Group>
            </Stack>
        </Paper>
    );
}
