export const DynamicDateTokens = {
    Today: "today",
    StartOfWeek: "start_of_week",
    EndOfWeek: "end_of_week",
    StartOfMonth: "start_of_month",
    EndOfMonth: "end_of_month",
    StartOfYear: "start_of_year",
    EndOfYear: "end_of_year",
} as const;

export type DynamicDateToken = (typeof DynamicDateTokens)[keyof typeof DynamicDateTokens];

export const ParameterizedDateTokenPrefixes = {
    LastNHours: "last_n_hours",
    LastNDays: "last_n_days",
    LastNWeeks: "last_n_weeks",
    LastNMonths: "last_n_months",
} as const;

export type ParameterizedDateTokenPrefix =
    (typeof ParameterizedDateTokenPrefixes)[keyof typeof ParameterizedDateTokenPrefixes];

const fixedTokenLabels: Record<DynamicDateToken, string> = {
    [DynamicDateTokens.Today]: "Today",
    [DynamicDateTokens.StartOfWeek]: "Start of week",
    [DynamicDateTokens.EndOfWeek]: "End of week",
    [DynamicDateTokens.StartOfMonth]: "Start of month",
    [DynamicDateTokens.EndOfMonth]: "End of month",
    [DynamicDateTokens.StartOfYear]: "Start of year",
    [DynamicDateTokens.EndOfYear]: "End of year",
};

export const parameterizedTokenLabels: Record<ParameterizedDateTokenPrefix, string> = {
    [ParameterizedDateTokenPrefixes.LastNHours]: "Last N hours",
    [ParameterizedDateTokenPrefixes.LastNDays]: "Last N days",
    [ParameterizedDateTokenPrefixes.LastNWeeks]: "Last N weeks",
    [ParameterizedDateTokenPrefixes.LastNMonths]: "Last N months",
};

// backwards-compat alias
export const dynamicDateTokenLabels = fixedTokenLabels;

export const fixedDynamicDateTokenOptions = Object.entries(fixedTokenLabels).map(
    ([value, label]) => ({ value, label })
);

export const parameterizedDateTokenOptions = Object.entries(parameterizedTokenLabels).map(
    ([value, label]) => ({ value, label })
);

export const dynamicDateTokenOptions = [
    ...fixedDynamicDateTokenOptions,
    ...parameterizedDateTokenOptions,
];

export function isDynamicDateToken(value: unknown): value is string {
    if (typeof value !== "string") return false;
    if ((Object.values(DynamicDateTokens) as string[]).includes(value)) return true;
    return isParameterizedDateToken(value);
}

export function isParameterizedDateToken(value: string): boolean {
    const parsed = parseParameterizedToken(value);
    return parsed !== null;
}

export function parseParameterizedToken(
    token: string
): { prefix: ParameterizedDateTokenPrefix; n: number } | null {
    const colon = token.indexOf(":");
    if (colon < 0) return null;
    const prefix = token.slice(0, colon) as ParameterizedDateTokenPrefix;
    const n = parseInt(token.slice(colon + 1), 10);
    if (
        !(Object.values(ParameterizedDateTokenPrefixes) as string[]).includes(prefix) ||
        isNaN(n) ||
        n === 0
    )
        return null;
    return { prefix, n };
}

export function serializeParameterizedToken(
    prefix: ParameterizedDateTokenPrefix,
    n: number
): string {
    return `${prefix}:${n}`;
}

export function formatDynamicDateToken(token: string): string {
    if ((Object.values(DynamicDateTokens) as string[]).includes(token)) {
        return fixedTokenLabels[token as DynamicDateToken];
    }
    const parsed = parseParameterizedToken(token);
    if (parsed) {
        const label = parameterizedTokenLabels[parsed.prefix];
        return label.replace("N", String(parsed.n));
    }
    return token;
}
