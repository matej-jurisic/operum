import { AppShell, Overlay, useMantineColorScheme } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { observer } from "mobx-react";
import { useEffect, useState } from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { areNotificationsEnabled } from "../../../features/notifications/config/notificationsFeature";
import globalStore from "../../stores/GlobalStore";
import inboxStore from "../../stores/InboxStore";
import navigationStore from "../../stores/NavigationStore";
import ErrorBoundary from "../ErrorBoundary";
import AppSidebar from "./AppSidebar";
import AppSpotlight from "./AppSpotlight";
import NavCreateHost from "./NavCreateHost";

const COLLAPSED_KEY = "operum.sidebarCollapsed";

/** Signed-in page chrome: sidebar (drawer on mobile) plus the command palette. */
const AppLayout = observer(() => {
    const location = useLocation();
    const { colorScheme } = useMantineColorScheme();
    const isMobile = useMediaQuery("(max-width: 48em)");
    const mobileOpened = navigationStore.mobileNavOpen;
    const [collapsed, setCollapsed] = useState(
        () => localStorage.getItem(COLLAPSED_KEY) === "true",
    );

    useEffect(() => {
        navigationStore.load();
    }, []);

    // Also refresh on focus, so the badge isn't stale after time away.
    useEffect(() => {
        if (!areNotificationsEnabled) return;

        inboxStore.refreshUnreadCount();
        const interval = window.setInterval(
            () => inboxStore.refreshUnreadCount(),
            60_000,
        );
        const onFocus = () => inboxStore.refreshUnreadCount();
        window.addEventListener("focus", onFocus);

        return () => {
            window.clearInterval(interval);
            window.removeEventListener("focus", onFocus);
        };
    }, []);

    useEffect(() => {
        navigationStore.closeMobileNav();
    }, [location.pathname]);

    // AppShell's mobile navbar doesn't lock scroll on its own; do it here.
    useEffect(() => {
        const locked = isMobile && mobileOpened;
        if (!locked) return;
        const previous = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        return () => {
            document.body.style.overflow = previous;
        };
    }, [isMobile, mobileOpened]);

    const toggleCollapsed = () => {
        setCollapsed((prev) => {
            const next = !prev;
            localStorage.setItem(COLLAPSED_KEY, String(next));
            return next;
        });
    };

    if (!globalStore.currentUser) return <Navigate to="/home" replace />;

    const dotPattern =
        colorScheme === "dark"
            ? "radial-gradient(circle, rgba(255,255,255,0.07) 1px, transparent 1px)"
            : "radial-gradient(circle, rgba(0,0,0,0.08) 1px, transparent 1px)";

    return (
        <AppShell
            h="100vh"
            w="100%"
            padding="md"
            transitionDuration={0}
            navbar={{
                width: collapsed ? 68 : 260,
                breakpoint: "sm",
                collapsed: { mobile: !mobileOpened, desktop: false },
            }}
        >
            {/* Must stay below the navbar's zIndex (102) or it buries the drawer. */}
            {isMobile && mobileOpened && (
                <Overlay
                    zIndex={101}
                    color="#000"
                    backgroundOpacity={0.35}
                    onClick={navigationStore.closeMobileNav}
                    hiddenFrom="sm"
                />
            )}

            <AppShell.Navbar zIndex={102}>
                <AppSidebar
                    collapsed={!isMobile && collapsed}
                    showCollapseToggle={!isMobile}
                    showBrand
                    onToggleCollapse={toggleCollapsed}
                    onNavigate={navigationStore.closeMobileNav}
                    onClose={navigationStore.closeMobileNav}
                />
            </AppShell.Navbar>

            <AppShell.Main
                h="100%"
                style={{ backgroundImage: dotPattern, backgroundSize: "28px 28px" }}
            >
                <ErrorBoundary resetKey={location.pathname}>
                    <Outlet />
                </ErrorBoundary>
            </AppShell.Main>

            <AppSpotlight />
            <NavCreateHost />
        </AppShell>
    );
});

export default AppLayout;
