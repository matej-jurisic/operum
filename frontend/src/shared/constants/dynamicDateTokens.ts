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

export const dynamicDateTokenLabels: Record<DynamicDateToken, string> = {
    [DynamicDateTokens.Today]: "Today",
    [DynamicDateTokens.StartOfWeek]: "Start of week",
    [DynamicDateTokens.EndOfWeek]: "End of week",
    [DynamicDateTokens.StartOfMonth]: "Start of month",
    [DynamicDateTokens.EndOfMonth]: "End of month",
    [DynamicDateTokens.StartOfYear]: "Start of year",
    [DynamicDateTokens.EndOfYear]: "End of year",
};

export const dynamicDateTokenOptions = Object.entries(dynamicDateTokenLabels).map(
    ([value, label]) => ({ value, label })
);

export function isDynamicDateToken(value: unknown): value is DynamicDateToken {
    return typeof value === "string" && Object.values(DynamicDateTokens).includes(value as DynamicDateToken);
}
