import { Button, Container, Group, Stack, Tabs, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import { IoMdReturnLeft } from "react-icons/io";
import { useNavigate, useParams } from "react-router-dom";
import api from "../api/api";
import AnalyiticsList from "../components/AnalyticsList";
import EntriesList from "../components/EntriesList";
import { FieldFormDialog } from "../components/FieldFormDialog";
import FieldsList from "../components/FieldsList";
import { TrackerProvider } from "../context/TrackerContext";
import { TrackerDto } from "../model/TrackerDto";

const GetTracker = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}`);
    return response.data.data;
};

enum OpenDialogType {
    CreateEntry,
    ImportEntries,
    AddField,
}

export default function Tracker() {
    const { trackerId } = useParams();
    const navigate = useNavigate();

    const [tracker, setTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    const [activeTab, setActiveTab] = useState<string | null>("entries");

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
        <TrackerProvider trackerId={tracker.id}>
            <Stack gap="lg">
                <Group align="center" justify="space-between" w="100%">
                    <Title
                        c={tracker.color}
                        className="truncated-text"
                        order={2}
                    >
                        {tracker.name}
                    </Title>
                    <Button
                        onClick={() => navigate("/")}
                        variant="outline"
                        leftSection={<IoMdReturnLeft size={18} />}
                        color={tracker.color}
                    >
                        Back
                    </Button>
                </Group>
                <Tabs
                    variant="default"
                    color={tracker.color}
                    value={activeTab}
                    onChange={setActiveTab}
                    keepMounted={false}
                >
                    <Tabs.List>
                        <Tabs.Tab value="entries">Entries</Tabs.Tab>
                        <Tabs.Tab value="fields">Fields</Tabs.Tab>
                        <Tabs.Tab value="analytics">Analytics</Tabs.Tab>
                    </Tabs.List>

                    <Container px={0} pt={"md"} fluid>
                        <Tabs.Panel value="entries">
                            <EntriesList tracker={tracker} />
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

            {openDialogType === OpenDialogType.AddField && (
                <FieldFormDialog
                    tracker={tracker}
                    onClose={() => setOpenDialogType(undefined)}
                    onFieldSaved={async () =>
                        setTracker(await GetTracker(tracker.id))
                    }
                />
            )}
        </TrackerProvider>
    );
}
