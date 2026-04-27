import { ActionIcon, Avatar, Badge, Group, Table, Text } from "@mantine/core";
import { IoMdMail, IoMdPerson } from "react-icons/io";
import { MdMail } from "react-icons/md";
import globalStore from "../../../shared/stores/GlobalStore";
import { UserDto } from "../../auth/types/UserDto";

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

interface Props {
    users: UserDto[];
    handleEditRoles: (user: UserDto) => void;
    handleConfirmMail: (user: UserDto) => void;
}

export default function UsersTable(props: Props) {
    return (
        <>
            <Table
                striped
                highlightOnHover
                withColumnBorders
                withTableBorder
                verticalSpacing={"sm"}
                style={{ backgroundColor: "var(--mantine-color-body)" }}
            >
                <Table.Thead>
                    <Table.Tr>
                        <Table.Th>User</Table.Th>
                        <Table.Th>Email</Table.Th>
                        <Table.Th>Roles</Table.Th>
                        <Table.Th>Mail Confirmed</Table.Th>
                        <Table.Th>Actions</Table.Th>
                    </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                    {props.users.map((user) => (
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
                                        <Text fw={500}>{user.userName}</Text>
                                        <Text size="xs" c="dimmed">
                                            ID: {user.id}
                                        </Text>
                                    </div>
                                </Group>
                            </Table.Td>
                            <Table.Td>
                                <Group gap="xs" wrap="nowrap">
                                    <IoMdMail size={14} />
                                    <Text size="sm">{user.email}</Text>
                                </Group>
                            </Table.Td>
                            <Table.Td>
                                <Group gap="xs" wrap="nowrap">
                                    {user.roles.length > 0 ? (
                                        user.roles.map((role, index) => (
                                            <Badge
                                                key={index}
                                                color={getRoleColor(role)}
                                                variant="light"
                                                size="sm"
                                                style={{
                                                    minWidth: "max-content",
                                                }}
                                            >
                                                {role}
                                            </Badge>
                                        ))
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
                                    color={user.mailConfirmed ? "green" : "red"}
                                    variant="outline"
                                >
                                    {String(user.mailConfirmed)}
                                </Badge>
                            </Table.Td>
                            <Table.Td>
                                <Group gap="xs" wrap="nowrap">
                                    {globalStore.currentUser !== undefined &&
                                        user.id !==
                                            globalStore.currentUser.id && (
                                            <ActionIcon
                                                variant="light"
                                                color="blue"
                                                onClick={() =>
                                                    props.handleEditRoles(user)
                                                }
                                            >
                                                <IoMdPerson size={14} />
                                            </ActionIcon>
                                        )}
                                    {!user.mailConfirmed && (
                                        <ActionIcon
                                            variant="light"
                                            color="blue"
                                            onClick={() =>
                                                props.handleConfirmMail(user)
                                            }
                                        >
                                            <MdMail size={14} />
                                        </ActionIcon>
                                    )}
                                </Group>
                            </Table.Td>
                        </Table.Tr>
                    ))}
                </Table.Tbody>
            </Table>
        </>
    );
}
