import { CSSProperties, RefObject, useLayoutEffect, useRef, useState } from "react";

// `fillHeight` switches between a fixed masonry height and stretching to fill a dashboard grid cell.

export const CHART_HEIGHT = 300;
export const MOBILE_CHART_HEIGHT = 210;

/** The card's header row, which the dashboard grid indents to clear its drag handle. */
export const CARD_HEADER_CLASS = "analytic-card-header";

// Below these thresholds, axis ticks are dropped as illegible rather than shrunk.
const MIN_Y_AXIS_WIDTH = 300;
const MIN_X_AXIS_HEIGHT = 200;

// Below these thresholds, the card switches to tighter padding and type.
const COMPACT_WIDTH = 300;
const COMPACT_HEIGHT = 240;

/** What its own measured size lets a card draw. */
export interface CardLayout {
    /** Goes on the card's outer Paper: what the rest of this is measured from. */
    ref: RefObject<HTMLDivElement | null>;
    /** The measured box, for content that can only be sized in pixels. */
    width: number;
    height: number;
    /** Small enough that chrome has to give way to content. */
    isCompact: boolean;
    padding: "xs" | "md";
    withXAxis: boolean;
    withYAxis: boolean;
}

/** Only a `fillHeight` card adapts to its measured size; masonry cards stay a fixed height. */
export function useCardLayout(fillHeight?: boolean): CardLayout {
    const { ref, width, height } = useSyncedElementSize<HTMLDivElement>(!!fillHeight);

    const measured = !!fillHeight && width > 0 && height > 0;
    const isCompact =
        measured && (width < COMPACT_WIDTH || height < COMPACT_HEIGHT);

    return {
        ref,
        width,
        height,
        isCompact,
        padding: isCompact ? "xs" : "md",
        withXAxis: !measured || height >= MIN_X_AXIS_HEIGHT,
        withYAxis: !measured || width >= MIN_Y_AXIS_WIDTH,
    };
}

/** The card's outer Paper: a column that owns the full height of its cell. */
export const cardShellProps = (
    fillHeight?: boolean,
): { h?: string; style?: CSSProperties } =>
    fillHeight
        ? {
              h: "100%",
              style: {
                  display: "flex",
                  flexDirection: "column",
                  overflow: "hidden",
              },
          }
        : {};

/** Anything that should take the height left over after the card's header. */
export const cardBodyProps = (
    fillHeight?: boolean,
): { style?: CSSProperties } =>
    fillHeight ? { style: { flex: 1, minHeight: 0 } } : {};

export const chartHeight = (fillHeight?: boolean, isMobile?: boolean) =>
    fillHeight ? "100%" : isMobile ? MOBILE_CHART_HEIGHT : CHART_HEIGHT;

// On touch, Recharts fires the tooltip on scroll-past touchmove; "click" avoids that.
export const chartTooltipTrigger = (isMobile?: boolean) =>
    isMobile ? "click" : "hover";

/**
 * Like Mantine's `useElementSize`, but measures synchronously in `useLayoutEffect` (avoids
 * a visible snap on first paint) and reports the border box, not the content box.
 */
export function useSyncedElementSize<T extends HTMLElement = HTMLDivElement>(
    enabled = true,
): { ref: RefObject<T | null>; width: number; height: number } {
    const ref = useRef<T>(null);
    const [size, setSize] = useState({ width: 0, height: 0 });

    useLayoutEffect(() => {
        if (!enabled) return;
        const el = ref.current;
        if (!el) return;

        const measure = () => {
            const rect = el.getBoundingClientRect();
            setSize((prev) =>
                prev.width === rect.width && prev.height === rect.height
                    ? prev
                    : { width: rect.width, height: rect.height },
            );
        };

        measure();

        const observer = new ResizeObserver(measure);
        observer.observe(el);
        return () => observer.disconnect();
    }, [enabled]);

    return { ref, width: size.width, height: size.height };
}
