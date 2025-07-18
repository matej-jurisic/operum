import { notifications } from "@mantine/notifications";
import axios, { AxiosError } from "axios";
import { ApiResponse } from "../model/common/ApiResponse";

axios.defaults.withCredentials = true;

const api = axios.create({
    baseURL: import.meta.env.VITE_REACT_API_URL,
    headers: {
        "Content-Type": "application/json",
    },
    timeout: 10000,
});

api.interceptors.response.use(
    (response) => {
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
                });
            });
        }
        return response;
    },
    (error: AxiosError<ApiResponse>) => {
        const messages = error.response?.data?.messages;
        if (messages && messages.length > 0) {
            messages.forEach((m) => {
                notifications.show({
                    title: "Error",
                    message: m,
                    color: "red",
                });
            });
        } else {
            notifications.show({
                title: "Error",
                message: "An unknown error occurred. Please try again.",
                color: "red",
            });
        }

        console.error(error);
        return Promise.reject(error);
    }
);

export default api;
