import { notifications } from "@mantine/notifications";
import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import api from "../../../shared/api/api";

export function ConfirmEmail() {
    const location = useLocation();
    const navigate = useNavigate();

    useEffect(() => {
        const params = new URLSearchParams(location.search);
        const userId = params.get("userId");
        const token = params.get("token");

        const ConfirmEmail = async () => {
            try {
                await api.post(
                    `/auth/confirm-email?userId=${userId}&token=${token}`
                );
                notifications.show({
                    message: "Email confirmed. You can now log in.",
                    color: "teal",
                    withBorder: true,
                });
            } catch {
                // The api layer already surfaced the error
            } finally {
                navigate("/home");
            }
        };

        ConfirmEmail();
    }, []);

    return <></>;
}
