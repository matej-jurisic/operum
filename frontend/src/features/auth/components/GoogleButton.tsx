import { Button } from "@mantine/core";
import { useEffect } from "react";
import useAuth from "../hooks/useAuth";

export const GoogleButton = () => {
    const auth = useAuth();

    useEffect(() => {
        const script = document.createElement("script");
        script.src = "https://accounts.google.com/gsi/client";
        script.async = true;
        script.defer = true;
        script.onload = () => {
            window.google?.accounts.id.initialize({
                client_id: import.meta.env.VITE_REACT_GOOGLE_CLIENT,
                callback: (response) => {
                    if (response.credential)
                        auth.loginWithGoogle(response.credential);
                },
            });
        };
        document.body.appendChild(script);
    }, []);

    const handleGoogleLogin = () => {
        window.google?.accounts.id.prompt();
    };

    return <Button onClick={handleGoogleLogin}>G</Button>;
};
