import { Button, Group, Stack, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import { CiFolderOn } from "react-icons/ci";
import { IoMdReturnLeft } from "react-icons/io";
import { useNavigate, useParams } from "react-router-dom";
import api from "../api/api";
import CreateEntryDialog from "../components/CreateEntryDialog";
import { CreateFieldDialog } from "../components/CreateFieldDialog";
import EntriesList from "../components/EntriesList";
import ViewFieldsDialog from "../components/ViewFieldsDialog";
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
    ViewFields,
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
                    <Button
                        onClick={() => navigate("/")}
                        variant="outline"
                        leftSection={<IoMdReturnLeft size={18} />}
                        color={tracker.color}
                    >
                        Back
                    </Button>

                    <Title
                        c={tracker.color}
                        className="truncated-text"
                        ta="right"
                        order={2}
                    >
                        {tracker.name}
                    </Title>
                </Group>
                <Group justify="flex-end">
                    {tracker.fields.length > 0 && (
                        <Button
                            color={tracker.color}
                            onClick={() =>
                                setOpenDialogType(OpenDialogType.CreateEntry)
                            }
                        >
                            Create Entry
                        </Button>
                    )}
                    <Button
                        color={tracker.color}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.AddField)
                        }
                    >
                        Create Field
                    </Button>
                    <Button
                        color={tracker.color}
                        variant="outline"
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.ViewFields)
                        }
                        leftSection={<CiFolderOn size={18} />}
                    >
                        {`Fields: ${tracker.fields.length}`}
                    </Button>
                </Group>

                <EntriesList
                    tracker={tracker}
                    entries={entries}
                    refreshEntries={async () => {
                        setEntries(await GetEntries(tracker.id));
                    }}
                />
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
            {openDialogType === OpenDialogType.ViewFields && (
                <ViewFieldsDialog
                    tracker={tracker}
                    onClose={async (withReload: boolean) => {
                        setOpenDialogType(undefined);
                        if (withReload) {
                            setEntries(await GetEntries(tracker.id));
                        }
                    }}
                />
            )}
        </>
    );
}
