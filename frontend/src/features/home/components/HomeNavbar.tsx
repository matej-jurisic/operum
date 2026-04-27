import {
    Anchor,
    Box,
    Button,
    Container,
    Group,
    Menu,
    Text,
    useComputedColorScheme,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { observer } from "mobx-react";
import { CiLogout, CiSettings, CiUser } from "react-icons/ci";
import { GoSun } from "react-icons/go";
import { IoMoonOutline } from "react-icons/io5";
import { useNavigate } from "react-router-dom";
import useAuth from "../../auth/hooks/useAuth";
import globalStore from "../../../shared/stores/GlobalStore";

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
}

const HomeNavbar = observer(({ scrolled, scrollTo, onAuthOpen }: Props) => {
    const theme = useMantineTheme();
    const { colorScheme, setColorScheme } = useMantineColorScheme();
    const computedColorScheme = useComputedColorScheme("light");
    const isDark = colorScheme === "dark";
    const navigate = useNavigate();
    const auth = useAuth();

    return (
        <Box
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
            <Container size="lg">
                <Group h={60} justify="space-between">
                    <Text
                        fw={800}
                        size="xl"
                        c={theme.primaryColor}
                        style={{ cursor: "pointer", userSelect: "none" }}
                        onClick={() => scrollTo("hero")}
                    >
                        Operum
                    </Text>

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

                    <Group gap="xs">
                        <Button
                            variant="subtle"
                            size="sm"
                            px="xs"
                            onClick={() =>
                                setColorScheme(
                                    computedColorScheme === "dark"
                                        ? "light"
                                        : "dark"
                                )
                            }
                        >
                            {colorScheme === "light" ? (
                                <IoMoonOutline size={16} />
                            ) : (
                                <GoSun size={16} />
                            )}
                        </Button>

                        {globalStore.currentUser ? (
                            <>
                                <Button
                                    size="sm"
                                    variant="light"
                                    onClick={() => navigate("/trackers")}
                                >
                                    Go to Trackers
                                </Button>
                                <Menu>
                                    <Menu.Target>
                                        <Button
                                            variant="subtle"
                                            size="sm"
                                            px="xs"
                                        >
                                            <CiUser size={18} />
                                        </Button>
                                    </Menu.Target>
                                    <Menu.Dropdown>
                                        <Menu.Item
                                            leftSection={<CiUser size={16} />}
                                        >
                                            {globalStore.currentUser.userName}
                                        </Menu.Item>
                                        {globalStore.userHasRole("admin") && (
                                            <Menu.Item
                                                leftSection={
                                                    <CiSettings size={16} />
                                                }
                                                onClick={() =>
                                                    navigate("/admin-panel")
                                                }
                                            >
                                                Admin Panel
                                            </Menu.Item>
                                        )}
                                        <Menu.Item
                                            leftSection={
                                                <CiLogout size={16} />
                                            }
                                            onClick={() => auth.logout()}
                                        >
                                            Logout
                                        </Menu.Item>
                                    </Menu.Dropdown>
                                </Menu>
                            </>
                        ) : (
                            <>
                                <Button
                                    variant="subtle"
                                    size="sm"
                                    onClick={() => onAuthOpen("login")}
                                >
                                    Sign In
                                </Button>
                                <Button
                                    size="sm"
                                    onClick={() => onAuthOpen("register")}
                                >
                                    Get Started
                                </Button>
                            </>
                        )}
                    </Group>
                </Group>
            </Container>
        </Box>
    );
});

export default HomeNavbar;
