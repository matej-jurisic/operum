import { Burger } from "@mantine/core";
import { observer } from "mobx-react";
import navigationStore from "../../stores/NavigationStore";

/**
 * Toggles the sidebar drawer on mobile. It lives inside each page's own header
 * row (see Tracker, DashboardPage, AdminPanel, ProfilePage) so the app needs no
 * separate top bar just to hold it. Renders nothing from `sm` up, where the
 * sidebar is always docked.
 */
const SidebarBurger = observer(() => (
    <Burger
        opened={navigationStore.mobileNavOpen}
        onClick={navigationStore.toggleMobileNav}
        size="sm"
        hiddenFrom="sm"
        aria-label="Toggle navigation"
        style={{ flexShrink: 0 }}
    />
));

export default SidebarBurger;
