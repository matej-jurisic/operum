import { Button, Container, Group, Stack, Tabs, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import { IoMdReturnLeft } from "react-icons/io";
import { useNavigate, useParams } from "react-router-dom";
import api from "../api/api";
import AnalyiticsList from "../components/AnalyticsList";
import CreateEntryDialog from "../components/CreateEntryDialog";
import { CreateFieldDialog } from "../components/CreateFieldDialog";
import EntriesList from "../components/EntriesList";
import FieldsList from "../components/FieldsList";
import { EntryDto } from "../model/EntryDto";
import { TrackerDto } from "../model/TrackerDto";

const GetTracker = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}`);
    return response.data.data;
};

const GetEntries = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/entries`);
    return response.data.data;
};

enum OpenDialogType {
    CreateEntry,
    AddField,
}

export default function Tracker() {
    const { trackerId } = useParams();
    const navigate = useNavigate();

    const [tracker, setTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [entries, setEntries] = useState<EntryDto[]>([]);

    useEffect(() => {
        const fetchData = async () => {
            if (trackerId) {
                setEntries(await GetEntries(trackerId));
                setTracker(await GetTracker(trackerId));
            }
        };
        fetchData();
    }, [trackerId]);

    if (!tracker) return null;

    return (
        <>
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
                    variant="outline"
                    color={tracker.color}
                    defaultValue="entries"
                >
                    <Tabs.List>
                        <Tabs.Tab value="entries">Entries</Tabs.Tab>
                        <Tabs.Tab value="fields">{`Fields (${tracker.fields.length})`}</Tabs.Tab>
                        <Tabs.Tab value="analytics">Analytics</Tabs.Tab>
                    </Tabs.List>

                    <Container
                        p={"xl"}
                        fluid
                        style={{
                            border: "1px solid var(--mantine-color-gray-3)",
                            borderRadius: 0,
                            borderTop: "none",
                        }}
                    >
                        <Tabs.Panel value="entries">
                            <EntriesList
                                tracker={tracker}
                                entries={entries}
                                refreshEntries={async () => {
                                    setEntries(await GetEntries(tracker.id));
                                }}
                            />
                        </Tabs.Panel>
                        <Tabs.Panel value="fields">
                            <FieldsList
                                tracker={tracker}
                                refreshTracker={async () => {
                                    setTracker(await GetTracker(tracker.id));
                                }}
                            />
                        </Tabs.Panel>
                        <Tabs.Panel value="analytics">
                            <AnalyiticsList tracker={tracker} />
                        </Tabs.Panel>
                    </Container>
                </Tabs>
            </Stack>

            {openDialogType === OpenDialogType.CreateEntry && (
                <CreateEntryDialog
                    tracker={tracker}
                    onClose={() => setOpenDialogType(undefined)}
                    onEntryCreated={async () =>
                        setEntries(await GetEntries(tracker.id))
                    }
                />
            )}
            {openDialogType === OpenDialogType.AddField && (
                <CreateFieldDialog
                    tracker={tracker}
                    onClose={() => setOpenDialogType(undefined)}
                    onFieldAdded={async () =>
                        setTracker(await GetTracker(tracker.id))
                    }
                />
            )}
        </>
    );
}
