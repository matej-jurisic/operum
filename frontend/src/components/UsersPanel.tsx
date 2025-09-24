import {
    ActionIcon,
    Avatar,
    Badge,
    Group,
    Paper,
    ScrollArea,
    Skeleton,
    Stack,
    Table,
    Text,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import { IoMdMail, IoMdPerson } from "react-icons/io";
import { MdMail } from "react-icons/md";
import api from "../api/api";
import { ApplicationUserDto } from "../model/ApplicationUserDto";
import globalStore from "../stores/GlobalStore";
import ConfirmationDialog from "./ConfirmationDialog";
import UserRolesFormDialog from "./UserRolesFormDialog";
import { UsersCards } from "./UsersCards";

const GetAllUsers = async () => {
    const users = await api.get("/users/all");
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

    // Check if mobile
    const isMobile = useMediaQuery("(max-width: 768px)");

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

    const handleEditRoles = (user: ApplicationUserDto) => {
        setSelectedUser(user);
        setOpenDialogType(OpenDialogType.EditRoles);
    };

    const handleConfirmMail = (user: ApplicationUserDto) => {
        setSelectedUser(user);
        setOpenDialogType(OpenDialogType.ConfirmMail);
    };

    return (
        <>
            <Skeleton visible={loading} h={"100%"}>
                <Stack gap="md" h={"100%"}>
                    <ScrollArea flex={1}>
                        {users.length > 0 && !loading ? (
                            isMobile ? (
                                <UsersCards
                                    users={users}
                                    onEditRoles={handleEditRoles}
                                    onConfirmMail={handleConfirmMail}
                                    currentUserId={globalStore.currentUser?.id}
                                />
                            ) : (
                                <Table.ScrollContainer minWidth={0}>
                                    <Table
                                        striped
                                        highlightOnHover
                                        withColumnBorders
                                        withTableBorder
                                        verticalSpacing={"sm"}
                                    >
                                        <Table.Thead>
                                            <Table.Tr>
                                                <Table.Th>User</Table.Th>
                                                <Table.Th>Email</Table.Th>
                                                <Table.Th>Roles</Table.Th>
                                                <Table.Th>
                                                    Mail Confirmed
                                                </Table.Th>
                                                <Table.Th>Actions</Table.Th>
                                            </Table.Tr>
                                        </Table.Thead>
                                        <Table.Tbody>
                                            {users.map((user) => (
                                                <Table.Tr key={user.id}>
                                                    <Table.Td>
                                                        <Group
                                                            gap="sm"
                                                            wrap="nowrap"
                                                        >
                                                            <Avatar
                                                                size="sm"
                                                                radius="xl"
                                                                color="indigo"
                                                            >
                                                                <IoMdPerson
                                                                    size={16}
                                                                />
                                                            </Avatar>
                                                            <div>
                                                                <Text fw={500}>
                                                                    {
                                                                        user.userName
                                                                    }
                                                                </Text>
                                                                <Text
                                                                    size="xs"
                                                                    c="dimmed"
                                                                >
                                                                    ID:{" "}
                                                                    {user.id}
                                                                </Text>
                                                            </div>
                                                        </Group>
                                                    </Table.Td>
                                                    <Table.Td>
                                                        <Group
                                                            gap="xs"
                                                            wrap="nowrap"
                                                        >
                                                            <IoMdMail
                                                                size={14}
                                                            />
                                                            <Text size="sm">
                                                                {user.email}
                                                            </Text>
                                                        </Group>
                                                    </Table.Td>
                                                    <Table.Td>
                                                        <Group
                                                            gap="xs"
                                                            wrap="nowrap"
                                                        >
                                                            {user.roles.length >
                                                            0 ? (
                                                                user.roles.map(
                                                                    (
                                                                        role,
                                                                        index
                                                                    ) => (
                                                                        <Badge
                                                                            key={
                                                                                index
                                                                            }
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
                                                                            {
                                                                                role
                                                                            }
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
                                                            {String(
                                                                user.mailConfirmed
                                                            )}
                                                        </Badge>
                                                    </Table.Td>
                                                    <Table.Td>
                                                        <Group
                                                            gap="xs"
                                                            wrap="nowrap"
                                                        >
                                                            {globalStore.currentUser !==
                                                                undefined &&
                                                                user.id !==
                                                                    globalStore
                                                                        .currentUser
                                                                        .id && (
                                                                    <ActionIcon
                                                                        variant="light"
                                                                        color="blue"
                                                                        onClick={() =>
                                                                            handleEditRoles(
                                                                                user
                                                                            )
                                                                        }
                                                                    >
                                                                        <IoMdPerson
                                                                            size={
                                                                                14
                                                                            }
                                                                        />
                                                                    </ActionIcon>
                                                                )}
                                                            {!user.mailConfirmed && (
                                                                <ActionIcon
                                                                    variant="light"
                                                                    color="blue"
                                                                    onClick={() =>
                                                                        handleConfirmMail(
                                                                            user
                                                                        )
                                                                    }
                                                                >
                                                                    <MdMail
                                                                        size={
                                                                            14
                                                                        }
                                                                    />
                                                                </ActionIcon>
                                                            )}
                                                        </Group>
                                                    </Table.Td>
                                                </Table.Tr>
                                            ))}
                                        </Table.Tbody>
                                    </Table>
                                </Table.ScrollContainer>
                            )
                        ) : loading ? (
                            <></>
                        ) : (
                            <Paper withBorder p="xl" radius="md">
                                <Stack gap="md" align="center">
                                    <Text size="lg" fw={500} c="dimmed">
                                        No Users Available
                                    </Text>
                                    <Text ta="center" c="dimmed">
                                        Users will appear here when they are
                                        registered.
                                    </Text>
                                </Stack>
                            </Paper>
                        )}
                    </ScrollArea>
                </Stack>
            </Skeleton>

            {/* Dialogs */}
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
