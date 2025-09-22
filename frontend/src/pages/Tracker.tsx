import { Badge, Container, Group, Stack, Tabs, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../api/api";
import AnalyiticsList from "../components/AnalyticsList";
import EntriesList from "../components/EntriesList";
import FieldsList from "../components/FieldsList";
import Header from "../components/Header";
import SelectView from "../components/SelectView";
import TrackerUserList from "../components/TrackerUserList";
import ViewsList from "../components/ViewsList";
import { TrackerProvider } from "../context/TrackerContext";
import { TrackerDto } from "../model/TrackerDto";
import globalStore from "../stores/GlobalStore";

const GetTracker = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}`);
    return response.data.data;
};

export default function Tracker() {
    const { trackerId } = useParams();
    const [tracker, setTracker] = useState<TrackerDto>();

    useEffect(() => {
        const fetchData = async () => {
            if (trackerId) {
                setTracker(await GetTracker(trackerId));
            }
        };
        fetchData();
    }, [trackerId]);

    if (!tracker) return null;

    return (
        <TrackerProvider initialTracker={tracker}>
            <Stack h="100%" gap={"md"}>
                <Stack justify="space-between" w="100%">
                    <Group justify="space-between">
                        <SelectView />
                        <Header color={tracker.color} />
                    </Group>
                    <Group align="center" flex={1} justify="space-between">
                        <Title order={2} c={tracker.color}>
                            {tracker.name}
                        </Title>
                        {tracker.trackerTypeName && (
                            <Badge variant="light">
                                {tracker.trackerTypeName}
                            </Badge>
                        )}
                        {tracker.ownerId !== globalStore.currentUser?.id && (
                            <Badge variant="light" color={tracker.color}>
                                Owned by: {tracker.ownerName}
                            </Badge>
                        )}
                    </Group>
                </Stack>

                <Stack flex="1" mih={0}>
                    <Tabs
                        variant="default"
                        color={tracker.color}
                        keepMounted={false}
                        defaultValue="entries"
                        h="100%"
                        display={"flex"}
                        style={{ flexDirection: "column" }}
                    >
                        <Tabs.List>
                            <Tabs.Tab value="entries">Entries</Tabs.Tab>
                            <Tabs.Tab value="fields">Fields</Tabs.Tab>
                            <Tabs.Tab value="views">Views</Tabs.Tab>
                            <Tabs.Tab value="analytics">Analytics</Tabs.Tab>
                            {globalStore.currentUser?.id ===
                                tracker.ownerId && (
                                <Tabs.Tab value="users">Users</Tabs.Tab>
                            )}
                        </Tabs.List>

                        <Container
                            fluid
                            flex={1}
                            w="100%"
                            py="md"
                            px={0}
                            mih={0}
                        >
                            <Tabs.Panel value="entries" h="100%">
                                <EntriesList />
                            </Tabs.Panel>
                            <Tabs.Panel value="views" h="100%">
                                <ViewsList tracker={tracker} />
                            </Tabs.Panel>
                            <Tabs.Panel value="fields" h="100%">
                                <FieldsList tracker={tracker} />
                            </Tabs.Panel>
                            <Tabs.Panel value="analytics" h="100%">
                                <AnalyiticsList tracker={tracker} />
                            </Tabs.Panel>
                            <Tabs.Panel value="users" h="100%">
                                <TrackerUserList />
                            </Tabs.Panel>
                        </Container>
                    </Tabs>
                </Stack>
            </Stack>
        </TrackerProvider>
    );
}
