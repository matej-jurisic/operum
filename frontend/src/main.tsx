import { MantineProvider } from "@mantine/core";
import { Notifications } from "@mantine/notifications";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "@fontsource-variable/inter";
import App from "./App.tsx";
import "./index.css";
import { LoadingProvider } from "./shared/context/LoadingContext.tsx";
import { theme } from "./theme.ts";

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <LoadingProvider>
            <MantineProvider theme={theme}>
                <Notifications position="bottom-right" />
                <App />
            </MantineProvider>
        </LoadingProvider>
    </StrictMode>,
);
