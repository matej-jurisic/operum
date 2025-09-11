export interface FieldAnalyticsDto {
    fieldName: string;
    fieldType: string;

    // Common
    count?: number;

    // For Numbers
    average?: number;
    min?: number;
    minEntryId?: string;
    max?: number;
    maxEntryId?: string;
    stdDev?: number;
    sum?: number;

    // For DateTime
    minDateTime?: string; // ISO string (e.g. "2025-08-07T12:34:56.789Z")
    minDateTimeEntryId?: string;
    maxDateTime?: string;
    maxDateTimeEntryId?: string;

    // For Date
    minDate?: string;
    minDateEntryId?: string;
    maxDate?: string;
    maxDateEntryId?: string;

    // For TimeSpan
    minTimeSpan?: string; // e.g., "00:30:00" or standard .NET timespan
    minTimeSpanEntryId?: string;
    maxTimeSpan?: string;
    maxTimeSpanEntryId?: string;
    averageTimeSpan?: string;
    sumTimeSpan?: string;

    // For Boolean
    trueCount?: number;
    falseCount?: number;
    truePercentage?: number;
}
