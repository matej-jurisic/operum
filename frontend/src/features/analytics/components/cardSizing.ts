import { CSSProperties, RefObject, useLayoutEffect, useRef, useState } from "react";

/**
 * A card renders at a fixed height inside the masonry on a tracker page, where an even
 * column is what matters, and stretches to fill its cell on a dashboard grid, where the
 * user has sized the widget themselves. `fillHeight` is what switches between the two.
 */

export const CHART_HEIGHT = 300;
export const MOBILE_CHART_HEIGHT = 210;

/** The card's header row, which the dashboard grid indents to clear its drag handle. */
export const CARD_HEADER_CLASS = "analytic-card-header";

// What a card has to be to draw each part of itself. Axis ticks are the first thing to
// go: below these they are illegible rather than informative, and the plot is better off
// with the space. A y-axis' labels cost width, an x-axis' cost height.
const MIN_Y_AXIS_WIDTH = 300;
const MIN_X_AXIS_HEIGHT = 200;

// Below either of these a card drops to its tighter padding and type, so that a widget
// dragged down to a few cells is not left as all chrome and no chart.
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

/**
 * Measures the card so its contents can size themselves against the cell the user
 * dragged out rather than against the viewport: the same analytic is a full chart at six
 * columns of the dashboard grid and has room for little more than its plot at two.
 *
 * Only a card that fills its cell adapts. In the masonry every card is drawn at the same
 * fixed height, and shrinking one because its column happens to be narrow would cost the
 * even grid that the masonry is there to give.
 */
export function useCardLayout(fillHeight?: boolean): CardLayout {
    // Measured synchronously (see useSyncedElementSize): isCompact and padding feed the
    // card's type and the box its value font is sized from, so a late first measurement
    // shows up as the whole card's text snapping size a frame or two into every load.
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
                  // The cell is the size the user chose, so anything that will not fit
                  // it is clipped rather than left to spill over the widget below.
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

/**
 * On a touchscreen, Recharts treats a scroll-past `touchmove` the same as a mouse hover
 * and pops the tooltip up mid-scroll — annoying on a dashboard where most charts are just
 * being scrolled past. Tapping a chart still fires a real `click` event, so switching the
 * trigger to "click" on mobile keeps that deliberate tap working while dropping the
 * incidental one from a scroll gesture. Desktop keeps the hover its mouse affords.
 */
export const chartTooltipTrigger = (isMobile?: boolean) =>
    isMobile ? "click" : "hover";

/**
 * Like `useElementSize`, but takes its first measurement synchronously in a layout
 * effect instead of waiting on a `ResizeObserver` callback. A `ResizeObserver`'s first
 * notification lands after the browser has already painted the mount frame, so anything
 * sized off it — a value's font, say — visibly snaps from its unmeasured fallback to the
 * real size a frame or two into every load. Measuring in `useLayoutEffect` instead lets
 * React re-render with the real size before that first frame is ever painted.
 *
 * Reports the border box (`getBoundingClientRect`), not the content box Mantine's
 * `useElementSize` gives: a card's thresholds are about the cell the user dragged out,
 * and the border box tracks that cell directly instead of shrinking and growing with the
 * card's own padding.
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
