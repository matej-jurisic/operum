/** Traces the outline of the padded union of a container's child widgets, so the
    container can be drawn as one shape that hugs them instead of a plain rectangle.

    The rectangles come from an integer grid, so coordinate compression keeps the
    occupancy mask small: at most 2*N distinct x's and y's for N widgets. Boundary
    edges are exactly the unit edges shared by an occupied and an unoccupied cell
    (a standard result for rectilinear unions), which sidesteps writing a full
    marching-squares case table. */

export interface ShapeRect {
    x: number;
    y: number;
    w: number;
    h: number;
}

interface Point {
    x: number;
    y: number;
}

function traceUnionOutline(rects: ShapeRect[]): Point[][] {
    const xsSet = new Set<number>();
    const ysSet = new Set<number>();
    for (const r of rects) {
        xsSet.add(r.x);
        xsSet.add(r.x + r.w);
        ysSet.add(r.y);
        ysSet.add(r.y + r.h);
    }
    const xs = [...xsSet].sort((a, b) => a - b);
    const ys = [...ysSet].sort((a, b) => a - b);
    const nx = xs.length - 1;
    const ny = ys.length - 1;
    if (nx <= 0 || ny <= 0) return [];

    const occupied: boolean[][] = Array.from({ length: nx }, () => new Array(ny).fill(false));
    for (let ix = 0; ix < nx; ix++) {
        const cx = (xs[ix] + xs[ix + 1]) / 2;
        for (let iy = 0; iy < ny; iy++) {
            const cy = (ys[iy] + ys[iy + 1]) / 2;
            occupied[ix][iy] = rects.some(
                (r) => cx > r.x && cx < r.x + r.w && cy > r.y && cy < r.y + r.h,
            );
        }
    }

    // Directed unit edges walked clockwise around each occupied cell; an edge shared
    // by two occupied cells is walked in both directions and cancels out, leaving
    // only the boundary.
    const edges = new Map<string, [number, number]>();
    const addEdge = (from: [number, number], to: [number, number]) =>
        edges.set(`${from[0]},${from[1]}`, to);

    for (let ix = 0; ix < nx; ix++) {
        for (let iy = 0; iy < ny; iy++) {
            if (!occupied[ix][iy]) continue;
            const neighborTop = iy > 0 && occupied[ix][iy - 1];
            const neighborRight = ix < nx - 1 && occupied[ix + 1][iy];
            const neighborBottom = iy < ny - 1 && occupied[ix][iy + 1];
            const neighborLeft = ix > 0 && occupied[ix - 1][iy];
            if (!neighborTop) addEdge([ix, iy], [ix + 1, iy]);
            if (!neighborRight) addEdge([ix + 1, iy], [ix + 1, iy + 1]);
            if (!neighborBottom) addEdge([ix + 1, iy + 1], [ix, iy + 1]);
            if (!neighborLeft) addEdge([ix, iy + 1], [ix, iy]);
        }
    }

    const loops: Point[][] = [];
    const visited = new Set<string>();
    for (const startKey of edges.keys()) {
        if (visited.has(startKey)) continue;
        const loopVerts: [number, number][] = [];
        let key = startKey;
        for (let steps = 0; steps < edges.size + 1; steps++) {
            if (visited.has(key)) break;
            visited.add(key);
            const [ix, iy] = key.split(",").map(Number) as [number, number];
            loopVerts.push([ix, iy]);
            const to = edges.get(key);
            if (!to) break;
            key = `${to[0]},${to[1]}`;
            if (key === startKey) break;
        }
        if (loopVerts.length >= 4) {
            loops.push(simplifyCollinear(loopVerts.map(([ix, iy]) => ({ x: xs[ix], y: ys[iy] }))));
        }
    }
    return loops;
}

/** Drops points that sit on a straight run between their neighbors, so corner
    rounding only ever sees real turns. */
function simplifyCollinear(points: Point[]): Point[] {
    const n = points.length;
    const result: Point[] = [];
    for (let i = 0; i < n; i++) {
        const prev = points[(i - 1 + n) % n];
        const curr = points[i];
        const next = points[(i + 1) % n];
        const horizontal = prev.y === curr.y && curr.y === next.y;
        const vertical = prev.x === curr.x && curr.x === next.x;
        if (horizontal || vertical) continue;
        result.push(curr);
    }
    return result.length >= 4 ? result : points;
}

const segLen = (a: Point, b: Point) => Math.hypot(b.x - a.x, b.y - a.y);
const dir = (a: Point, b: Point): Point => {
    const l = segLen(a, b) || 1;
    return { x: (b.x - a.x) / l, y: (b.y - a.y) / l };
};

/** Every turn in the traced outline is a right angle, so a corner is rounded by
    cutting back `r` along each adjacent edge and joining the two points with a
    quarter-circle. The same construction rounds concave (reflex) corners too: the
    arc's center falls out of the perpendicularity of the two edges regardless of
    which way the corner turns, so it naturally bulges into whichever side is
    correct without special-casing convex vs. concave. */
