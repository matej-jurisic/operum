export interface CreateTrackerDto {
    name: string;
    description?: string;
    color?: string;
    trackerTypeId?: number;
    templateTrackerId?: string;
}
