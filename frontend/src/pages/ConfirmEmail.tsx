import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../api/api";

export function ConfirmEmail() {
    const { userId, token } = useParams();
    const [messages, setMessages] = useState<string[]>([]);

    useEffect(() => {
        const ConfirmEmail = async () => {
            const response = await api.get(
                `/auth/confirm-email?userId=${userId}&token=${token}`
            );
            setMessages(response.data.messages);
        };

        ConfirmEmail();
    }, [userId, token]);

    return (
        <div>
            {messages.map((msg, index) => (
                <p key={index}>{msg}</p>
            ))}
        </div>
    );
}
