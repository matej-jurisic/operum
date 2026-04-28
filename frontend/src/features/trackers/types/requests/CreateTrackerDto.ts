export interface CreateTrackerDto {
    name: string;
    description?: string;
    color?: string;
    icon?: string;
    trackerTypeId?: number;
    templateTrackerId?: string;
}
