/**
 * Mirrors Operum.Model.Constants.DynamicDateTokens on the backend. The backend is the authority --
 * it resolves these at query time -- but the UI needs the same grammar to label a token and to
 * preview what it currently points at.
 *
 * Grammar: `token` or `token:n`. Anchors snap to a period boundary and take an optional signed
 * offset counted in that anchor's own period (`start_of_month:-1` is last month). Lookbacks
 * (`last_n_*`) require their argument and measure backwards from now.
 */

export const DateAnchors = {
    Today: "today",
    EndOfDay: "end_of_day",
    StartOfWeek: "start_of_week",
    EndOfWeek: "end_of_week",
    StartOfMonth: "start_of_month",
    EndOfMonth: "end_of_month",
    StartOfYear: "start_of_year",
    EndOfYear: "end_of_year",
} as const;

export type DateAnchor = (typeof DateAnchors)[keyof typeof DateAnchors];

export const LookbackPrefixes = {
    LastNHours: "last_n_hours",
    LastNDays: "last_n_days",
    LastNWeeks: "last_n_weeks",
    LastNMonths: "last_n_months",
} as const;

export type LookbackPrefix = (typeof LookbackPrefixes)[keyof typeof LookbackPrefixes];

type AnchorPeriod = "day" | "week" | "month" | "year";

/** The calendar unit an anchor's offset is counted in. */
const anchorPeriod: Record<DateAnchor, AnchorPeriod> = {
    [DateAnchors.Today]: "day",
    [DateAnchors.EndOfDay]: "day",
    [DateAnchors.StartOfWeek]: "week",
    [DateAnchors.EndOfWeek]: "week",
    [DateAnchors.StartOfMonth]: "month",
    [DateAnchors.EndOfMonth]: "month",
    [DateAnchors.StartOfYear]: "year",
    [DateAnchors.EndOfYear]: "year",
};

/** Label for the anchor alone; the offset is described separately. */
export const anchorLabels: Record<DateAnchor, string> = {
    [DateAnchors.Today]: "Start of day",
    [DateAnchors.EndOfDay]: "End of day",
    [DateAnchors.StartOfWeek]: "Start of week",
    [DateAnchors.EndOfWeek]: "End of week",
    [DateAnchors.StartOfMonth]: "Start of month",
    [DateAnchors.EndOfMonth]: "End of month",
    [DateAnchors.StartOfYear]: "Start of year",
    [DateAnchors.EndOfYear]: "End of year",
};

export const lookbackLabels: Record<LookbackPrefix, string> = {
    [LookbackPrefixes.LastNHours]: "Last N hours",
    [LookbackPrefixes.LastNDays]: "Last N days",
    [LookbackPrefixes.LastNWeeks]: "Last N weeks",
    [LookbackPrefixes.LastNMonths]: "Last N months",
};

export const anchorOptions = Object.entries(anchorLabels).map(([value, label]) => ({
    value,
    label,
}));

export const lookbackOptions = Object.entries(lookbackLabels).map(([value, label]) => ({
    value,
    label,
}));

export interface ParsedAnchorToken {
    anchor: DateAnchor;
    offset: number;
}

export interface ParsedLookbackToken {
    prefix: LookbackPrefix;
    n: number;
}

function splitToken(token: string): { head: string; arg: string | null } {
    const colon = token.indexOf(":");
    if (colon < 0) return { head: token, arg: null };
    return { head: token.slice(0, colon), arg: token.slice(colon + 1) };
}

function parseInteger(raw: string): number | null {
    // parseInt alone would accept "7abc"; a token argument has to be an integer and nothing else.
    if (!/^-?\d+$/.test(raw)) return null;
    return parseInt(raw, 10);
}

export function parseAnchorToken(token: string): ParsedAnchorToken | null {
    const { head, arg } = splitToken(token);
    if (!(Object.values(DateAnchors) as string[]).includes(head)) return null;

    if (arg === null) return { anchor: head as DateAnchor, offset: 0 };

    const offset = parseInteger(arg);
    if (offset === null) return null;
    return { anchor: head as DateAnchor, offset };
}

export function parseLookbackToken(token: string): ParsedLookbackToken | null {
    const { head, arg } = splitToken(token);
    if (arg === null) return null;
    if (!(Object.values(LookbackPrefixes) as string[]).includes(head)) return null;

    const n = parseInteger(arg);
    if (n === null || n === 0) return null;
    return { prefix: head as LookbackPrefix, n };
}

export function isAnchorToken(value: unknown): value is string {
    return typeof value === "string" && parseAnchorToken(value) !== null;
}

export function isLookbackToken(value: unknown): value is string {
    return typeof value === "string" && parseLookbackToken(value) !== null;
}

