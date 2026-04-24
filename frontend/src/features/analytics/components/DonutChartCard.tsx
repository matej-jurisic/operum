import { DonutChart } from "@mantine/charts";
import { ActionIcon, Box, em, Group, Paper, Stack, Text, Tooltip } from "@mantine/core";
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

    const { positivePoints, excludedPoints } = useMemo(() => {
        const positive = analytic.points.filter((x) => (x.value ?? 0) > 0);
        const excluded = analytic.points.filter((x) => (x.value ?? 0) <= 0);
        return { positivePoints: positive, excludedPoints: excluded };
    }, [analytic.points]);

    const coloredPoints = useMemo(() => {
        const baseColor = tracker.color ?? "blue";

        return positivePoints.map((x, index) => {
            const opacity = 0.2 + (index / Math.max(positivePoints.length, 1)) * 0.8;

            return {
                name: x.name,
                value: x.value,
                color: `color-mix(in srgb, var(--mantine-color-${baseColor}-6) ${
                    opacity * 100
                }%, white)`,
            };
        });
    }, [positivePoints, tracker.color]);

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Group align="flex-start">
                        <Text size="sm" mb="sm">
                            {`${analytic.name}: ${analytic.nameField.name} - ${analytic.valueField.name}`}
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
                <Box
                    h={isMobile ? 210 : 300}
                    style={{ display: "flex", flexDirection: "column", gap: "var(--mantine-spacing-xs)" }}
                >
                    {excludedPoints.length > 0 && (
                        <Tooltip
                            label={excludedPoints.map((p) => p.name ?? "Unknown").join(", ")}
                            multiline
                            maw={260}
                        >
                            <Text size="xs" c="dimmed" style={{ cursor: "default" }}>
                                {excludedPoints.length} categor{excludedPoints.length === 1 ? "y" : "ies"} not shown (zero or negative value)
                            </Text>
                        </Tooltip>
                    )}
                    <Box style={{ flex: 1, minHeight: 0, display: "flex", justifyContent: "center" }}>
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
                            h={"100%"}
                        />
                    </Box>
                </Box>
            </Stack>
        </Paper>
    );
}
