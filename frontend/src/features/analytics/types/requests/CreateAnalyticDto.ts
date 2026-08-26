// The purpose -> field mapping half of a chart definition, shared by the Widget Library
// (features/widgets/types/WidgetDto.ts) and the dashboard's inline "New chart" form
// (CustomAnalyticForm) -- kept here since both features already depended on this file.
export interface CreateAnalyticFieldDto {
    fieldId: string;
    purpose: string;
}
