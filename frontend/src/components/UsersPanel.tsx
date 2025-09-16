import {
    ActionIcon,
    Avatar,
    Badge,
    Card,
    Group,
    Skeleton,
    Table,
    Text,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { IoMdMail, IoMdPerson } from "react-icons/io";
import { MdMail } from "react-icons/md";
import api from "../api/api";
import { ApplicationUserDto } from "../model/ApplicationUserDto";
import globalStore from "../stores/GlobalStore";
import ConfirmationDialog from "./ConfirmationDialog";
import UserRolesFormDialog from "./UserRolesFormDialog";

const GetAllUsers = async () => {
    const users = await api.get("/users");
    return users.data.data;
};

const ConfirmEmail = async (userId: string) => {
    await api.post(`/users/${userId}/confirm-email`);
};

enum OpenDialogType {
    EditRoles,
    ConfirmMail,
}

export default function UsersPanel() {
    const [users, setUsers] = useState<ApplicationUserDto[]>([]);
    const [selectedUser, setSelectedUser] = useState<ApplicationUserDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [loading, setLoading] = useState<boolean>(true);

    useEffect(() => {
        const GetData = async () => {
            setLoading(true);
            const users = await GetAllUsers();
            setUsers(users);
            setLoading(false);
        };

        GetData();
    }, []);

    const getRoleColor = (role: string) => {
        const colors: { [key: string]: string } = {
            Admin: "red",
            Manager: "blue",
            User: "green",
            Moderator: "orange",
            Guest: "gray",
        };
        return colors[role] || "indigo";
    };

    return (
        <>
            <Skeleton visible={loading}>
                <Card shadow="sm" padding="lg" radius="md" withBorder>
                    <Group justify="space-between" mb="md">
                        <Text size="lg" fw={600}>
                            User Management ({users.length} users)
                        </Text>
                    </Group>
                    <Table.ScrollContainer minWidth={0}>
                        <Table striped highlightOnHover withTableBorder>
                            <Table.Thead>
                                <Table.Tr>
                                    <Table.Th miw={200}>User</Table.Th>
                                    <Table.Th miw={150}>Email</Table.Th>
                                    <Table.Th miw={120}>Roles</Table.Th>
                                    <Table.Th>Mail Confirmed</Table.Th>
                                    <Table.Th>Actions</Table.Th>
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {users.length === 0 ? (
                                    <Table.Tr>
                                        <Table.Td
                                            colSpan={4}
                                            style={{
                                                textAlign: "center",
                                                padding: "2rem",
                                            }}
                                        >
                                            <Text c="dimmed">
                                                No users found
                                            </Text>
                                        </Table.Td>
                                    </Table.Tr>
                                ) : (
                                    users.map((user) => (
                                        <Table.Tr key={user.id}>
                                            <Table.Td>
                                                <Group gap="sm" wrap="nowrap">
                                                    <Avatar
                                                        size="sm"
                                                        radius="xl"
                                                        color="indigo"
                                                    >
                                                        <IoMdPerson size={16} />
                                                    </Avatar>
                                                    <div>
                                                        <Text fw={500}>
                                                            {user.userName}
                                                        </Text>
                                                        <Text
                                                            size="xs"
                                                            c="dimmed"
                                                        >
                                                            ID: {user.id}
                                                        </Text>
                                                    </div>
                                                </Group>
                                            </Table.Td>
                                            <Table.Td>
                                                <Group gap="xs" wrap="nowrap">
                                                    <IoMdMail size={14} />
                                                    <Text size="sm">
                                                        {user.email}
                                                    </Text>
                                                </Group>
                                            </Table.Td>
                                            <Table.Td>
                                                <Group gap="xs" wrap="nowrap">
                                                    {user.roles.length > 0 ? (
                                                        user.roles.map(
                                                            (role, index) => (
                                                                <Badge
                                                                    key={index}
                                                                    color={getRoleColor(
                                                                        role
                                                                    )}
                                                                    variant="light"
                                                                    size="sm"
                                                                    style={{
                                                                        minWidth:
                                                                            "max-content",
                                                                    }}
                                                                >
                                                                    {role}
                                                                </Badge>
                                                            )
                                                        )
                                                    ) : (
                                                        <Badge
                                                            color="gray"
                                                            variant="light"
                                                            size="sm"
                                                        >
                                                            No roles
                                                        </Badge>
                                                    )}
                                                </Group>
                                            </Table.Td>
                                            <Table.Td>
                                                <Badge
                                                    color={
                                                        user.mailConfirmed
                                                            ? "green"
                                                            : "red"
                                                    }
                                                    variant="outline"
                                                >
                                                    {String(user.mailConfirmed)}
                                                </Badge>
                                            </Table.Td>
                                            <Table.Td>
                                                <Group gap="xs" wrap="nowrap">
                                                    {globalStore.currentUser !==
                                                        undefined &&
                                                        user.id !==
                                                            globalStore
                                                                .currentUser
                                                                .id && (
                                                            <ActionIcon
                                                                variant="light"
                                                                color="blue"
                                                                size="sm"
                                                                onClick={() => {
                                                                    setSelectedUser(
                                                                        user
                                                                    );
                                                                    setOpenDialogType(
                                                                        OpenDialogType.EditRoles
                                                                    );
                                                                }}
                                                            >
                                                                <IoMdPerson
                                                                    size={14}
                                                                />
                                                            </ActionIcon>
                                                        )}
                                                    {!user.mailConfirmed && (
                                                        <ActionIcon
                                                            variant="light"
                                                            color="blue"
                                                            size="sm"
                                                            onClick={() => {
                                                                setSelectedUser(
                                                                    user
                                                                );
                                                                setOpenDialogType(
                                                                    OpenDialogType.ConfirmMail
                                                                );
                                                            }}
                                                        >
                                                            <MdMail size={14} />
                                                        </ActionIcon>
                                                    )}
                                                </Group>
                                            </Table.Td>
                                        </Table.Tr>
                                    ))
                                )}
                            </Table.Tbody>
                        </Table>
                    </Table.ScrollContainer>
                </Card>
            </Skeleton>
            {openDialogType === OpenDialogType.EditRoles && selectedUser && (
                <UserRolesFormDialog
                    user={selectedUser}
                    onClose={() => {
                        setSelectedUser(undefined);
                        setOpenDialogType(undefined);
                    }}
                    onRoleChange={async () => {
                        setUsers(await GetAllUsers());
                    }}
                />
            )}
            {openDialogType === OpenDialogType.ConfirmMail && selectedUser && (
                <ConfirmationDialog
                    isOpen
                    onClose={() => {
                        setSelectedUser(undefined);
                        setOpenDialogType(undefined);
                    }}
                    onConfirm={async () => {
                        await ConfirmEmail(selectedUser.id);
                        setUsers(await GetAllUsers());
                        setSelectedUser(undefined);
                        setOpenDialogType(undefined);
                    }}
                    title="Mail Confirmation"
                    severity="info"
                    message={`Set mail as confirmed for user ${selectedUser.userName}?`}
                />
            )}
        </>
    );
}
