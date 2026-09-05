import { NotificationEventDto } from "../types/NotificationDto";

const OPERATOR_PHRASES: Record<string, string> = {
    "Equals": "is",
    "Not Equals": "is not",
    "Greater Than": "is greater than",
    "Greater Than Or Equal": "is at least",
    "Less Than": "is less than",
    "Less Than Or Equal": "is at most",
    "Contains": "contains",
    "Starts With": "starts with",
    "Ends With": "ends with",
};

export function operatorPhrase(operator: string): string {
    return OPERATOR_PHRASES[operator] ?? operator.toLowerCase();
}

export function displayValue(value: unknown): string {
    if (value === undefined || value === null || value === "") return "…";
    if (value instanceof Date) return value.toLocaleDateString();
    return String(value);
}

type ScheduleEvent = Pick<
    NotificationEventDto,
    | "eventType"
    | "timeOfDay"
    | "intervalDays"
    | "skipWeekendsDay"
    | "intervalWeeks"
    | "daysOfWeek"
    | "dayOfMonth"
    | "lastDayOfMonth"
    | "skipWeekendsMonth"
>;

export function schedulePhrase(event: ScheduleEvent): string {
    const time = event.timeOfDay ?? "09:00";
    switch (event.eventType) {
        case "Day": {
            const interval = event.intervalDays ?? 1;
            const freq = interval === 1 ? "every day" : `every ${interval} days`;
            const skip = event.skipWeekendsDay ? ", weekdays only" : "";
            return `${freq} at ${time}${skip}`;
        }
        case "Week": {
            const interval = event.intervalWeeks ?? 1;
            const days = event.daysOfWeek?.length ? event.daysOfWeek.join(", ") : "a day";
            const freq = interval === 1 ? "every week" : `every ${interval} weeks`;
            return `${freq} on ${days} at ${time}`;
        }
        case "Month": {
            const day = event.lastDayOfMonth
                ? "the last day of the month"
                : `day ${event.dayOfMonth ?? 1} of the month`;
            const skip = event.skipWeekendsMonth ? ", weekdays only" : "";
            return `on ${day} at ${time}${skip}`;
        }
        default:
            return "";
    }
}

export interface ConditionClause {
    subject: string;
    operator: string;
    value: string;
}

export interface SentenceParams {
    valueMode: string;
    isScheduled: boolean;
    event: ScheduleEvent;
    analyticSubject?: string;
    clauses: ConditionClause[];
}

/** Builds a live "Notify me..." preview sentence from the in-progress form state. */
export function buildNotificationSentence({
    valueMode,
    isScheduled,
    event,
    analyticSubject,
    clauses,
}: SentenceParams): string {
    if (valueMode === "Analytic") {
        const subject = analyticSubject ?? "the value";
        const condition = clauses.length
            ? clauses
                  .map((c, i) => `${i === 0 ? subject : "and it"} ${operatorPhrase(c.operator)} ${c.value}`)
                  .join(" ")
            : `${subject} changes`;
        return isScheduled
            ? `Notify me ${schedulePhrase(event)} if ${condition}`
            : `Notify me as soon as ${condition}`;
    }

    const condition = clauses.length
        ? clauses.map((c, i) => `${i === 0 ? "" : "and "}${c.subject} ${operatorPhrase(c.operator)} ${c.value}`.trim()).join(" ")
        : "any entry";
    return isScheduled
        ? `Notify me ${schedulePhrase(event)} about entries where ${condition}`
        : `Notify me as soon as an entry matches: ${condition}`;
}
