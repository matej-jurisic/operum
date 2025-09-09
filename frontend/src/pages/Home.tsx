import {
    ActionIcon,
    Button,
    Card,
    Group,
    Stack,
    Text,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { Dispatch, SetStateAction, useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import { useNavigate } from "react-router-dom";
import api from "../api/api";
import ConfirmationDialog from "../components/ConfirmationDialog";

import Header from "../components/Header";
import TrackerFormDialog from "../components/TrackerFormDialog";
import { TrackerDto } from "../model/TrackerDto";

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
    CreateTracker,
    DeleteTracker,
    UpdateTracker,
}

export default function Home() {
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
            <Stack gap="md" align="stretch">
                <Group justify="space-between">
                    <Title c={theme.primaryColor} order={2} mb="md">
                        {"Operum"}
                    </Title>
                    <Header />
                </Group>
                <Group>
                    <Button
                        leftSection={<FiPlus size={18} />}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateTracker)
                        }
                    >
                        Create Tracker
                    </Button>
                </Group>
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
                                cursor: "pointer",
                            }}
                            onClick={() => navigate(`/trackers/${x.id}`)}
                        >
                            <Group justify="space-between" align="center">
                                <Stack gap={"sm"}>
                                    <Title
                                        order={4}
                                        lineClamp={1}
                                        className="wrapped-text"
                                    >
                                        {x.name}
                                    </Title>
                                    <Text
                                        c="dimmed"
                                        size="sm"
                                        className="wrapped-text"
                                        lineClamp={2}
                                    >
                                        {x.description || "No description"}
                                    </Text>
                                </Stack>

                                <Group
                                    gap="xs"
                                    justify="flex-end"
                                    style={{ flexGrow: 1 }}
                                >
                                    <ActionIcon
                                        size={"lg"}
                                        variant="outline"
                                        color="green"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            setSelectedTracker(x);
                                            setOpenDialogType(
                                                OpenDialogType.UpdateTracker
                                            );
                                        }}
                                    >
                                        <MdEdit size={18} />
                                    </ActionIcon>
                                    <ActionIcon
                                        size={"lg"}
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
                                    </ActionIcon>
                                </Group>
                            </Group>
                        </Card>
                    );
                })}
            </Stack>

            {/* Dialogs */}
            {openDialogType === OpenDialogType.CreateTracker && (
                <TrackerFormDialog
                    onConfirm={() => GetTrackers(setTrackers)}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                />
            )}

            {openDialogType === OpenDialogType.UpdateTracker &&
                selectedTracker && (
                    <TrackerFormDialog
                        onConfirm={() => GetTrackers(setTrackers)}
                        onClose={() => {
                            setOpenDialogType(undefined);
                        }}
                        initialValues={selectedTracker}
                        trackerId={selectedTracker.id}
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