function roundedLoopPath(points: Point[], radius: number): string {
    const n = points.length;
    if (n < 4) return "";
    if (radius <= 0) {
        return `M ${points.map((p) => `${p.x} ${p.y}`).join(" L ")} Z`;
    }

    const corners = points.map((curr, i) => {
        const prev = points[(i - 1 + n) % n];
        const next = points[(i + 1) % n];
        const r = Math.min(radius, segLen(prev, curr) / 2, segLen(curr, next) / 2);
        const d01 = dir(prev, curr);
        const d12 = dir(curr, next);
        const a: Point = { x: curr.x - d01.x * r, y: curr.y - d01.y * r };
        const b: Point = { x: curr.x + d12.x * r, y: curr.y + d12.y * r };
        const c: Point = { x: a.x + d12.x * r, y: a.y + d12.y * r };
        return { a, b, c, r };
    });

    const arcPoints = (c: Point, from: Point, to: Point, r: number, steps = 6): Point[] => {
        if (r <= 0.01) return [to];
        const a0 = Math.atan2(from.y - c.y, from.x - c.x);
        const a1 = Math.atan2(to.y - c.y, to.x - c.x);
        let delta = a1 - a0;
        while (delta > Math.PI) delta -= Math.PI * 2;
        while (delta < -Math.PI) delta += Math.PI * 2;
        const pts: Point[] = [];
        for (let s = 1; s <= steps; s++) {
            const t = s / steps;
            const ang = a0 + delta * t;
            pts.push({ x: c.x + r * Math.cos(ang), y: c.y + r * Math.sin(ang) });
        }
        return pts;
    };

    const start = corners[0].a;
    let d = `M ${start.x} ${start.y}`;
    for (let i = 0; i < n; i++) {
        const { a, b, c, r } = corners[i];
        for (const p of arcPoints(c, a, b, r)) d += ` L ${p.x} ${p.y}`;
        const nextA = corners[(i + 1) % n].a;
        d += ` L ${nextA.x} ${nextA.y}`;
    }
    return `${d} Z`;
}

/** Two rects a normal grid gap apart don't touch or overlap, so tracing their union
    outright leaves them as separate loops. For every pair up to `maxGap` apart (edge to
    edge, with their perpendicular extents overlapping), this fills the gap with a
    connecting rect spanning exactly that channel -- widgets a standard grid gap apart
    merge into one outline this way, with no added padding anywhere else, so the traced
    border ends up flush against every widget's own edge instead of floating past it. */
function bridgeRects(rects: ShapeRect[], maxGap: number): ShapeRect[] {
    const bridges: ShapeRect[] = [];
    for (let i = 0; i < rects.length; i++) {
        for (let j = i + 1; j < rects.length; j++) {
            const a = rects[i];
            const b = rects[j];

            const horizGap = a.x < b.x ? b.x - (a.x + a.w) : a.x - (b.x + b.w);
            const left = a.x < b.x ? a : b;
            const vOverlapStart = Math.max(a.y, b.y);
            const vOverlapEnd = Math.min(a.y + a.h, b.y + b.h);
            if (horizGap > 0 && horizGap <= maxGap && vOverlapEnd > vOverlapStart) {
                bridges.push({
                    x: left.x + left.w,
                    y: vOverlapStart,
                    w: horizGap,
                    h: vOverlapEnd - vOverlapStart,
                });
            }

            const vertGap = a.y < b.y ? b.y - (a.y + a.h) : a.y - (b.y + b.h);
            const top = a.y < b.y ? a : b;
            const hOverlapStart = Math.max(a.x, b.x);
            const hOverlapEnd = Math.min(a.x + a.w, b.x + b.w);
            if (vertGap > 0 && vertGap <= maxGap && hOverlapEnd > hOverlapStart) {
                bridges.push({
                    x: hOverlapStart,
                    y: top.y + top.h,
                    w: hOverlapEnd - hOverlapStart,
                    h: vertGap,
                });
            }
        }
    }

    // Where four widgets meet, the gap-by-gap intersection square isn't covered by any
    // bridge and would trace as a tiny hole. Fill it once every side of it is occupied.
    const covered = (px: number, py: number) =>
        [...rects, ...bridges].some(
            (r) => px > r.x && px < r.x + r.w && py > r.y && py < r.y + r.h,
        );
    const probe = 3;
    const corners: ShapeRect[] = [];
    for (let i = 0; i < rects.length; i++) {
        for (let j = i + 1; j < rects.length; j++) {
            const a = rects[i];
            const b = rects[j];
            const left = a.x < b.x ? a : b;
            const right = left === a ? b : a;
            const top = a.y < b.y ? a : b;
            const bottom = top === a ? b : a;
            const w = right.x - (left.x + left.w);
            const h = bottom.y - (top.y + top.h);
            if (w <= 0 || w > maxGap || h <= 0 || h > maxGap) continue;
            const x = left.x + left.w;
            const y = top.y + top.h;
            const cx = x + w / 2;
            const cy = y + h / 2;
            if (
                covered(x - probe, cy) &&
                covered(x + w + probe, cy) &&
                covered(cx, y - probe) &&
                covered(cx, y + h + probe)
            ) {
                corners.push({ x, y, w, h });
            }
        }
    }
    return [...bridges, ...corners];
}

/** SVG path data for the shape a container draws around `rects`: widgets up to `maxGap`
    apart are bridged into one outline (see bridgeRects), everyone else stays a separate
    loop, and the result is corner-rounded to `radius`. Null when there's nothing to draw. */
export function huggingShapePath(
    rects: ShapeRect[],
    maxGap: number,
    radius: number,
): string | null {
    if (rects.length === 0) return null;
    const bridges = bridgeRects(rects, maxGap);
    const loops = traceUnionOutline([...rects, ...bridges]);
    if (loops.length === 0) return null;
    const path = loops.map((loop) => roundedLoopPath(loop, radius)).filter(Boolean).join(" ");
    return path || null;
}
