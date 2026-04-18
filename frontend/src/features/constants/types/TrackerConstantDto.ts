export interface TrackerConstantValueFilterDto {
    id: string;
    fieldId: string;
    operator: string;
    value: string | null;
}

export interface TrackerConstantValueDto {
    id: string;
    priority: number;
    value: string;
    filters: TrackerConstantValueFilterDto[];
}

export interface CreateTrackerConstantValueFilterDto {
    fieldId: string;
    operator: string;
    value: string | null;
}

export interface CreateTrackerConstantValueDto {
    priority: number;
    value: string;
    filters: CreateTrackerConstantValueFilterDto[];
}

export interface TrackerConstantDto {
    id: string;
    name: string;
    type: string;
    value: string;
    values: TrackerConstantValueDto[];
}

export interface CreateTrackerConstantDto {
    name: string;
    type: string;
    value: string;
    values: CreateTrackerConstantValueDto[];
}

export interface UpdateTrackerConstantDto {
    name: string;
    type: string;
    value: string;
    values: CreateTrackerConstantValueDto[];
}
