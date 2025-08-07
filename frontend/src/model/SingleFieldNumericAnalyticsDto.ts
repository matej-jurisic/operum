export interface SingleFieldAnalyticsDto {
    // Common
    count?: number;

    // For Numbers
    average?: number;
    min?: number;
    max?: number;
    stdDev?: number;

    // For DateTime
    minDateTime?: string; // ISO string (e.g. "2025-08-07T12:34:56.789Z")
    maxDateTime?: string;

    // For Date
    minDate?: string;
    maxDate?: string;

    // For TimeSpan
    minTimeSpan?: string; // e.g., "00:30:00" or standard .NET timespan
    maxTimeSpan?: string;
    averageTimeSpan?: string;

    // For Boolean
    trueCount?: number;
    falseCount?: number;
    truePercentage?: number;
}
