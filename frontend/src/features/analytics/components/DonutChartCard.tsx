import { DonutChart } from "@mantine/charts";
import { ActionIcon, Box, em, Paper, Stack, Text, Tooltip } from "@mantine/core";
import { useDisclosure, useElementSize, useMediaQuery } from "@mantine/hooks";
import { useMemo } from "react";
import { MdInfoOutline } from "react-icons/md";
import { DonutChartAnaylticDto } from "../types/AnalyticDto";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    chartHeight,
    chartTooltipTrigger,
    useCardLayout,
} from "./cardSizing";
import { createDonutTooltipContent } from "./ChartFormatters";
import { DonutValuesModal } from "./DonutValuesModal";

interface Props {
    analytic: DonutChartAnaylticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

// The ring is the one part of a card that has to be given a size in pixels, so a widget
// that fills its cell works its own out from the box the ring is drawn in.
//
// Mantine centres the ring and leaves the rest of the box to the labels, which reach out
// past it on every side: below a box that can seat a readable ring and its labels both,
// the labels are dropped and the ring takes the whole box instead.
const LABEL_GUTTER = 40;
const MIN_LABELLED_BOX = 200;
const MIN_DONUT_SIZE = 60;
const MAX_DONUT_SIZE = 400;

// Below this share, a segment's label and leader line are dropped: the text is too
// cramped to read and the lines cross each other. The segment is still drawn, and
// hovering it shows the full value in the tooltip.
const MIN_LABEL_SHARE = 0.03;

const clamp = (value: number, min: number, max: number) =>
    Math.max(min, Math.min(max, value));

interface DonutLabelProps {
    x: number;
    y: number;
    cx: number;
    percent?: number;
    points?: { x: number; y: number }[];
}

// Mirrors Mantine's own percent label, but returns nothing under MIN_LABEL_SHARE.
const renderDonutLabel = ({ x, y, cx, percent }: DonutLabelProps) => {
    if ((percent ?? 0) < MIN_LABEL_SHARE) return null;
    return (
        <text
            x={x}
            y={y}
            textAnchor={x > cx ? "start" : "end"}
            fill="var(--chart-labels-color, var(--mantine-color-dimmed))"
            fontFamily="var(--mantine-font-family)"
            fontSize={12}
        >
            <tspan x={x}>{`${Math.round((percent ?? 0) * 100)}%`}</tspan>
        </text>
    );
};

const renderDonutLabelLine = ({ points, percent }: DonutLabelProps) => {
    if ((percent ?? 0) < MIN_LABEL_SHARE || !points) return null;
    return (
        <polyline
            points={points.map((p) => `${p.x},${p.y}`).join(" ")}
            stroke="var(--chart-label-color, var(--mantine-color-dimmed))"
            strokeWidth={1}
            fill="none"
        />
    );
};

export function DonutChartCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    fillHeight,
}: Props) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const layout = useCardLayout(fillHeight);
    const plot = useElementSize<HTMLDivElement>();
    const [valuesOpened, valuesModal] = useDisclosure(false);

    const { positivePoints, excludedPoints, isAbsolute } = useMemo(() => {
        const positive = analytic.points.filter((x) => (x.value ?? 0) > 0);
        const negative = analytic.points.filter((x) => (x.value ?? 0) < 0);

        // Firefly-style data stores outflows as negative numbers, which would leave a
        // "sum per category" ring with nothing to draw. When every category is
        // non-positive and at least one is negative, show magnitudes instead. Mixed
        // signs keep the plain split: a share-of-whole reading means nothing when some
        // categories are inflows and others outflows.
        if (positive.length === 0 && negative.length > 0) {
            return {
                positivePoints: negative.map((x) => ({
                    ...x,
                    value: Math.abs(x.value ?? 0),
                })),
                excludedPoints: analytic.points.filter((x) => (x.value ?? 0) === 0),
                isAbsolute: true,
            };
        }

        const excluded = analytic.points.filter((x) => (x.value ?? 0) <= 0);
        return {
            positivePoints: positive,
            excludedPoints: excluded,
            isAbsolute: false,
        };
    }, [analytic.points]);

    const coloredPoints = useMemo(() => {
        const baseColor = color ?? "blue";

        return positivePoints.map((x, index) => {
            const opacity =
                0.2 + (index / Math.max(positivePoints.length, 1)) * 0.8;

            return {
                name: x.name,
                value: x.value,
                color: `color-mix(in srgb, var(--mantine-color-${baseColor}-6) ${
                    opacity * 100
                }%, white)`,
            };
        });
    }, [positivePoints, color]);

    // In the masonry the box is the same on every card, so the ring is too.
    const donut = useMemo(() => {
        if (!fillHeight) {
            const size = isMobile ? 150 : 200;
            return { size, thickness: 20, withLabels: true };
        }

        const box = Math.min(plot.width, plot.height);
        const withLabels = box >= MIN_LABELLED_BOX;
        const size = clamp(
            box - (withLabels ? LABEL_GUTTER * 2 : 4),
            MIN_DONUT_SIZE,
            MAX_DONUT_SIZE,
        );

        return {
            size,
            thickness: clamp(Math.round(size * 0.1), 6, 28),
            withLabels,
        };
    }, [fillHeight, isMobile, plot.width, plot.height]);

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
                    actions={
                        <Tooltip label="Show values">
                            <ActionIcon
                                size="md"
                                color={color}
                                variant="subtle"
                                style={{ pointerEvents: "auto" }}
                                onClick={valuesModal.open}
                                aria-label="Show values"
                            >
                                <MdInfoOutline size={18} />
                            </ActionIcon>
                        </Tooltip>
                    }
                />
                <Box
                    h={chartHeight(fillHeight, isMobile)}
                    style={{
                        display: "flex",
                        flexDirection: "column",
                        gap: "var(--mantine-spacing-xs)",
                        ...(fillHeight ? { flex: 1, minHeight: 0 } : {}),
                    }}
                >
                    {/* The note is what a small widget can least afford, and the ring is
                        what it is there for: on one the count alone has to carry it. */}
                    {excludedPoints.length > 0 && (
                        <Tooltip
                            label={excludedPoints
                                .map((p) => p.name ?? "Unknown")
                                .join(", ")}
                            multiline
                            maw={260}
                        >
                            <Text
                                size="xs"
                                c="dimmed"
                                lineClamp={1}
                                style={{ cursor: "default" }}
                            >
                                {layout.isCompact
                                    ? `${excludedPoints.length} not shown`
                                    : `${excludedPoints.length} categor${
                                          excludedPoints.length === 1
                                              ? "y"
                                              : "ies"
                                      } not shown (${
                                          isAbsolute
                                              ? "zero value"
                                              : "zero or negative value"
                                      })`}
                            </Text>
                        </Tooltip>
                    )}
                    <Box
                        ref={plot.ref}
                        style={{
                            flex: 1,
                            minHeight: 0,
                            display: "flex",
                            justifyContent: "center",
                        }}
                    >
                        <DonutChart
                            withLabelsLine={donut.withLabels}
                            w={"100%"}
                            withLabels={donut.withLabels}
                            size={donut.size}
                            thickness={donut.thickness}
                            pieProps={
                                donut.withLabels
                                    ? {
                                          label: renderDonutLabel,
                                          labelLine: renderDonutLabelLine,
                                      }
                                    : undefined
                            }
                            paddingAngle={2}
                            tooltipDataSource="segment"
                            tooltipProps={{
                                trigger: chartTooltipTrigger(isMobile),
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
            <DonutValuesModal
                analytic={analytic}
                isAbsolute={isAbsolute}
                opened={valuesOpened}
                onClose={valuesModal.close}
            />
        </Paper>
    );
}
