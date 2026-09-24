import { CompositeChart } from "@mantine/charts";
import { Box, em, Tooltip } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useMemo } from "react";
import { MdWarningAmber } from "react-icons/md";
import { ComposedChartAnalyticDto } from "../types/AnalyticDto";
import {
    cardBodyProps,
    chartHeight,
    chartTooltipTrigger,
    useCardLayout,
} from "./cardSizing";
import {
    createComposedTooltipContent,
    getAxisFormatter,
} from "./ChartFormatters";
import { WidgetShell } from "./WidgetShell";

interface Props {
    analytic: ComposedChartAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
    flat?: boolean;
}

// Fallback for a series with no tracker color; index 0 is reserved for the board color.
const SERIES_COLORS = [
    "blue",
    "orange",
    "teal",
    "grape",
    "yellow",
    "red",
    "cyan",
    "pink",
];

export function ComposedChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    fillHeight,
    flat,
}: Props) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);

    // Union of every series' x labels; best-effort shared axis since series may use different x semantics (see `warnings`).
    const data = useMemo(() => {
        const xValues = new Set<string>();
        analytic.series.forEach((s) =>
            s.points.forEach((p) => xValues.add(p.x)),
        );
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
        color:
            s.color ??
            (index === 0
                ? (color ?? SERIES_COLORS[0])
                : SERIES_COLORS[index % SERIES_COLORS.length]),
        label: s.label,
    }));

    const xAxisFormatter = analytic.series[0]?.xField
        ? getAxisFormatter(analytic.series[0].xField.type)
        : undefined;

    // Only formatted when every series shares a value type; a mixed chart falls back to raw numbers.
    const yValueType = analytic.series[0]?.valueField.type;
    const yAxisFormatter =
        yValueType &&
        analytic.series.every((s) => s.valueField.type === yValueType)
            ? getAxisFormatter(yValueType)
            : undefined;

    return (
        <WidgetShell
            layout={layout}
            fillHeight={fillHeight}
            flat={flat}
            isConfiguring={isConfiguring}
            color={color}
            itemId={analytic.id}
            onRemove={onRemove}
            onEdit={onEdit}
            title={analytic.name}
            titleAdornment={
                analytic.warnings.length > 0 && (
                    <Tooltip
                        label={analytic.warnings.join(" ")}
                        multiline
                        maw={280}
                    >
                        <Box style={{ cursor: "default", display: "flex" }}>
                            <MdWarningAmber
                                size={16}
                                color="var(--mantine-color-yellow-6)"
                            />
                        </Box>
                    </Tooltip>
                )
            }
        >
            <CompositeChart
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
                yAxisProps={{
                    tickFormatter: yAxisFormatter,
                    domain: analytic.yAxisFromZero
                        ? [0, "auto"]
                        : ["auto", "auto"],
                }}
                tooltipProps={{
                    trigger: chartTooltipTrigger(isMobile),
                    content: createComposedTooltipContent(analytic),
                }}
            />
        </WidgetShell>
    );
}
