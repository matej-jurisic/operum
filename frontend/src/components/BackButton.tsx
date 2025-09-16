import { Button } from "@mantine/core";
import { IoHome } from "react-icons/io5";
import { useNavigate } from "react-router-dom";

interface Props {
    color?: string;
    homePath?: string;
}

export default function BackButton(props: Props) {
    const navigate = useNavigate();
    return (
        <Button
            variant="outline"
            color={props.color}
            onClick={() => navigate(props.homePath || "/home")}
        >
            <IoHome size={16} />
        </Button>
    );
}
