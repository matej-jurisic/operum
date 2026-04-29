import { Button } from "@mantine/core";
import { CiDatabase } from "react-icons/ci";
import { MdDashboard } from "react-icons/md";
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
            onClick={() => navigate(onTrackersList ? "/dashboard" : "/trackers")}
        >
            {onTrackersList ? (
                <MdDashboard size={16} />
            ) : (
                <CiDatabase size={16} />
            )}
        </Button>
    );
}
