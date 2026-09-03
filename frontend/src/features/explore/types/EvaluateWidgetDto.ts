import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";

/** One inline, field-bound filter clause. A missing value means "has no value" for the
    two equality operators; for anything else a missing value drops the clause. */
export interface EvaluateFilterClauseDto {
    fieldId: string;
    operator: string;
    value?: string;
}

/** A chart definition evaluated once against live data, saved nowhere. Single source. */
export interface EvaluateWidgetDto {
    resultType: string;
    code: string;
    trackerId: string;
    fields: CreateAnalyticFieldDto[];
    /** Optional saved view supplying the base filter and sort. */
    viewId?: string;
    /** Inline clauses, ANDed on top of the view. */
    filters: EvaluateFilterClauseDto[];
}
