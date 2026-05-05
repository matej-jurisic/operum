export interface CreateNotificationEventDto {
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

export interface CreateNotificationConditionFilterDto {
    fieldId?: string | null;
    operator: string;
    value?: string | null;
}

export interface CreateNotificationConditionPurposeFieldDto {
    fieldId: string;
    purpose: string;
}

export interface CreateNotificationConditionDto {
    valueMode: string;
    analyticCode?: string | null;
    purposeFields: CreateNotificationConditionPurposeFieldDto[];
    filters: CreateNotificationConditionFilterDto[];
}

export interface CreateTrackerNotificationDto {
    name: string;
    isEnabled: boolean;
    viewIds: string[];
    event: CreateNotificationEventDto;
    condition: CreateNotificationConditionDto;
}

export interface UpdateTrackerNotificationDto extends CreateTrackerNotificationDto {}
