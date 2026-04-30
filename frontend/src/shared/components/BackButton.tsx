import { Button } from "@mantine/core";
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
    const onDashboard = location.pathname.startsWith("/dashboard/");

    const targetRoute = onTrackers || onDashboard ? "/dashboard" : "/trackers";
    const Icon = onTrackers || onDashboard ? LuLayoutDashboard : PiDatabaseBold;

    return (
        <Button
            variant="outline"
            color={props.color}
            onClick={() => navigate(targetRoute)}
        >
            <Icon size={16} />
        </Button>
    );
}
