import { Badge, Container, Group, Stack, Tabs, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../api/api";
import AnalyiticsList from "../components/AnalyticsList";
import BackButton from "../components/BackButton";
import EntriesList from "../components/EntriesList";
import FieldsList from "../components/FieldsList";
import Header from "../components/Header";
import SelectView from "../components/SelectView";
import StickyContainer from "../components/StickyContainer";
import ViewsList from "../components/ViewsList";
import { TrackerProvider } from "../context/TrackerContext";
import { TrackerDto } from "../model/TrackerDto";

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
            <Stack gap="lg">
                <Group justify="space-between" gap={"xl"}>
                    <Group align="center">
                        <Title order={2} c={tracker.color} flex={1}>
                            {tracker.name}
                        </Title>
                        {tracker.trackerTypeName && (
                            <Badge variant="light">
                                {tracker.trackerTypeName}
                            </Badge>
                        )}
                    </Group>

                    <Header
                        color={tracker.color}
                        items={[
                            <SelectView />,
                            <BackButton color={tracker.color} />,
                        ]}
                    />
                </Group>

                <Tabs
                    variant="default"
                    color={tracker.color}
                    keepMounted={false}
                    defaultValue={"entries"}
                >
                    <StickyContainer>
                        <Tabs.List>
                            <Tabs.Tab value="entries">Entries</Tabs.Tab>
                            <Tabs.Tab value="fields">Fields</Tabs.Tab>
                            <Tabs.Tab value="views">Views</Tabs.Tab>
                            <Tabs.Tab value="analytics">Analytics</Tabs.Tab>
                        </Tabs.List>
                    </StickyContainer>
                    <Container px={0} pt={"md"} fluid>
                        <Tabs.Panel value="entries">
                            <EntriesList />
                        </Tabs.Panel>
                        <Tabs.Panel value="views">
                            <ViewsList tracker={tracker} />
                        </Tabs.Panel>
                        <Tabs.Panel value="fields">
                            <FieldsList tracker={tracker} />
                        </Tabs.Panel>
                        <Tabs.Panel value="analytics">
                            <AnalyiticsList tracker={tracker} />
                        </Tabs.Panel>
                    </Container>
                </Tabs>
            </Stack>
        </TrackerProvider>
    );
}
