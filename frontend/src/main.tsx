import { MantineProvider } from "@mantine/core";
import { Notifications } from "@mantine/notifications";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import App from "./App.tsx";
import { LoadingProvider } from "./context/LoadingContext.tsx";
import "./index.css";

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <LoadingProvider>
            <MantineProvider>
                <Notifications position="bottom-right" />
                <App />
            </MantineProvider>
        </LoadingProvider>
    </StrictMode>
);
