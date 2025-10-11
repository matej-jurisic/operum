import { Button } from "@mantine/core";
import { FiDatabase } from "react-icons/fi";
import { IoHome } from "react-icons/io5";
import { useLocation, useNavigate } from "react-router-dom";

interface Props {
    color?: string;
}

export default function BackButton(props: Props) {
    const navigate = useNavigate();
    const location = useLocation();

    return (
        <Button
            variant="outline"
            color={props.color}
            onClick={() =>
                location.pathname === "/trackers"
                    ? navigate("/home")
                    : navigate("/trackers")
            }
        >
            {location.pathname === "/trackers" ? (
                <IoHome size={16} />
            ) : (
                <FiDatabase size={16} />
            )}
        </Button>
    );
}
