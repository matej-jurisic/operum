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

    const targetRoute = onTrackers ? "/dashboard" : "/trackers";
    const Icon = onTrackers ? LuLayoutDashboard : PiDatabaseBold;

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
