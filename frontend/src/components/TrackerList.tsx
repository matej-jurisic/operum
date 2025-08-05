import {
    Button,
    Card,
    Group,
    Stack,
    Text,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { Dispatch, SetStateAction, useEffect, useState } from "react";
import { MdDelete } from "react-icons/md";
import { useNavigate } from "react-router-dom";
import api from "../api/api";
import { TrackerDto } from "../model/TrackerDto";
import ConfirmationDialog from "./ConfirmationDialog";
import CreateTrackerDialog from "./CreateTrackerDialog";
import ViewFieldsDialog from "./ViewFieldsDialog";

const GetTrackers = async (
    setTrackers: Dispatch<SetStateAction<TrackerDto[]>>
) => {
    const response = await api.get("/trackers");
    setTrackers(response.data.data);
};

const DeleteTracker = async (trackerId: string) => {
    await api.delete(`/trackers/${trackerId}`);
};

enum OpenDialogType {
    ViewFields,
    CreateTracker,
    DeleteTracker,
}

export default function TrackerList() {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [selectedTracker, setSelectedTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const navigate = useNavigate();
    const theme = useMantineTheme();

    useEffect(() => {
        GetTrackers(setTrackers);
    }, []);

    return (
        <>
            <Stack gap="xl">
                {/* Top actions */}
                <Group justify="flex-end">
                    <Button
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateTracker)
                        }
                    >
                        + Create Tracker
                    </Button>
                </Group>

                {/* Trackers */}
                <Stack gap="md">
                    {trackers.map((x) => {
                        const isValidColor =
                            x.color !== undefined && x.color in theme.colors;
                        const borderColor =
                            theme.colors[
                                (isValidColor
                                    ? x.color
                                    : "indigo") as keyof typeof theme.colors
                            ][6];

                        return (
                            <Card
                                key={x.id}
                                shadow="sm"
                                padding="lg"
                                radius="md"
                                withBorder
                                style={{
                                    borderLeft: `6px solid ${borderColor}`,
                                    transition: "transform 0.15s ease",
                                    cursor: "pointer",
                                }}
                                onClick={() => navigate(`/trackers/${x.id}`)}
                            >
                                <Group
                                    justify="space-between"
                                    align="flex-start"
                                >
                                    <Stack gap={4} maw="90%">
                                        <Title
                                            order={4}
                                            className="truncated-text"
                                        >
                                            {x.name}
                                        </Title>
                                        <Text
                                            c="dimmed"
                                            size="sm"
                                            style={{
                                                whiteSpace: "normal",
                                                wordBreak: "break-word",
                                            }}
                                        >
                                            {x.description || "No description"}
                                        </Text>
                                    </Stack>

                                    <Group
                                        gap="xs"
                                        justify="flex-end"
                                        style={{ flexGrow: 1 }}
                                    >
                                        <Button
                                            size="xs"
                                            variant="outline"
                                            color="red"
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                setSelectedTracker(x);
                                                setOpenDialogType(
                                                    OpenDialogType.DeleteTracker
                                                );
                                            }}
                                        >
                                            <MdDelete size={18} />
                                        </Button>
                                    </Group>
                                </Group>
                            </Card>
                        );
                    })}
                </Stack>
            </Stack>

            {/* Dialogs */}
            {selectedTracker &&
                openDialogType === OpenDialogType.ViewFields && (
                    <ViewFieldsDialog
                        tracker={selectedTracker}
                        onClose={() => {
                            setSelectedTracker(undefined);
                            setOpenDialogType(undefined);
                        }}
                    />
                )}

            {openDialogType === OpenDialogType.CreateTracker && (
                <CreateTrackerDialog
                    onCreate={() => GetTrackers(setTrackers)}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                />
            )}

            {openDialogType === OpenDialogType.DeleteTracker &&
                selectedTracker && (
                    <ConfirmationDialog
                        isOpen
                        onClose={() => {
                            setSelectedTracker(undefined);
                            setOpenDialogType(undefined);
                        }}
                        onConfirm={async () => {
                            await DeleteTracker(selectedTracker.id);
                            GetTrackers(setTrackers);
                            setSelectedTracker(undefined);
                            setOpenDialogType(undefined);
                        }}
                        message="Are you sure you want to delete the tracker?"
                        severity="important"
                    />
                )}
        </>
    );
}
