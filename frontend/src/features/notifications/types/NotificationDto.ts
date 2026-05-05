export interface NotificationEventDto {
    eventType: string;
    timeOfDay?: string | null;
    intervalDays?: number | null;
    skipWeekendsDay?: boolean | null;
    intervalWeeks?: number | null;
    daysOfWeek?: string[] | null;
    dayOfMonth?: number | null;
    lastDayOfMonth?: boolean | null;
    skipWeekendsMonth?: boolean | null;
}

export interface NotificationConditionFilterDto {
    fieldId?: string | null;
    operator: string;
    value?: string | null;
}

export interface NotificationConditionPurposeFieldDto {
    purpose: string;
    fieldId: string;
}

export interface NotificationConditionDto {
    valueMode: string;
    analyticCode?: string | null;
    analyticResultType?: string | null;
    purposeFields: NotificationConditionPurposeFieldDto[];
    filters: NotificationConditionFilterDto[];
}

export interface TrackerNotificationDto {
    id: string;
    name: string;
    isEnabled: boolean;
    isTriggered: boolean;
    lastEvaluatedAt?: string | null;
    lastFiredAt?: string | null;
    viewIds: string[];
    event: NotificationEventDto;
    condition: NotificationConditionDto;
}
