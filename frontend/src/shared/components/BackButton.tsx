import { Button, Menu } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LuLayoutDashboard } from "react-icons/lu";
import { TbDatabase, TbLayoutGrid } from "react-icons/tb";
import { useLocation, useNavigate } from "react-router-dom";

interface Props {
    color?: string;
}

// The app's three top-level, signed-in destinations. Order here is the order they're
// offered in the dropdown below.
const DESTINATIONS = [
    {
        path: "/dashboard",
        label: "Dashboards",
        // Tabler's dashboard icon: the rest of the app already uses it for this.
        Icon: LuLayoutDashboard,
        match: (pathname: string) => pathname.startsWith("/dashboard"),
    },
    {
        path: "/trackers",
        label: "Trackers",
        Icon: TbDatabase,
        match: (pathname: string) => pathname.startsWith("/trackers"),
    },
    {
        path: "/widgets",
        label: "Widget Library",
        Icon: TbLayoutGrid,
        match: (pathname: string) => pathname.startsWith("/widgets"),
    },
];

/**
 * The one button that switches between the app's top-level destinations. With three of
 * them there's no single "other page" to point at any more (the way there was when this
 * was a plain Dashboard <-> Trackers toggle), so it reads as "you are here" — its icon is
 * the current section's — and opens a dropdown offering the other two.
 */
export default function BackButton(props: Props) {
    const navigate = useNavigate();
    const location = useLocation();

    const current =
        DESTINATIONS.find((d) => d.match(location.pathname)) ?? DESTINATIONS[0];

    // Only ever an icon, so on a phone it drops the padding a label would have needed
    // and leaves that width to the rest of the header's row.
    const isMobile = useMediaQuery("(max-width: 48em)");

    return (
        <Menu position="bottom-end">
            <Menu.Target>
                <Button
                    variant="outline"
                    px={isMobile ? "xs" : undefined}
                    color={props.color}
                >
                    <current.Icon size={16} />
                </Button>
            </Menu.Target>
            <Menu.Dropdown>
                {DESTINATIONS.map((destination) => (
                    <Menu.Item
                        key={destination.path}
                        leftSection={<destination.Icon size={16} />}
                        disabled={destination.path === current.path}
                        onClick={() => navigate(destination.path)}
                    >
                        {destination.label}
                    </Menu.Item>
                ))}
            </Menu.Dropdown>
        </Menu>
    );
}
