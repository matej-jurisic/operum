export interface CreateTrackerDto {
    name: string;
    description?: string;
    color?: string;
    trackerTypeId?: string;
    templateTrackerId?: string;
}
