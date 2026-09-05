/// <reference lib="webworker" />
import { cleanupOutdatedCaches, precacheAndRoute } from "workbox-precaching";

declare const self: ServiceWorkerGlobalScope;

cleanupOutdatedCaches();
precacheAndRoute(self.__WB_MANIFEST);

self.addEventListener("push", (event) => {
    const payload = event.data?.json() ?? {};
    const title: string = payload.title ?? "Operum";
    const body: string = payload.body ?? "";

    event.waitUntil(
        self.registration.showNotification(title, {
            body,
            icon: "/icon.svg",
            badge: "/icon.svg",
            // Only the inner `data` object, not the whole payload -- notificationclick below
            // reads `.data.url` off the notification, and title/body are already shown.
            data: payload.data,
        }),
    );
});

self.addEventListener("notificationclick", (event) => {
    event.notification.close();

    const urlToOpen = event.notification.data?.url || "/";

    event.waitUntil(
        self.clients
            .matchAll({ type: "window", includeUncontrolled: true })
            .then((clientList) => {
                for (const client of clientList) {
                    if (client.url.includes(urlToOpen) && "focus" in client) {
                        return client.focus();
                    }
                }

                return self.clients.openWindow(urlToOpen);
            }),
    );
});
