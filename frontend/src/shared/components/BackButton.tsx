import { Button } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LuLayoutDashboard } from "react-icons/lu";
import { TbDatabase } from "react-icons/tb";
import { useLocation, useNavigate } from "react-router-dom";

interface Props {
    color?: string;
}

export default function BackButton(props: Props) {
    const navigate = useNavigate();
    const location = useLocation();

    const onTrackers = location.pathname === "/trackers";

    const targetRoute = onTrackers ? "/dashboard" : "/trackers";
    // Tabler's database, not Phosphor's bold one: it is the icon the rest of the app
    // already uses for a tracker, and its stroke matches the other icons in the header
    // instead of sitting a weight heavier than all of them.
    const Icon = onTrackers ? LuLayoutDashboard : TbDatabase;

    // Only ever an icon, so on a phone it drops the padding a label would have needed
    // and leaves that width to the rest of the header's row.
    const isMobile = useMediaQuery("(max-width: 48em)");

    return (
        <Button
            variant="outline"
            px={isMobile ? "xs" : undefined}
            color={props.color}
            onClick={() => navigate(targetRoute)}
        >
            <Icon size={16} />
        </Button>
    );
}
