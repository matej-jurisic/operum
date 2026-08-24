import { CSSProperties } from "react";

/**
 * A card renders at a fixed height inside the masonry on a tracker page, where an even
 * column is what matters, and stretches to fill its cell on a dashboard grid, where the
 * user has sized the widget themselves. `fillHeight` is what switches between the two.
 */

export const CHART_HEIGHT = 300;
export const MOBILE_CHART_HEIGHT = 210;

/** The card's outer Paper: a column that owns the full height of its cell. */
export const cardShellProps = (fillHeight?: boolean) =>
    fillHeight
        ? {
              h: "100%",
              style: {
                  display: "flex",
                  flexDirection: "column",
              } as CSSProperties,
          }
        : {};

/** Anything that should take the height left over after the card's header. */
export const cardBodyProps = (fillHeight?: boolean) =>
    fillHeight ? { style: { flex: 1, minHeight: 0 } as CSSProperties } : {};

export const chartHeight = (fillHeight?: boolean, isMobile?: boolean) =>
    fillHeight ? "100%" : isMobile ? MOBILE_CHART_HEIGHT : CHART_HEIGHT;
