export interface CreateAnalyticFieldDto {
    fieldId: string;
    purpose: string;
}

export interface CreateAnalyticDto {
    code: string;
    type: string;
    /** Optional: left unset, the analytic falls back to its definition's own label. */
    name?: string;
    analyticFields: CreateAnalyticFieldDto[];
}
