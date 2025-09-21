import { Stack } from "@mantine/core";

import TrackerList from "../components/TrackerList";

export default function Home() {
    return (
        <>
            <Stack gap="md" align="stretch" h={"100%"}>
                <TrackerList />
            </Stack>
        </>
    );
}
