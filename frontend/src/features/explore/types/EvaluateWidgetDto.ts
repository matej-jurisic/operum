import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";

/** One inline, field-bound filter clause. A missing value means "has no value" for the
    two equality operators; for anything else a missing value drops the clause. */
export interface EvaluateFilterClauseDto {
    fieldId: string;
    operator: string;
    value?: string;
}

/** One source of an ad hoc evaluation: a tracker, its field mapping, an optional saved
    view for the base filter and sort, and inline clauses ANDed on top. */
export interface EvaluateSourceDto {
    trackerId: string;
    fields: CreateAnalyticFieldDto[];
    viewId?: string;
    filters: EvaluateFilterClauseDto[];
}

/** A chart definition evaluated once against live data, saved nowhere. One source renders
    on its own; multiple sources merge the way a multi-tracker widget does. */
export interface EvaluateWidgetDto {
    resultType: string;
    code: string;
    /** Combined charts only: keep just the x-axis values every source has a point for. */
    matchedValuesOnly?: boolean;
    sources: EvaluateSourceDto[];
}
