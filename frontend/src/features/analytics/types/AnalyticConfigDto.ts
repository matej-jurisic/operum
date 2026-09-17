export interface PurposeDto {
    name: string;
    allowedDataTypes: string[];
    /** True for a purpose the calculation runs fine without (Min/Max's Display field). */
    optional: boolean;
}

export interface CodeDto {
    code: string;
    name: string;
    purposes: PurposeDto[];
}

/** Line/Bar only: one grouping option and the aggregation codes it allows. */
export interface GroupingDto {
    grouping: string;
    name: string;
    /** Field types the grouping purpose accepts under this grouping. */
    allowedDataTypes: string[];
    /** Aggregation codes valid with this grouping. */
    allowedCodes: string[];
}

export interface ResultTypeDto {
    name: string;
    /** True for result types only offered when building a saved widget (a Goal), not in Explore or a notification condition. */
    widgetOnly: boolean;
    codes: CodeDto[];
    /** Line/Bar only: the calculation is a (grouping, code) pair; empty for every other result type. */
    groupings: GroupingDto[];
    /** Line/Bar only: the purpose the grouping constrains; "" for every other result type. */
    groupingPurpose: string;
}

export interface AnalyticConfigDto {
    resultTypes: ResultTypeDto[];
}

/** Whether this result type's calculation is a (grouping, code) pair rather than a bare code. */
export const usesGrouping = (rt: ResultTypeDto | undefined): boolean =>
    !!rt && rt.groupings.length > 0;

/** The field-mapping purposes for a chosen (grouping, code): grouping purpose first, then the aggregation's own. */
export function effectivePurposes(
    rt: ResultTypeDto | undefined,
    grouping: GroupingDto | undefined,
    code: CodeDto | undefined,
): PurposeDto[] {
    if (!rt) return [];
    const base = code?.purposes ?? [];
    if (!rt.groupingPurpose || !grouping) return base;
    return [
        {
            name: rt.groupingPurpose,
            allowedDataTypes: grouping.allowedDataTypes,
            optional: false,
        },
        ...base,
    ];
}
