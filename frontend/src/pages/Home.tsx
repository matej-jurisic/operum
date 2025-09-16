import { Group, Stack, Title, useMantineTheme } from "@mantine/core";

import Header from "../components/Header";
import TrackerList from "../components/TrackerList";

export default function Home() {
    const theme = useMantineTheme();

    return (
        <>
            <Stack gap="md" align="stretch">
                <Group justify="space-between">
                    <Title c={theme.primaryColor} order={2}>
                        {"Operum"}
                    </Title>
                    <Header />
                </Group>
                <TrackerList />
            </Stack>
        </>
    );
}
