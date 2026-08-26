import { CompositeChart } from "@mantine/charts";
import { Box, em, Paper, Stack, Tooltip } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useMemo, useRef } from "react";
import { MdWarningAmber } from "react-icons/md";
import { ComposedChartAnalyticDto } from "../types/AnalyticDto";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    chartHeight,
    useCardLayout,
} from "./cardSizing";
import { createComposedTooltipContent, getAxisFormatter } from "./ChartFormatters";

interface Props {
    analytic: ComposedChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

// Fallback only, for a series whose tracker carries no color of its own: cycled per
// series, with the board color reserved for the first one so a combined chart still leads
// with the dashboard's own accent color absent anything more specific to draw it with.
const SERIES_COLORS = ["blue", "orange", "teal", "grape", "yellow", "red", "cyan", "pink"];

export function ComposedChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    fillHeight,
}: Props) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);
    const chartRef = useRef<HTMLDivElement>(null);

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
        // Each line/bar is colored like the tracker it came from, so a combined chart
        // still reads as "which tracker" at a glance. Only a series whose tracker has no
        // color of its own falls back to the cycling palette.
        color:
            s.color ??
            (index === 0 ? color ?? SERIES_COLORS[0] : SERIES_COLORS[index % SERIES_COLORS.length]),
        label: s.label,
    }));

    // Same best-effort caveat as the tooltip: the shared axis is formatted using the
    // first series' field type, which may not hold for every mixed-semantics series.
    const xAxisFormatter = analytic.series[0]?.xField
        ? getAxisFormatter(analytic.series[0].xField.type)
        : undefined;

    return (
        <Paper
            ref={layout.ref}
            withBorder
            p={layout.padding}
            radius="md"
            {...cardShellProps(fillHeight)}
        >
            <Stack gap="xs" {...cardBodyProps(fillHeight)}>
                <AnalyticCardHeader
                    title={analytic.name}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={analytic.id}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    titleAdornment={
                        analytic.warnings.length > 0 && (
                            <Tooltip label={analytic.warnings.join(" ")} multiline maw={280}>
                                <Box style={{ cursor: "default", display: "flex" }}>
                                    <MdWarningAmber size={16} color="var(--mantine-color-yellow-6)" />
                                </Box>
                            </Tooltip>
                        )
                    }
                />
                <CompositeChart
                    ref={chartRef}
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    data={data}
                    dataKey="x"
                    h={chartHeight(fillHeight, isMobile)}
                    {...cardBodyProps(fillHeight)}
                    withXAxis={layout.withXAxis}
                    withYAxis={layout.withYAxis}
                    withDots={!layout.isCompact}
                    series={chartSeries}
                    xAxisProps={{ tickFormatter: xAxisFormatter }}
                    tooltipProps={{
                        content: createComposedTooltipContent(analytic, chartRef),
                    }}
                />
            </Stack>
        </Paper>
    );
}
