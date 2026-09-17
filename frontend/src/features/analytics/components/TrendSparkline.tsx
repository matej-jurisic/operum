import { Text, useComputedColorScheme } from "@mantine/core";
import { TrendDto } from "../types/AnalyticDto";

interface Props {
    trend: TrendDto | undefined;
    /** Compared against trend.previousValue to draw the delta line. */
    currentValue: string | undefined;
    valueFieldType: string | undefined;
    color: string | undefined;
    /** "neutral" keeps the delta text in a fixed color regardless of sign. */
    direction: "higherIsBetter" | "lowerIsBetter" | "neutral";
    previousPeriodLabel?: string;
    /** Shrinks the chart so the widget it sits in can be shorter, e.g. on mobile. */
    compact?: boolean;
}

// Mirrors TrendCalculator's magnitude parsing server-side (timespan as seconds, else a plain number).
function parseMagnitude(
    type: string | undefined,
    raw: string | undefined,
): number | undefined {
    if (!raw) return undefined;
    if (type === "timespan") {
        const parts = raw.split(":");
        if (parts.length < 3) return undefined;
        const [h, m, s] = parts;
        const seconds = Number(h) * 3600 + Number(m) * 60 + Number(s);
        return Number.isFinite(seconds) ? seconds : undefined;
    }
    const n = Number(raw);
    return Number.isFinite(n) ? n : undefined;
}

const WIDTH = 100;
const HEIGHT = 60;
const PAD = 3;

const COMPACT_WIDTH = 80;
const COMPACT_HEIGHT = 32;
const COMPACT_PAD = 2;

/** Renders nothing when `trend` is undefined (no connected date range for TrendCalculator to bucket). */
export function TrendSparkline({
    trend,
    currentValue,
    valueFieldType,
    color,
    direction,
    previousPeriodLabel = "previous period",
    compact,
}: Props) {
    const isDark = useComputedColorScheme("light") === "dark";

    if (!trend) return null;

    const width = compact ? COMPACT_WIDTH : WIDTH;
    const height = compact ? COMPACT_HEIGHT : HEIGHT;
    const pad = compact ? COMPACT_PAD : PAD;

    const points = trend.points;
    const current = parseMagnitude(valueFieldType, currentValue);
    const previous = parseMagnitude(valueFieldType, trend.previousValue);

    // Abs(previous) only when sign flips, else it inverts same-sign deltas (e.g. negative expenses).
    const delta =
        current !== undefined && previous !== undefined && previous !== 0
            ? ((current - previous) /
                  ((previous < 0) === (current < 0) ? previous : Math.abs(previous))) *
              100
            : undefined;

    const deltaTone: "neutral" | "good" | "bad" =
        delta === undefined || direction === "neutral"
            ? "neutral"
            : delta >= 0 === (direction === "higherIsBetter")
              ? "good"
              : "bad";

    const spark =
        points.length >= 2
            ? (() => {
                  const ys = points.map((p) => p.y);
                  const minY = Math.min(...ys);
                  const maxY = Math.max(...ys);
                  const scaleX = (i: number) =>
                      pad + (i / (points.length - 1)) * (width - pad * 2);
                  const scaleY = (y: number) =>
                      maxY === minY
                          ? height / 2
                          : height -
                            pad -
                            ((y - minY) / (maxY - minY)) * (height - pad * 2);
                  const coords = points.map(
                      (p, i) => [scaleX(i), scaleY(p.y)] as const,
                  );
                  const linePath = coords
                      .map(([x, y], i) => `${i === 0 ? "M" : "L"}${x},${y}`)
                      .join(" ");
                  const last = coords[coords.length - 1];
                  return { linePath, last };
              })()
            : null;

    return (
        <div
            style={{
                display: "flex",
                flexDirection: "column",
                gap: compact ? 0 : 2,
                width: "100%",
                alignItems: "center",
                justifyContent: "space-between",
            }}
        >
            {spark && (
                <div
                    style={{
                        width: "80%",
                        maxWidth: width,
                        aspectRatio: `${width} / ${height}`,
                    }}
                >
                    <svg
                        viewBox={`0 0 ${width} ${height}`}
                        width="100%"
                        height="100%"
                        preserveAspectRatio="none"
                        style={{ display: "block" }}
                    >
                        <path
                            d={spark.linePath}
                            fill="none"
                            stroke="var(--mantine-color-dimmed)"
                            strokeWidth={1.5}
                            strokeLinecap="round"
                            strokeLinejoin="round"
                        />
                        <circle
                            cx={spark.last[0]}
                            cy={spark.last[1]}
                            r={2.5}
                            fill={`var(--mantine-color-${color ?? "blue"}-6)`}
                        />
                    </svg>
                </div>
            )}
            {delta !== undefined && (
                <Text
                    size="xs"
                    c={
                        deltaTone === "neutral"
                            ? "dimmed"
                            : deltaTone === "good"
                              ? isDark
                                  ? "green.4"
                                  : "green.8"
                              : "red"
                    }
                >
                    {delta >= 0 ? "+" : ""}
                    {Math.round(delta)}% vs {previousPeriodLabel}
                </Text>
            )}
        </div>
    );
}
