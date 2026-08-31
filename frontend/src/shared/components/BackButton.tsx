import { Button } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { LuLayoutDashboard } from "react-icons/lu";
import { useNavigate } from "react-router-dom";
import { readDefaultPage } from "../constants/defaultPage";

interface Props {
    color?: string;
}

/**
 * Jumps into the app proper from the public marketing chrome (Header / HomeNavbar,
 * the only place this still renders). Lands on the user's chosen default page.
 */
export default function BackButton(props: Props) {
    const navigate = useNavigate();

    // Only ever an icon, so on a phone it drops the padding a label would have needed
    // and leaves that width to the rest of the header's row.
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
