export interface TrackerUpsertDto {
    name: string;
    description?: string;
    color?: string;
    trackerTypeId?: number;
    templateTrackerId?: string;
}
