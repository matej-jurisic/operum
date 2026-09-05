/** Mirrors NotificationPurposes on the backend. Entry mode's own purpose (Analytic mode's
    field-purposes come from the analytic config instead): the fields whose values are listed
    out via the {fieldValueList} message token. */
export const NotificationPurposes = {
    Display: "Display",
} as const;
