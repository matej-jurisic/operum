import { notifySuccess } from "../../../shared/utils/notify";
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
                notifySuccess("Email confirmed. You can now log in.");
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
