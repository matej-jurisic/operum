import { FieldValueDto } from "./FieldValueDto";

export interface EntryDto {
    id: string;
    trackerId: string;
    createdAt: Date;
    fieldValues: FieldValueDto[];
}
