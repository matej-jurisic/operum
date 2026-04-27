import {
    Anchor,
    Box,
    Group,
    Title,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { observer } from "mobx-react";
import Header from "../../../shared/components/Header";

const NAV_LINKS = [
    { label: "Features", id: "features" },
    { label: "Data Types", id: "data-types" },
    { label: "Analytics", id: "analytics" },
    { label: "Collaboration", id: "collaboration" },
];

interface Props {
    scrolled: boolean;
    scrollTo: (id: string) => void;
    onAuthOpen: (tab: "login" | "register") => void;
    color?: string;
}

const HomeNavbar = observer(({ scrolled, scrollTo, color }: Props) => {
    const theme = useMantineTheme();
    const { colorScheme } = useMantineColorScheme();
    const isDark = colorScheme === "dark";
    const iconColor = color ?? theme.primaryColor;

    return (
        <Box
            p={"md"}
            style={{
                position: "fixed",
                top: 0,
                left: 0,
                right: 0,
                zIndex: 1000,
                backdropFilter: scrolled ? "blur(14px)" : "none",
                WebkitBackdropFilter: scrolled ? "blur(14px)" : "none",
                background: scrolled
                    ? isDark
                        ? "rgba(26, 27, 30, 0.88)"
                        : "rgba(255, 255, 255, 0.88)"
                    : "transparent",
                borderBottom: scrolled
                    ? "1px solid var(--mantine-color-default-border)"
                    : "1px solid transparent",
                transition:
                    "background 0.25s ease, border-color 0.25s ease, backdrop-filter 0.25s ease",
            }}
        >
            <Group justify="space-between">
                <Box flex={{ base: "none", md: 1 }}>
                    <Title
                        order={2}
                        fw={800}
                        c={iconColor}
                        style={{ cursor: "pointer", userSelect: "none" }}
                        onClick={() => scrollTo("hero")}
                    >
                        Operum
                    </Title>
                </Box>

                {/* Navigation Links */}
                <Group gap={2} visibleFrom="md">
                    {NAV_LINKS.map((link) => (
                        <Anchor
                            key={link.id}
                            component="button"
                            onClick={() => scrollTo(link.id)}
                            c="dimmed"
                            underline="never"
                            fz="sm"
                            fw={500}
                            style={{
                                padding: "5px 12px",
                                borderRadius: "var(--mantine-radius-sm)",
                            }}
                        >
                            {link.label}
                        </Anchor>
                    ))}
                </Group>

                <Group flex={{ base: "none", md: 1 }} justify="flex-end">
                    <Header />
                </Group>
            </Group>
        </Box>
    );
});

export default HomeNavbar;
