import { notifications } from "@mantine/notifications";

export const notifySuccess = (message: string, title?: string) =>
    notifications.show({ title, message, color: "teal", withBorder: true });
