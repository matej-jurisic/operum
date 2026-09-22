export interface CorrelationInsightDto {
    id: string;

    trackerAId: string;
    trackerAName: string;
    trackerAColor?: string;
    matchFieldAId: string;
    valueFieldAId: string;
    valueFieldAName: string;

    trackerBId: string;
    trackerBName: string;
    trackerBColor?: string;
    matchFieldBId: string;
    valueFieldBId: string;
    valueFieldBName: string;

    coefficient: number;
    sampleSize: number;
    computedAt: string;
}
