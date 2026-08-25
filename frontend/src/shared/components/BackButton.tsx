import { Button } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LuLayoutDashboard } from "react-icons/lu";
import { PiDatabaseBold } from "react-icons/pi";
import { useLocation, useNavigate } from "react-router-dom";

interface Props {
    color?: string;
}

export default function BackButton(props: Props) {
    const navigate = useNavigate();
    const location = useLocation();

    const onTrackers = location.pathname === "/trackers";

    const targetRoute = onTrackers ? "/dashboard" : "/trackers";
    const Icon = onTrackers ? LuLayoutDashboard : PiDatabaseBold;

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
