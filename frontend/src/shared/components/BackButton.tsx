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

    const onTrackersList = location.pathname === "/trackers";

    return (
        <Button
            variant="outline"
            color={props.color}
            onClick={() =>
                navigate(onTrackersList ? "/dashboard" : "/trackers")
            }
        >
            {onTrackersList ? (
                <LuLayoutDashboard size={16} />
            ) : (
                <PiDatabaseBold size={16} />
            )}
        </Button>
    );
}
