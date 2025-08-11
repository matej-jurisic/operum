import { Button } from "@mantine/core";
import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import api from "../api/api";

export function ConfirmEmail() {
    const [messages, setMessages] = useState<string[]>([]);
    const location = useLocation();
    const navigate = useNavigate();

    useEffect(() => {
        const params = new URLSearchParams(location.search);
        const userId = params.get("userId");
        const token = params.get("token");
        if (!userId || !token) {
            setMessages(["Invalid confirmation link."]);
            return;
        }
        const ConfirmEmail = async () => {
            const response = await api.get(
                `/auth/confirm-email?userId=${userId}&token=${token}`
            );
            setMessages(response.data.messages);
        };

        ConfirmEmail();
    }, [location.search]);

    return (
        <div>
            {messages.map((msg, index) => (
                <p key={index}>{msg}</p>
            ))}
            <Button
                onClick={() => {
                    navigate("/auth");
                }}
            >
                Go to Login
            </Button>
        </div>
    );
}
