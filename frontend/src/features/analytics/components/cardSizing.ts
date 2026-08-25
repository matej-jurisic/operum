import { useElementSize } from "@mantine/hooks";
import { CSSProperties, RefObject } from "react";

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

// A name gets at most this many lines before it is cut, so that a wrapping one cannot
// push the card's actual content off the bottom of the widget.
const DEFAULT_MAX_TITLE_LINES = 2;

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
    titleSize: "xs" | "sm";
    /** How many lines of a long analytic name the card can afford. */
    titleLineClamp: number | undefined;
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
export function useCardLayout(
    fillHeight?: boolean,
    /** For a card whose content sizes itself to the room the name leaves over. */
    maxTitleLines: number = DEFAULT_MAX_TITLE_LINES,
): CardLayout {
    const { ref, width, height } = useElementSize<HTMLDivElement>();

    const measured = !!fillHeight && width > 0 && height > 0;
    const isCompact =
        measured && (width < COMPACT_WIDTH || height < COMPACT_HEIGHT);

    return {
        ref,
        width,
        height,
        isCompact,
        padding: isCompact ? "xs" : "md",
        titleSize: isCompact ? "xs" : "sm",
        // A name long enough to wrap costs a fixed-height card nothing and a small
        // widget most of its plot.
        titleLineClamp: measured ? (isCompact ? 1 : maxTitleLines) : undefined,
        withXAxis: !measured || height >= MIN_X_AXIS_HEIGHT,
        withYAxis: !measured || width >= MIN_Y_AXIS_WIDTH,
    };
}

/**
 * What the card leads with: its name, qualified by the fields it was built from wherever
 * there is room for both.
 *
 * The qualifier is the first half to go. A card too small to hold the whole title cuts it
 * at whatever character it runs out of room at, and `Steps: Da...` names the analytic no
 * better than `Steps` does while telling the reader less than the name alone.
 */
export const cardTitle = (
    layout: CardLayout,
    name: string,
    subtitle?: string,
): string => (subtitle && !layout.isCompact ? `${name}: ${subtitle}` : name);

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
