/** Mirrors LegacyLineBarCodes on the backend: maps a pre-grouping fused Line/Bar code from an old bookmarked URL to (grouping, code). */
export const LEGACY_LINE_BAR_CODES: Record<
    string,
    { grouping: string; code: string }
> = {
    "Line Chart": { grouping: "None", code: "Raw Values" },
    "Aggregated Sum": { grouping: "Exact", code: "Sum" },
    "Cumulative Sum": { grouping: "Exact", code: "Cumulative Sum" },
    Daily: { grouping: "Daily", code: "Sum" },
    Weekly: { grouping: "Weekly", code: "Sum" },
    Monthly: { grouping: "Monthly", code: "Sum" },
    Yearly: { grouping: "Yearly", code: "Sum" },
    "Count Bar Chart": { grouping: "Exact", code: "Count" },
    "Sum Bar Chart": { grouping: "Exact", code: "Sum" },
    "Average Bar Chart": { grouping: "Exact", code: "Average" },
    "Daily Bar Chart": { grouping: "Daily", code: "Sum" },
    "Weekly Bar Chart": { grouping: "Weekly", code: "Sum" },
    "Monthly Bar Chart": { grouping: "Monthly", code: "Sum" },
    "Yearly Bar Chart": { grouping: "Yearly", code: "Sum" },
};

export function resolveLegacyCalculation(
    grouping: string | null | undefined,
    code: string | null | undefined,
): { grouping: string | null; code: string | null } {
    if (grouping || !code || !(code in LEGACY_LINE_BAR_CODES))
        return { grouping: grouping ?? null, code: code ?? null };
    const mapped = LEGACY_LINE_BAR_CODES[code];
    return { grouping: mapped.grouping, code: mapped.code };
}
