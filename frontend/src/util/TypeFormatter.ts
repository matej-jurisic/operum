export const formatDateTime = (value?: string) =>
    value ? new Date(value).toLocaleString() : "";

export const formatDateOnly = (value?: string) =>
    value ? new Date(value).toLocaleDateString() : "";

export const formatTimeSpan = (value?: string) => {
    if (!value) return "";
    const [hours, minutes, seconds] = value.split(":");
    return `${hours}:${minutes}:${seconds.split(".")[0]}`;
};
