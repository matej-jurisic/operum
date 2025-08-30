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

export const formatTimeSpan = (value?: string) => {
    if (!value) return "";
    const [hours, minutes, seconds] = value.split(":");
    return `${hours}:${minutes}:${seconds.split(".")[0]}`;
};
