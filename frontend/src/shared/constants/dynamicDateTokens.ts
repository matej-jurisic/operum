export const DynamicDateTokens = {
    Today: "today",
    StartOfMonth: "start_of_month",
    EndOfMonth: "end_of_month",
} as const;

export type DynamicDateToken = (typeof DynamicDateTokens)[keyof typeof DynamicDateTokens];

export const dynamicDateTokenLabels: Record<DynamicDateToken, string> = {
    [DynamicDateTokens.Today]: "Today",
    [DynamicDateTokens.StartOfMonth]: "Start of month",
    [DynamicDateTokens.EndOfMonth]: "End of month",
};

export const dynamicDateTokenOptions = Object.entries(dynamicDateTokenLabels).map(
    ([value, label]) => ({ value, label })
);

export function isDynamicDateToken(value: unknown): value is DynamicDateToken {
    return typeof value === "string" && Object.values(DynamicDateTokens).includes(value as DynamicDateToken);
}
