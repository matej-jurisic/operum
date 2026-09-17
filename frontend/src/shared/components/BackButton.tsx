import { Button } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LuLayoutDashboard } from "react-icons/lu";
import { useNavigate } from "react-router-dom";
import { readDefaultPage } from "../constants/defaultPage";

interface Props {
    color?: string;
}

export default function BackButton(props: Props) {
    const navigate = useNavigate();

    const isMobile = useMediaQuery("(max-width: 48em)");

    return (
        <Button
            variant="outline"
            px={isMobile ? "xs" : undefined}
            color={props.color}
            aria-label="Go to app"
            onClick={() => navigate(readDefaultPage())}
        >
            <LuLayoutDashboard size={16} />
        </Button>
    );
}
