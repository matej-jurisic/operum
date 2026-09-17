import { Button, Stack, Text, ThemeIcon, useMantineTheme } from "@mantine/core";
import { observer } from "mobx-react";
import { TbError404 } from "react-icons/tb";
import { Link, Navigate } from "react-router-dom";
import {
    FALLBACK_PAGE,
    readDefaultPage,
    writeDefaultPage,
} from "../constants/defaultPage";
import globalStore from "../stores/GlobalStore";

interface Props {
    /** If this matches the saved default page, that choice is cleared so it doesn't dead-end future visits. */
    path?: string;
}

/** Shown for an unknown URL, or a tracker or board that is gone or not shared with the user. */
const NotFound = observer(function NotFound(props: Props) {
    const theme = useMantineTheme();

    if (props.path && readDefaultPage() === props.path) {
        writeDefaultPage(null);
        return <Navigate to={FALLBACK_PAGE} replace />;
    }

    const home = globalStore.currentUser ? readDefaultPage() : "/home";

    return (
        <Stack align="center" gap="md" py={80}>
            <ThemeIcon
                size={72}
                radius="xl"
                variant="light"
                color={theme.primaryColor}
            >
                <TbError404 size={36} />
            </ThemeIcon>
            <Text fw={700} size="xl">
                Page not found
            </Text>
            <Text c="dimmed" ta="center">
                It may have been deleted, or it isn't shared with you.
            </Text>
            <Button component={Link} to={home}>
                Go home
            </Button>
        </Stack>
    );
});

export default NotFound;
