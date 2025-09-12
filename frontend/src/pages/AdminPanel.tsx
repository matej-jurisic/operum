import {
    ActionIcon,
    Avatar,
    Badge,
    Button,
    Card,
    Group,
    Stack,
    Table,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { IoMdMail, IoMdPerson, IoMdReturnLeft } from "react-icons/io";
import { useNavigate } from "react-router-dom";
import api from "../api/api";
import Header from "../components/Header";
import UserRolesFormDialog from "../components/UserRolesFormDialog";
import { ApplicationUserDto } from "../model/ApplicationUserDto";
import globalStore from "../stores/GlobalStore";

const GetAllUsers = async () => {
    const users = await api.get("/users");
    return users.data.data;
};

enum OpenDialogType {
    EditRoles,
}

export default function AdminPanel() {
    const navigate = useNavigate();
    const [users, setUsers] = useState<ApplicationUserDto[]>([]);
    const [selectedUser, setSelectedUser] = useState<ApplicationUserDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    useEffect(() => {
        const GetData = async () => {
            setUsers(await GetAllUsers());
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
            <Stack gap="xl">
                <Group justify="space-between">
                    <Title c={"indigo"} order={2}>
                        Admin Panel
                    </Title>
                    <Header
                        items={[
                            <Button
                                variant="outline"
                                onClick={() => navigate("/home")}
                                leftSection={<IoMdReturnLeft size={16} />}
                            >
                                Back to Home
                            </Button>,
                        ]}
                    />
                </Group>

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
                                                </Group>
                                            </Table.Td>
                                        </Table.Tr>
                                    ))
                                )}
                            </Table.Tbody>
                        </Table>
                    </Table.ScrollContainer>
                </Card>
            </Stack>
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
        </>
    );
}
