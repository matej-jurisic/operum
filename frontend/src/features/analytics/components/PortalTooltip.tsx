import { CSSProperties, ReactNode, RefObject, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";

interface Coordinate {
    x: number;
    y: number;
}

const GAP = 12;
const VIEWPORT_MARGIN = 8;

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

        // Anchor to whichever side of the point still has room, so the box grows away
        // from the nearest window edge instead of off it.
        let left = pointX + GAP;
        if (left + width + VIEWPORT_MARGIN > window.innerWidth) {
            left = pointX - GAP - width;
        }
        left = Math.min(
            Math.max(left, VIEWPORT_MARGIN),
            Math.max(VIEWPORT_MARGIN, window.innerWidth - width - VIEWPORT_MARGIN),
        );

        let top = pointY - height - GAP;
        if (top < VIEWPORT_MARGIN) {
            top = pointY + GAP;
        }
        top = Math.min(
            Math.max(top, VIEWPORT_MARGIN),
            Math.max(VIEWPORT_MARGIN, window.innerHeight - height - VIEWPORT_MARGIN),
        );

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
