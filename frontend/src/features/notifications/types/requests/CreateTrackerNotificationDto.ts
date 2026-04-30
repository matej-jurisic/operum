export interface CreateNotificationConditionDto {
    code: string;
    operator: string;
    value: string;
    conditionFields: { fieldId: string; purpose: string }[];
}

export interface CreateTrackerNotificationDto {
    name: string;
    isEnabled: boolean;
    viewIds: string[];
    condition: CreateNotificationConditionDto;
}

export interface UpdateTrackerNotificationDto extends CreateTrackerNotificationDto {}
