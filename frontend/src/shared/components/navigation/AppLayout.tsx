import { AppShell, Overlay, useMantineColorScheme } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { observer } from "mobx-react";
import { useEffect, useState } from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import globalStore from "../../stores/GlobalStore";
import navigationStore from "../../stores/NavigationStore";
import AppSidebar from "./AppSidebar";
import AppSpotlight from "./AppSpotlight";
import NavCreateHost from "./NavCreateHost";

const COLLAPSED_KEY = "operum.sidebarCollapsed";

/**
 * The chrome around every signed-in page: a persistent sidebar (a drawer on
 * mobile) plus the command palette. Public pages (home, legal, confirm-email)
 * render outside this and keep their own bare layout.
 */
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

    // Close the mobile drawer whenever navigation lands somewhere new.
    useEffect(() => {
        navigationStore.closeMobileNav();
    }, [location.pathname]);

    // AppShell's mobile navbar is just a sliding panel -- it locks nothing. Keep
    // the page behind it from scrolling (and swallowing taps) while it is open.
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
            {/*
             * Sits above the header/main (z 100) but below the navbar (see its
             * zIndex below). AppShell's own z-indexes are ~100, so the old value
             * of 199 buried the drawer under the overlay -- it looked dimmed and
             * swallowed every tap.
             */}
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
                    compactFooter={!!isMobile}
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
                <Outlet />
            </AppShell.Main>

            <AppSpotlight />
            <NavCreateHost />
        </AppShell>
    );
});

export default AppLayout;
