import { CSSProperties, ReactNode, RefObject, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";

interface Coordinate {
    x: number;
    y: number;
}

const GAP = 12;
const VIEWPORT_MARGIN = 8;

/**
 * One axis of the tooltip's placement: decides whether the box sits before or after the
 * point, then clamps it to fit the window.
 *
 * Whichever side the point has *more* room on wins the tie-break when neither side has a
 * full `size` of space -- that keeps the box anchored near the point it's describing. The
 * alternative (always trying one side first, falling back to the other, then clamping
 * whatever comes out to the window bounds) is what let a wide box -- a long category name,
 * say -- get flipped to a heavily negative position and then clamped all the way across to
 * the opposite edge of the screen instead of just sliding over enough to fit.
 */
function pickPosition({
    point,
    size,
    gap,
    margin,
    windowSize,
    preferBefore = false,
}: {
    point: number;
    size: number;
    gap: number;
    margin: number;
    windowSize: number;
    preferBefore?: boolean;
}): number {
    const spaceBefore = point - gap - margin;
    const spaceAfter = windowSize - point - gap - margin;

    const useBefore = preferBefore
        ? spaceBefore >= size || spaceAfter < size
        : spaceAfter < size && spaceBefore >= spaceAfter;

    const raw = useBefore ? point - gap - size : point + gap;
    return Math.min(
        Math.max(raw, margin),
        Math.max(margin, windowSize - size - margin),
    );
}

/**
 * A chart tooltip normally lives inside the chart's own DOM, absolutely positioned and
 * clamped to stay within the chart's box. Dashboard widgets clip overflow (DashboardGrid.css)
 * so a card smaller than the tooltip needs chops off whatever spills past the widget's edge
 * -- clamping to the chart's own box doesn't help when that box is the small thing to begin
 * with.
 *
 * This portals the tooltip content straight to <body>, escaping every clipped ancestor, and
 * works out its own fixed position from the chart-local `coordinate` Recharts hands the
 * tooltip content plus the chart container's own screen position -- then nudges it back on
 * screen if it would otherwise run past the window edge.
 */
export function PortalTooltip({
    coordinate,
    containerRef,
    children,
}: {
    coordinate: Coordinate | undefined;
    containerRef: RefObject<HTMLElement | null>;
    children: ReactNode;
}) {
    const nodeRef = useRef<HTMLDivElement>(null);
    const [style, setStyle] = useState<CSSProperties>({ visibility: "hidden" });

    useLayoutEffect(() => {
        const container = containerRef.current;
        const node = nodeRef.current;
        if (!container || !node || !coordinate) {
            setStyle({ visibility: "hidden" });
            return;
        }

        const containerRect = container.getBoundingClientRect();
        const { width, height } = node.getBoundingClientRect();
        const pointX = containerRect.left + coordinate.x;
        const pointY = containerRect.top + coordinate.y;

        const left = pickPosition({
            point: pointX,
            size: width,
            gap: GAP,
            margin: VIEWPORT_MARGIN,
            windowSize: window.innerWidth,
        });
        const top = pickPosition({
            point: pointY,
            size: height,
            gap: GAP,
            margin: VIEWPORT_MARGIN,
            windowSize: window.innerHeight,
            // Vertically the tooltip prefers sitting above the point (like a speech
            // bubble), only dropping below it when there's more room that way.
            preferBefore: true,
        });

        setStyle({
            position: "fixed",
            left,
            top,
            visibility: "visible",
            pointerEvents: "none",
            zIndex: 1000,
        });
    }, [coordinate?.x, coordinate?.y, containerRef]);

    if (typeof document === "undefined") return null;

    return createPortal(
        <div ref={nodeRef} style={style}>
            {children}
        </div>,
        document.body,
    );
}
