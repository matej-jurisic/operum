import {
    Button,
    Card,
    Group,
    Menu,
    Stack,
    Text,
    Title,
    useComputedColorScheme,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { Dispatch, SetStateAction, useEffect, useState } from "react";
import { CiLogout, CiSettings, CiUser } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { GoSun } from "react-icons/go";
import { IoMoonOutline } from "react-icons/io5";
import { MdDelete } from "react-icons/md";
import { useNavigate } from "react-router-dom";
import api from "../api/api";
import ConfirmationDialog from "../components/ConfirmationDialog";

import CreateTrackerDialog from "../components/TrackerFormDialog";
import useAuth from "../hooks/useAuth";
import { TrackerDto } from "../model/TrackerDto";
import globalStore from "../stores/GlobalStore";

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
}

export default function Home() {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [selectedTracker, setSelectedTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const navigate = useNavigate();
    const theme = useMantineTheme();
    const auth = useAuth();
    const { colorScheme, setColorScheme } = useMantineColorScheme();
    const computedColorScheme = useComputedColorScheme("light");

    useEffect(() => {
        GetTrackers(setTrackers);
    }, []);

    return (
        <>
            <Stack gap="xl">
                <Group justify="space-between">
                    <Title c="indigo" order={2}>
                        Operum
                    </Title>
                    <Group justify="flex-end">
                        <Menu>
                            <Menu.Target>
                                <Button variant="outline">
                                    <CiUser size={18} />
                                </Button>
                            </Menu.Target>

                            <Menu.Dropdown>
                                <Menu.Item leftSection={<CiUser size={16} />}>
                                    {globalStore.currentUser?.userName ||
                                        "Guest"}
                                </Menu.Item>
                                <Menu.Item
                                    leftSection={<CiSettings size={16} />}
                                    onClick={() => navigate("/admin-panel")}
                                >
                                    Admin panel
                                </Menu.Item>
                                <Menu.Item
                                    leftSection={<CiLogout size={16} />}
                                    onClick={async () => {
                                        await auth.logout();
                                        window.location.reload();
                                    }}
                                >
                                    Logout
                                </Menu.Item>
                            </Menu.Dropdown>
                        </Menu>
                        <Button
                            variant="outline"
                            color={theme.primaryColor}
                            onClick={() => {
                                setColorScheme(
                                    computedColorScheme === "dark"
                                        ? "light"
                                        : "dark"
                                );
                            }}
                        >
                            {colorScheme === "light" ? (
                                <IoMoonOutline size={16} />
                            ) : (
                                <GoSun size={16} />
                            )}
                        </Button>
                    </Group>
                </Group>

                <Stack gap="md" align="stretch">
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
                                            className="truncated-text"
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
