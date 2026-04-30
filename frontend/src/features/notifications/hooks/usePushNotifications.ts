import { useEffect, useState } from "react";
import api from "../../../shared/api/api";

type PushStatus = "unsupported" | "denied" | "subscribed" | "unsubscribed" | "loading";

function urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
    const raw = atob(base64);
    return Uint8Array.from(raw, (c) => c.charCodeAt(0));
}

export function usePushNotifications() {
    const [status, setStatus] = useState<PushStatus>("loading");

    useEffect(() => {
        checkStatus().then(setStatus);
    }, []);

    async function checkStatus(): Promise<PushStatus> {
        if (!("PushManager" in window) || !("serviceWorker" in navigator)) return "unsupported";
        if (Notification.permission === "denied") return "denied";

        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.getSubscription();
        return sub ? "subscribed" : "unsubscribed";
    }

    async function subscribe() {
        setStatus("loading");
        try {
            const reg = await navigator.serviceWorker.ready;

            const keyRes = await api.get("/push/public-key");
            const publicKey = keyRes.data;

            const sub = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(publicKey),
            });

            const json = sub.toJSON();
            await api.post("/push/subscribe", {
                endpoint: json.endpoint,
                p256dh: json.keys!.p256dh,
                auth: json.keys!.auth,
            });

            setStatus("subscribed");
        } catch (e) {
            console.error("Push subscribe failed:", e);
            setStatus(Notification.permission === "denied" ? "denied" : "unsubscribed");
        }
    }

    async function unsubscribe() {
        setStatus("loading");
        try {
            const reg = await navigator.serviceWorker.ready;
            const sub = await reg.pushManager.getSubscription();
            if (sub) {
                await api.delete("/push/subscribe", { data: { endpoint: sub.endpoint } });
                await sub.unsubscribe();
            }
            setStatus("unsubscribed");
        } catch (e) {
            console.error("Push unsubscribe failed:", e);
            setStatus("subscribed");
        }
    }

    return { status, subscribe, unsubscribe };
}
