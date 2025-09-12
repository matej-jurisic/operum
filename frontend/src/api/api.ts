import { notifications } from "@mantine/notifications";
import axios, { AxiosError, AxiosRequestConfig } from "axios";
import { setGlobalLoading } from "../context/LoadingContext";
import { AuhtResponseDto as AuthResponseDto } from "../model/AuthResponseDto";
import { ApiResponse } from "../model/common/ApiResponse";
import globalStore from "../stores/GlobalStore";

axios.defaults.withCredentials = true;

const USERNAME_KEY = "username";
const ID_KEY = "id";
const EXP_KEY = "exp";
const ROLES_KEY = "roles";

// Use a local variable to prevent multiple concurrent refresh calls
let isRefreshing = false;
let failedQueue: {
    resolve: (value?: unknown) => void;
    reject: (reason?: unknown) => void;
}[] = [];

// Helper function to process the queue of failed requests
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

// Centralized function to set user data and update localStorage
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

// Centralized function to clear user data
const clearUserData = () => {
    globalStore.setCurrentUser(undefined);
    localStorage.removeItem(USERNAME_KEY);
    localStorage.removeItem(ID_KEY);
    localStorage.removeItem(EXP_KEY);
    document.location.reload();
};

const showSessionExpiredNotification = () => {
    notifications.show({
        title: "Session Expired",
        message: "Please log in again.",
        color: "red",
        withBorder: true,
    });
};

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

        const messages = response.data?.messages;
        if (messages?.length) {
            messages.forEach((m: string) => {
                notifications.show({
                    title: "Success",
                    message: m,
                    color: "teal",
                    withBorder: true,
                });
            });
        }
        return response;
    },
    async (error: AxiosError<ApiResponse>) => {
        // A single try-finally block can ensure loading is always turned off.
        try {
            const originalRequest = error.config as AxiosRequestConfig & {
                _retry?: boolean;
            };

            // Check if the request is for the refresh endpoint, we don't want to intercept this
            if (originalRequest.url?.includes("/auth/refresh")) {
                clearUserData();
                showSessionExpiredNotification();
                return Promise.reject(error);
            }

            if (error.response?.status === 401 && !originalRequest._retry) {
                if (isRefreshing) {
                    // If a refresh is already in progress, queue the request
                    return new Promise((resolve, reject) => {
                        failedQueue.push({ resolve, reject });
                    }).then(() => api(originalRequest));
                }

                // Mark that we are now refreshing
                originalRequest._retry = true;
                isRefreshing = true;

                try {
                    // Use a raw axios instance here to avoid the interceptor loop
                    const response = await axios.post(
                        `${import.meta.env.VITE_REACT_API_URL}/auth/refresh`,
                        {
                            withCredentials: true,
                        }
                    );

                    // Update user data with the new token information
                    setUserData(response.data.data);

                    isRefreshing = false;
                    // Process the queue with the new token
                    processQueue(null, "success");

                    // Retry the original request
                    return api(originalRequest);
                } catch (refreshError) {
                    // Refresh failed, clear user data and show a notification
                    isRefreshing = false;
                    processQueue(refreshError);
                    clearUserData();
                    showSessionExpiredNotification();
                    // We must still reject the original request promise here
                    return Promise.reject(refreshError);
                }
            }

            if (
                originalRequest.responseType === "blob" &&
                error.response?.data instanceof Blob
            ) {
                // For blob requests that return JSON errors, we need to read the blob as text
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

            return Promise.reject(error);
        } finally {
            // This ensures that loading is turned off for any outcome,
            // whether the refresh succeeds, fails, or it's a different error.
            setGlobalLoading(false);
        }
    }
);

export default api;
