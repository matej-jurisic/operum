import basicSsl from "@vitejs/plugin-basic-ssl";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
    plugins: [
        react(),
        basicSsl(),
        VitePWA({
            registerType: "autoUpdate",
            manifest: {
                name: "Operum",
                short_name: "Operum",
                description: "Flexible data tracking",
                theme_color: "#131314",
                background_color: "#131314",
                display: "standalone",
                icons: [
                    { src: "icon.svg", sizes: "any", type: "image/svg+xml", purpose: "any maskable" },
                ],
            },
        }),
    ],
    build: {
        rollupOptions: {
            output: {
                manualChunks(id) {
                    if (id.includes("recharts") || id.includes("@mantine/charts")) {
                        return "vendor-charts";
                    }
                    if (id.includes("@mantine/")) {
                        return "vendor-mantine";
                    }
                    if (
                        id.includes("node_modules/react/") ||
                        id.includes("node_modules/react-dom/") ||
                        id.includes("react-router")
                    ) {
                        return "vendor-react";
                    }
                    if (id.includes("node_modules")) {
                        return "vendor-misc";
                    }
                },
            },
        },
    },
    server: {
        host: "localhost",
        port: 3000,
        https: true,
        watch: {
            usePolling: true,
        },
    },
});
