export interface AdminTrackerDto {
    id: string;
    name: string;
    description?: string;
    color?: string;
    icon?: string;
    ownerId: string;
    ownerName?: string;
    fieldCount: number;
    entryCount: number;
    collaboratorCount: number;
    lastEntryAt?: string;
}
