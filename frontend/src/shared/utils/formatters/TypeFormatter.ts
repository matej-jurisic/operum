export const parseDate = (dateStr: string): Date => {
    const parts = dateStr.split(" ");
    const datePart = parts[0];
    const timePart = parts[1] || "00:00:00";

    const [day, month, year] = datePart.split("/").map(Number);
    const [hours, minutes, seconds] = timePart.split(":").map(Number);

    return new Date(
        year,
        month - 1,
        day,
        hours || 0,
        minutes || 0,
        seconds || 0
    );
};

export const formatDateTime = (value?: string) =>
    value
        ? new Date(value).toLocaleString("en-GB", {
              day: "2-digit",
              month: "2-digit",
              year: "numeric",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
              hour12: false,
          })
        : "";

// Format date for display
export const formatFullDate = (date: Date) => {
    return new Date(date).toLocaleDateString(undefined, {
        weekday: "long",
        month: "long",
        day: "numeric",
        year: "numeric",
    });
};

export const formatDateTimeFromDate = (date?: Date) =>
    date
        ? date.toLocaleString("en-GB", {
              day: "2-digit",
              month: "2-digit",
              year: "numeric",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
              hour12: false,
          })
        : "";

export const formatDateOnly = (value?: string) =>
    value
        ? new Date(value).toLocaleDateString("en-GB", {
              day: "2-digit",
              month: "2-digit",
              year: "numeric",
          })
        : "";

export const formatDateOnlyFromDate = (date: Date) =>
    date
        ? date.toLocaleDateString("en-GB", {
              day: "2-digit",
              month: "2-digit",
              year: "numeric",
          })
        : "";

export const formatTimeSpan = (value?: string) => {
    if (!value) return "";
    const [hours, minutes, seconds] = value.split(":");
    return `${hours}:${minutes}:${seconds.split(".")[0]}`;
};

export const formatMinutesToTime = (minutes?: number): string => {
    if (minutes === undefined || minutes === null) return "";

    const hours = Math.floor(minutes / 60);
    const mins = Math.floor(minutes % 60);
    const seconds = Math.floor((minutes % 1) * 60);
    return `${hours.toString().padStart(2, "0")}:${mins
        .toString()
        .padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;
};

export const formatBoolean = (value?: number): string => {
    if (value === undefined || value === null) return "";
    return value === 1 ? "Yes" : "No";
};
