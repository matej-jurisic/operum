export interface EntryRevisionFieldChangeDto {
    fieldId: string;
    fieldName: string;
    fieldType: string;
    oldValue?: string | null;
    newValue?: string | null;
}

export interface EntryRevisionDto {
    id: string;
    changeType: string;
    changedAt: Date;
    changedByUserName: string;
    changes: EntryRevisionFieldChangeDto[];
}
