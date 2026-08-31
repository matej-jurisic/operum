import { Button } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LuLayoutDashboard } from "react-icons/lu";
import { TbDatabase } from "react-icons/tb";
import { useLocation, useNavigate } from "react-router-dom";

interface Props {
    color?: string;
}

/**
 * Toggles between the app's two top-level signed-in destinations, Dashboards and Trackers.
 * It shows the icon of the place it would take you to, not the one you're on, the way a
 * back button points away from the current page. (The Widget Library used to be a third
 * destination here; it's now reached from the board menu — see WidgetLibraryModal.)
 */
export default function BackButton(props: Props) {
    const navigate = useNavigate();
    const location = useLocation();

    // From a dashboard the button crosses to Trackers; so does a single tracker's own
    // pages (/trackers/:trackerId...), which point back up to the Trackers list. From
    // anywhere else (the Trackers list itself, Profile, the admin panel) it heads to
    // Dashboards.
    const onDashboard = location.pathname.startsWith("/dashboard");
    const onTracker = /^\/trackers\/.+/.test(location.pathname);
    const target =
        onDashboard || onTracker
            ? { path: "/trackers", label: "Trackers", Icon: TbDatabase }
            : { path: "/dashboard", label: "Dashboards", Icon: LuLayoutDashboard };

    // Only ever an icon, so on a phone it drops the padding a label would have needed
    // and leaves that width to the rest of the header's row.
    const isMobile = useMediaQuery("(max-width: 48em)");

    return (
        <Button
            variant="outline"
            px={isMobile ? "xs" : undefined}
            color={props.color}
            aria-label={`Go to ${target.label}`}
            onClick={() => navigate(target.path)}
        >
            <target.Icon size={16} />
        </Button>
    );
}
