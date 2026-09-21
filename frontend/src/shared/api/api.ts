import { notifications } from "@mantine/notifications";
import axios, { AxiosError, AxiosRequestConfig } from "axios";
import { AuthResponseDto } from "../../features/auth/types/AuthResponseDto";
import { setGlobalLoading } from "../context/LoadingContext";
import globalStore from "../stores/GlobalStore";
import { ApiResponse } from "../types/ApiResponse";

axios.defaults.withCredentials = true;

const USERNAME_KEY = "username";
const ID_KEY = "id";
const EXP_KEY = "exp";
const ROLES_KEY = "roles";

let isRefreshing = false;
let failedQueue: {
    resolve: (value?: unknown) => void;
    reject: (reason?: unknown) => void;
}[] = [];

const processQueue = (error: unknown, token: string | null = null) => {
    failedQueue.forEach((prom) => {
        if (token) {
            prom.resolve(token);
        } else {
            prom.reject(error);
        }
    });
    failedQueue = [];
};

const setUserData = (user: AuthResponseDto) => {
    globalStore.setCurrentUser({
        userName: user.userName,
        id: user.id,
        roles: user.roles,
    });
    localStorage.setItem(USERNAME_KEY, user.userName);
    localStorage.setItem(ID_KEY, user.id);
    localStorage.setItem(ROLES_KEY, JSON.stringify(user.roles));
    localStorage.setItem(EXP_KEY, (Date.now() + 1000 * 60 * 2).toString());
};

const clearUserData = () => {
    globalStore.setCurrentUser(undefined);
    localStorage.removeItem(USERNAME_KEY);
    localStorage.removeItem(ID_KEY);
    localStorage.removeItem(EXP_KEY);
};

const showSessionExpiredNotification = () => {
    notifications.show({
        title: "Session Expired",
        message: "Please log in again.",
        color: "red",
        withBorder: true,
    });
};

/** Timeout for calls that can legitimately run long: imports, bulk edits, and integration syncs. */
export const LONG_REQUEST_TIMEOUT_MS = 5 * 60 * 1000;

const api = axios.create({
    baseURL: import.meta.env.VITE_REACT_API_URL,
    headers: {
        "Content-Type": "application/json",
    },
    timeout: 10000,
});

api.interceptors.request.use(
    (config) => {
        setGlobalLoading(true);
        return config;
    },
    (error) => {
        setGlobalLoading(false);
        return Promise.reject(error);
    }
);

api.interceptors.response.use(
    (response) => {
        setGlobalLoading(false);

        if (response.config.responseType === "blob") {
            return response;
        }

        return response.data;
    },
    async (error: AxiosError<ApiResponse>) => {
        try {
            const originalRequest = error.config as AxiosRequestConfig & {
                _retry?: boolean;
            };

            // Avoid intercepting the refresh call's own 401, which would loop.
            if (originalRequest.url?.includes("/auth/refresh")) {
                clearUserData();
                showSessionExpiredNotification();
                return Promise.reject(error);
            }

            if (error.response?.status === 401 && !originalRequest._retry) {
                if (isRefreshing) {
                    return new Promise((resolve, reject) => {
                        failedQueue.push({ resolve, reject });
                    }).then(() => api(originalRequest));
                }

                originalRequest._retry = true;
                isRefreshing = true;

                try {
                    // Raw axios instance, not `api`, to avoid the interceptor loop.
                    const response = await axios.post(
                        `${import.meta.env.VITE_REACT_API_URL}/auth/refresh`,
                        {
                            withCredentials: true,
                        }
                    );

                    setUserData(response.data.data);

                    isRefreshing = false;
                    processQueue(null, "success");

                    return api(originalRequest);
                } catch (refreshError) {
                    isRefreshing = false;
                    processQueue(refreshError);
                    clearUserData();
                    showSessionExpiredNotification();
                    return Promise.reject(refreshError);
                }
            }

            if (error.response?.status === 429) {
                notifications.show({
                    title: "Error",
                    message: "Too many requests. Wait a moment and try again.",
                    color: "red",
                    withBorder: true,
                });
                return Promise.reject(error.response.data);
            }

            if (
                originalRequest.responseType === "blob" &&
                error.response?.data instanceof Blob
            ) {
                // Error responses to blob requests still arrive as a JSON blob; decode as text first.
                try {
                    const text = await error.response.data.text();
                    const errorData = JSON.parse(text);
                    const messages = errorData?.messages;
                    if (messages?.length) {
                        messages.forEach((m: string) => {
                            notifications.show({
                                title: "Error",
                                message: m,
                                color: "red",
                                withBorder: true,
                            });
                        });
                    } else {
                        notifications.show({
                            title: "Error",
                            message:
                                "An unknown error occurred. Please try again.",
                            color: "red",
                            withBorder: true,
                        });
                    }
                } catch {
                    notifications.show({
                        title: "Error",
                        message: "An unknown error occurred. Please try again.",
                        color: "red",
                        withBorder: true,
                    });
                }
            } else {
                const messages = error.response?.data?.messages;
                if (messages?.length) {
                    messages.forEach((m: string) => {
                        notifications.show({
                            title: "Error",
                            message: m,
                            color: "red",
                            withBorder: true,
                        });
                    });
                } else {
                    notifications.show({
                        title: "Error",
                        message: "An unknown error occurred. Please try again.",
                        color: "red",
                        withBorder: true,
                    });
                }
            }

            return Promise.reject(error.response?.data);
        } finally {
            setGlobalLoading(false);
        }
    }
);

export default api;
