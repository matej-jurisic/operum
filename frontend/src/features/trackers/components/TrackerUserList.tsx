import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Checkbox,
    Group,
    Modal,
    Paper,
    ScrollArea,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import globalStore from "../../../shared/stores/GlobalStore";
import { trackersController } from "../api/trackersController";
import { useTracker } from "../context/TrackerContext";
import { TrackerCollaboratorDto } from "../types/TrackerCollaboratorDto";
import UserTrackerFormDialog from "./UserTrackerFormDialog";

enum OpenDialogType {
    AddUser,
    RemoveUser,
    EditPermissions,
}

export default function TrackerUserList() {
    const { tracker } = useTracker();
    const isOwner = globalStore.currentUser?.id === tracker.ownerId;
    const [userList, setUserList] = useState<TrackerCollaboratorDto[]>([]);
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [selectedUser, setSelectedUser] = useState<TrackerCollaboratorDto>();
    const [editCanEditData, setEditCanEditData] = useState(false);
    const [editCanEditSchema, setEditCanEditSchema] = useState(false);

    const getData = async () => {
        const response = await trackersController.getTrackerUserList(tracker.id);
        setUserList(response.data);
    };

    useEffect(() => {
        getData();
    }, []);

    const handleOpenEditPermissions = (user: TrackerCollaboratorDto) => {
        setSelectedUser(user);
        setEditCanEditData(user.canEditData);
        setEditCanEditSchema(user.canEditSchema);
        setOpenDialogType(OpenDialogType.EditPermissions);
    };

    const handleSavePermissions = async () => {
        if (!selectedUser) return;
        await trackersController.updateCollaboratorPermissions(tracker.id, {
            username: selectedUser.userName,
            canEditData: editCanEditData,
            canEditSchema: editCanEditSchema,
        });
        await getData();
        setOpenDialogType(undefined);
        setSelectedUser(undefined);
    };

    return (
        <>
            <Stack gap={"md"} h={"100%"}>
                {isOwner && (
                    <Group justify="space-between" w={"100%"}>
                        <Button
                            color={tracker.color}
                            onClick={() => setOpenDialogType(OpenDialogType.AddUser)}
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
                                <Card key={user.id} p={"md"} radius={"md"} withBorder>
                                    <Group justify="space-between" wrap="nowrap">
                                        <Stack gap={4}>
                                            <Title order={4} lineClamp={1} className="wrapped-text">
                                                {user.userName}
                                            </Title>
                                            <Group gap="xs">
                                                <Badge
                                                    size="sm"
                                                    variant="light"
                                                    color={user.canEditData ? "green" : "gray"}
                                                >
                                                    Edit data
                                                </Badge>
                                                <Badge
                                                    size="sm"
                                                    variant="light"
                                                    color={user.canEditSchema ? "blue" : "gray"}
                                                >
                                                    Edit schema
                                                </Badge>
                                            </Group>
                                        </Stack>
                                        {isOwner && (
                                            <Group gap="xs" wrap="nowrap">
                                                <ActionIcon
                                                    variant="outline"
                                                    color="green"
                                                    size="lg"
                                                    onClick={() => handleOpenEditPermissions(user)}
                                                    aria-label={`Edit permissions for ${user.userName}`}
                                                >
                                                    <MdEdit size={16} />
                                                </ActionIcon>
                                                <ActionIcon
                                                    variant="outline"
                                                    color="red"
                                                    size="lg"
                                                    onClick={() => {
                                                        setSelectedUser(user);
                                                        setOpenDialogType(OpenDialogType.RemoveUser);
                                                    }}
                                                    aria-label={`Remove user`}
                                                >
                                                    <MdDelete size={16} />
                                                </ActionIcon>
                                            </Group>
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
                                        Users will appear here when you add them to the tracker.
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
                        await trackersController.removeUserFromTracker(tracker.id, {
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

            {openDialogType === OpenDialogType.EditPermissions && selectedUser && (
                <Modal
                    opened
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedUser(undefined);
                    }}
                    title={`Permissions — ${selectedUser.userName}`}
                    centered
                >
                    <Stack>
                        <Checkbox
                            label="Can edit data (add, edit, delete entries)"
                            checked={editCanEditData}
                            onChange={(e) => setEditCanEditData(e.currentTarget.checked)}
                        />
                        <Checkbox
                            label="Can edit schema (fields, views, analytics, constants)"
                            checked={editCanEditSchema}
                            onChange={(e) => setEditCanEditSchema(e.currentTarget.checked)}
                        />
                        <Button color={tracker.color} onClick={handleSavePermissions}>
                            Save
                        </Button>
                    </Stack>
                </Modal>
            )}
        </>
    );
}
