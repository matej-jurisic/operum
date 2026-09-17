import { Text } from "@mantine/core";
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

/** Renders nothing when `trend` is undefined (no connected date range for TrendCalculator to bucket). */
export function TrendSparkline({
    trend,
    currentValue,
    valueFieldType,
    color,
    direction,
    previousPeriodLabel = "previous period",
}: Props) {
    if (!trend) return null;

    const points = trend.points;
    const current = parseMagnitude(valueFieldType, currentValue);
    const previous = parseMagnitude(valueFieldType, trend.previousValue);

    const delta =
        current !== undefined && previous !== undefined && previous !== 0
            ? ((current - previous) / Math.abs(previous)) * 100
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
                      PAD + (i / (points.length - 1)) * (WIDTH - PAD * 2);
                  const scaleY = (y: number) =>
                      maxY === minY
                          ? HEIGHT / 2
                          : HEIGHT -
                            PAD -
                            ((y - minY) / (maxY - minY)) * (HEIGHT - PAD * 2);
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
                gap: 2,
                width: "100%",
                alignItems: "center",
                justifyContent: "space-between",
            }}
        >
            {spark && (
                <svg
                    viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
                    width="80%"
                    height={HEIGHT}
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
            )}
            {delta !== undefined && (
                <Text
                    size="xs"
                    c={
                        deltaTone === "neutral"
                            ? "dimmed"
                            : deltaTone === "good"
                              ? "teal"
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