export function isDynamicDateToken(value: unknown): value is string {
    return isAnchorToken(value) || isLookbackToken(value);
}

export function serializeAnchorToken(anchor: DateAnchor, offset: number): string {
    return offset === 0 ? anchor : `${anchor}:${offset}`;
}

export function serializeLookbackToken(prefix: LookbackPrefix, n: number): string {
    return `${prefix}:${n}`;
}

/** "this month" / "last month" / "3 months ago" / "in 2 months", in the anchor's own period. */
export function describeOffset(anchor: DateAnchor, offset: number): string {
    const period = anchorPeriod[anchor];
    const plural = Math.abs(offset) === 1 ? period : `${period}s`;

    if (offset === 0) return `this ${period}`;
    if (offset === -1) return `last ${period}`;
    if (offset === 1) return `next ${period}`;
    if (offset < 0) return `${-offset} ${plural} ago`;
    return `in ${offset} ${plural}`;
}

function capitalize(text: string): string {
    return text.charAt(0).toUpperCase() + text.slice(1);
}

/** Day anchors read better as calendar words than as "start of day, last day". */
function describeDay(offset: number): string {
    if (offset === 0) return "today";
    if (offset === -1) return "yesterday";
    if (offset === 1) return "tomorrow";
    return offset < 0 ? `${-offset} days ago` : `in ${offset} days`;
}

export function formatDynamicDateToken(token: string): string {
    const anchorToken = parseAnchorToken(token);
    if (anchorToken) {
        const { anchor, offset } = anchorToken;

        if (anchorPeriod[anchor] === "day") {
            const day = describeDay(offset);
            return anchor === DateAnchors.EndOfDay ? `End of ${day}` : capitalize(day);
        }

        return `${anchorLabels[anchor]} (${describeOffset(anchor, offset)})`;
    }

    const lookback = parseLookbackToken(token);
    if (lookback) return lookbackLabels[lookback.prefix].replace("N", String(lookback.n));

    return token;
}

/**
 * Resolves a token the way the backend would, for preview only. Kept deliberately close to
 * DynamicDateTokens.Resolve so the two stay comparable; the browser's zone stands in for the
 * user's stored zone, which is the one the picker is showing them anyway.
 */
export function resolveDynamicDateToken(token: string, now: Date = new Date()): Date | null {
    const anchorToken = parseAnchorToken(token);
    if (anchorToken) return resolveAnchor(anchorToken, now);

    const lookback = parseLookbackToken(token);
    if (!lookback) return null;

    const { prefix, n } = lookback;
    if (prefix === LookbackPrefixes.LastNHours) {
        // Hours are a pure instant offset, so they need no calendar reasoning.
        return new Date(now.getTime() - n * 60 * 60 * 1000);
    }

    const startOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    switch (prefix) {
        case LookbackPrefixes.LastNDays:
            return addDays(startOfDay, -n);
        case LookbackPrefixes.LastNWeeks:
            return addDays(startOfDay, -n * 7);
        case LookbackPrefixes.LastNMonths:
            return new Date(
                startOfDay.getFullYear(),
                startOfDay.getMonth() - n,
                startOfDay.getDate(),
            );
    }
}

function resolveAnchor({ anchor, offset }: ParsedAnchorToken, now: Date): Date {
    const year = now.getFullYear();
    const month = now.getMonth();
    const startOfDay = new Date(year, month, now.getDate());
    // Weeks start on Monday, matching the backend.
    const startOfWeek = addDays(startOfDay, -((now.getDay() + 6) % 7));

    // End anchors are the start of the next period minus a millisecond, so they cover the whole
    // period instead of stopping short at 23:59:59.
    switch (anchor) {
        case DateAnchors.Today:
            return addDays(startOfDay, offset);
        case DateAnchors.EndOfDay:
            return lastInstantBefore(addDays(startOfDay, offset + 1));
        case DateAnchors.StartOfWeek:
            return addDays(startOfWeek, offset * 7);
        case DateAnchors.EndOfWeek:
            return lastInstantBefore(addDays(startOfWeek, (offset + 1) * 7));
        case DateAnchors.StartOfMonth:
            return new Date(year, month + offset, 1);
        case DateAnchors.EndOfMonth:
            return lastInstantBefore(new Date(year, month + offset + 1, 1));
        case DateAnchors.StartOfYear:
            return new Date(year + offset, 0, 1);
        case DateAnchors.EndOfYear:
            return lastInstantBefore(new Date(year + offset + 1, 0, 1));
    }
}

function addDays(date: Date, days: number): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

function lastInstantBefore(date: Date): Date {
    return new Date(date.getTime() - 1);
}
