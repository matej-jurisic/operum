import { FieldValueDto } from "../../fields/types/FieldValueDto";

export interface EntryDto {
    id: string;
    trackerId: string;
    createdAt: Date;
    fieldValues: FieldValueDto[];
}
