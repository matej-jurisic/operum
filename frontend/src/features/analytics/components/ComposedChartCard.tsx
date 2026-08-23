import { CompositeChart } from "@mantine/charts";
import {
    ActionIcon,
    Box,
    em,
    Group,
    Paper,
    Stack,
    Text,
    Tooltip,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useMemo } from "react";
import { MdDelete, MdWarningAmber } from "react-icons/md";
import { ComposedChartAnalyticDto } from "../types/AnalyticDto";
import { createComposedTooltipContent, getAxisFormatter } from "./ChartFormatters";

interface Props {
    analytic: ComposedChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
}

// Cycled per series; the board color is reserved for the first series so a combined
// chart still leads with the dashboard's own accent color.
const SERIES_COLORS = ["blue", "orange", "teal", "grape", "yellow", "red", "cyan", "pink"];

export function ComposedChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
}: Props) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);

    // Union of every series' x labels, sorted. Sources may bucket by different
    // semantics (dates vs. category names, monthly vs. yearly, ...) — see the
    // `warnings` surfaced below — so this is a best-effort shared axis, not a
    // guaranteed-meaningful one.
    const data = useMemo(() => {
        const xValues = new Set<string>();
        analytic.series.forEach((s) => s.points.forEach((p) => xValues.add(p.x)));
        const sortedX = Array.from(xValues).sort((a, b) => a.localeCompare(b));

        return sortedX.map((x) => {
            const row: Record<string, string | number | null> = { x };
            analytic.series.forEach((s) => {
                row[s.key] = s.points.find((p) => p.x === x)?.y ?? null;
            });
            return row;
        });
    }, [analytic.series]);

    const chartSeries = analytic.series.map((s, index) => ({
        name: s.key,
        type: s.renderType,
        color: index === 0 ? color ?? SERIES_COLORS[0] : SERIES_COLORS[index % SERIES_COLORS.length],
        label: s.label,
    }));

    const subtitle = analytic.series.map((s) => s.label).join(" + ");

    // Same best-effort caveat as the tooltip: the shared axis is formatted using the
    // first series' field type, which may not hold for every mixed-semantics series.
    const xAxisFormatter = analytic.series[0]?.xField
        ? getAxisFormatter(analytic.series[0].xField.type)
        : undefined;

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Group align="flex-start" gap="xs" wrap="nowrap">
                        <Text size="sm" mb="sm">
                            {subtitle}
                        </Text>
                        {analytic.warnings.length > 0 && (
                            <Tooltip label={analytic.warnings.join(" ")} multiline maw={280}>
                                <Box style={{ cursor: "default", display: "flex" }}>
                                    <MdWarningAmber size={16} color="var(--mantine-color-yellow-6)" />
                                </Box>
                            </Tooltip>
                        )}
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
                <CompositeChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    data={data}
                    dataKey="x"
                    h={isMobile ? 210 : 300}
                    series={chartSeries}
                    xAxisProps={{ tickFormatter: xAxisFormatter }}
                    tooltipProps={{
                        content: createComposedTooltipContent(analytic),
                    }}
                />
            </Stack>
        </Paper>
    );
}
