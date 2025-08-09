import { notifications } from "@mantine/notifications";
import axios, { AxiosError, AxiosRequestConfig } from "axios";
import { setGlobalLoading } from "../context/LoadingContext";
import { ApiResponse } from "../model/common/ApiResponse"; // import setter

axios.defaults.withCredentials = true;

const refreshToken = async (): Promise<void> => {
    await axios.get(`${import.meta.env.VITE_REACT_API_URL}/auth/refresh`, {
        withCredentials: true,
    });
};

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
        if (
            response.data &&
            response.data.messages &&
            response.data.messages.length > 0
        ) {
            response.data.messages.forEach((m: string) => {
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
        setGlobalLoading(false);

        const originalRequest = error.config as AxiosRequestConfig & {
            _retry?: boolean;
        };

        if (error.response?.status === 401 && !originalRequest._retry) {
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                }).then(() => api(originalRequest));
            }

            originalRequest._retry = true;
            isRefreshing = true;

            try {
                await refreshToken();
                processQueue(null);
                return api(originalRequest);
            } catch (refreshError) {
                processQueue(refreshError);
                notifications.show({
                    title: "Session Expired",
                    message: "Please log in again.",
                    color: "red",
                    withBorder: true,
                });
                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
            }
        }

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

        console.error(error);
        return Promise.reject(error);
    }
);

export default api;
