export interface NotificationConditionFieldDto {
    fieldId: string;
    purpose: string;
}

export interface NotificationConditionDto {
    code: string;
    resultType: string;
    operator: string;
    value: string;
    conditionFields: NotificationConditionFieldDto[];
}

export interface TrackerNotificationDto {
    id: string;
    name: string;
    isEnabled: boolean;
    lastTriggeredAt?: string;
    isTriggered: boolean;
    viewIds: string[];
    condition: NotificationConditionDto;
}
