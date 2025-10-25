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
            await api.post(
                `/auth/confirm-email?userId=${userId}&token=${token}`
            );
            navigate("/home");
        };

        ConfirmEmail();
    }, []);

    return <></>;
}
