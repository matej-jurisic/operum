import { Button, Group, Stack, Text } from "@mantine/core";
import { Dispatch, SetStateAction, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api/api";
import { TrackerDto } from "../model/TrackerDto";
import CreateTrackerDialog from "./CreateTrackerDialog";
import ViewFieldsDialog from "./ViewFieldsDialog";

const GetTrackers = async (
    setTrackers: Dispatch<SetStateAction<TrackerDto[]>>
) => {
    const response = await api.get("/trackers");
    setTrackers(response.data.data);
};

enum OpenDialogType {
    ViewFields,
    CreateTracker,
}

export default function TrackerList() {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [selectedTracker, setSelectedTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const navigate = useNavigate();

    useEffect(() => {
        GetTrackers(setTrackers);
    }, []);

    return (
        <>
            <Stack
                style={{
                    width: "100%",
                    flexGrow: 1,
                }}
            >
                <Group justify="flex-start" style={{ width: "100%" }}>
                    <Button
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateTracker)
                        }
                    >
                        Create Tracker
                    </Button>
                </Group>
                {trackers.map((x) => (
                    <Group
                        key={x.id}
                        style={{
                            padding: "2rem",
                            border: "1px solid #ddd",
                            borderRadius: "8px",
                            justifyContent: "space-between",
                        }}
                    >
                        <Stack>
                            <Text size="xl">{x.name}</Text>
                            <Group>
                                <Text c="#777" size="md">
                                    {x.description}
                                </Text>
                            </Group>
                        </Stack>
                        <Group align="flex-start" style={{ height: "100%" }}>
                            <Button
                                variant="outline"
                                onClick={() => navigate(`/trackers/${x.id}`)}
                            >
                                Open
                            </Button>
                        </Group>
                    </Group>
                ))}
            </Stack>
            {selectedTracker &&
                openDialogType === OpenDialogType.ViewFields && (
                    <ViewFieldsDialog
                        tracker={selectedTracker}
                        onClose={() => setSelectedTracker(undefined)}
                    />
                )}
            {openDialogType === OpenDialogType.CreateTracker && (
                <CreateTrackerDialog
                    onCreate={() => GetTrackers(setTrackers)}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
        </>
    );
}
