import { FieldDto } from "./FieldDto";

export interface TrackerDto {
    id: string;
    name: string;
    description?: string;
    color?: string;
    fields: FieldDto[];
    trackerTypeId?: number;
    defaultViewId?: string;
    trackerTypeName?: string;
}
