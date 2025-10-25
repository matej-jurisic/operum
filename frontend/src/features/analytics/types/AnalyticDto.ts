import { FieldDto } from "../../fields/types/FieldDto";

export interface AnalyticDto {
    id: string;
    name: string;
    description?: string;
    code: string;
    resultType: string;
    order?: number;
}

export interface SingleValueAnalyticDto extends AnalyticDto {
    value: string;
    valueField?: FieldDto;
    entryId?: string;
}

export interface LineChartAnalyticDto extends AnalyticDto {
    xField: FieldDto;
    yField: FieldDto;
    points: { x: string; y: number }[];
}

export interface DonutChartAnaylticDto extends AnalyticDto {
    nameField: FieldDto;
    valueField: FieldDto;
    points: { name: string; value: number }[];
}

export interface ScatterChartAnalyticDto extends AnalyticDto {
    xField: FieldDto;
    yField: FieldDto;
    points: { x: number; y: number }[];
}

export interface CalendarAnalyticDto extends AnalyticDto {
    whenField: FieldDto;
    whatField: FieldDto;
    points: { date: string; name: string; entryId: string }[];
}
