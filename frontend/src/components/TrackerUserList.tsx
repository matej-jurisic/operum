import {
    ActionIcon,
    Button,
    Card,
    Group,
    Paper,
    ScrollArea,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { PublicApplicationUserDto } from "../model/PublicApplicationUserDto";
import ModifyUserTrackerDto from "../model/requests/ModifyUserTrackerDto";
import globalStore from "../stores/GlobalStore";
import ConfirmationDialog from "./ConfirmationDialog";
import UserTrackerFormDialog from "./UserTrackerFormDialog";

enum OpenDialogType {
    AddUser,
    RemoveUser,
}

const GetTrackerUserList = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/users`);
    return response.data.data;
};

const RemoveUserFromTracker = async (
    trackerId: string,
    request: ModifyUserTrackerDto
) => {
    await api.delete(`/trackers/${trackerId}/users`, {
        data: request,
    });
};

export default function TrackerUserList() {
    const { tracker } = useTracker();
    const [userList, setUserList] = useState<PublicApplicationUserDto[]>([]);
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [selectedUser, setSelectedUser] =
        useState<PublicApplicationUserDto>();

    const getData = async () => {
        setUserList(await GetTrackerUserList(tracker.id));
    };

    useEffect(() => {
        getData();
    }, []);

    return (
        <>
            <Stack gap={"md"} h={"100%"}>
                {globalStore.currentUser?.id === tracker.ownerId && (
                    <Group justify="space-between" w={"100%"}>
                        <Button
                            color={tracker.color}
                            onClick={() =>
                                setOpenDialogType(OpenDialogType.AddUser)
                            }
                            variant="outline"
                            leftSection={<FiPlus size={18} />}
                        >
                            Add
                        </Button>
                    </Group>
                )}
                <ScrollArea flex={1} mih={0}>
                    <Stack gap={"md"}>
                        {userList.length > 0 ? (
                            userList.map((user) => (
                                <Card p={"md"} radius={"md"} withBorder>
                                    <Group justify="space-between">
                                        <Title
                                            order={4}
                                            lineClamp={1}
                                            className="wrapped-text"
                                        >
                                            {user.userName}
                                        </Title>
                                        {globalStore.currentUser?.id ===
                                            tracker.ownerId && (
                                            <ActionIcon
                                                variant="outline"
                                                color="red"
                                                size="lg"
                                                onClick={() => {
                                                    setSelectedUser(user);
                                                    setOpenDialogType(
                                                        OpenDialogType.RemoveUser
                                                    );
                                                }}
                                                aria-label={`Remove user`}
                                            >
                                                <MdDelete size={16} />
                                            </ActionIcon>
                                        )}
                                    </Group>
                                </Card>
                            ))
                        ) : (
                            <Paper withBorder p="xl" radius="md">
                                <Stack gap="md" align="center">
                                    <Text size="lg" fw={500} c="dimmed">
                                        No Users Added
                                    </Text>
                                    <Text ta="center" c="dimmed">
                                        Users will appear here when you add them
                                        to the tracker.
                                    </Text>
                                </Stack>
                            </Paper>
                        )}
                    </Stack>
                </ScrollArea>
            </Stack>
            {openDialogType === OpenDialogType.AddUser && (
                <UserTrackerFormDialog
                    onClose={async () => {
                        await getData();
                        setOpenDialogType(undefined);
                    }}
                />
            )}
            {openDialogType === OpenDialogType.RemoveUser && selectedUser && (
                <ConfirmationDialog
                    isOpen
                    onClose={() => {
                        setSelectedUser(undefined);
                        setOpenDialogType(undefined);
                    }}
                    onConfirm={async () => {
                        await RemoveUserFromTracker(tracker.id, {
                            username: selectedUser.userName,
                        });
                        await getData();
                        setOpenDialogType(undefined);
                        setSelectedUser(undefined);
                    }}
                    message={`Remove user ${selectedUser.userName} from tracker?`}
                    severity="warning"
                />
            )}
        </>
    );
}
