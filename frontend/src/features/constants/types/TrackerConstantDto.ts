export interface TrackerConstantDto {
    id: string;
    name: string;
    type: string;
    value: string;
}

export interface CreateTrackerConstantDto {
    name: string;
    type: string;
    value: string;
}

export interface UpdateTrackerConstantDto {
    name: string;
    type: string;
    value: string;
}
