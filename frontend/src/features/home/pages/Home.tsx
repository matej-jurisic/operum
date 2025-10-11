import { Stack } from "@mantine/core";
import Trackers from "../../trackers/components/Trackers";

export default function Home() {
    return (
        <>
            <Stack gap="md" align="stretch" h={"100%"}>
                <Trackers />
            </Stack>
        </>
    );
}
