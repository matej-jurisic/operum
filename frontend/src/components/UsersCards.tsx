import {
    ActionIcon,
    Avatar,
    Badge,
    Card,
    Group,
    Stack,
    Text,
} from "@mantine/core";
import { IoMdMail, IoMdPerson } from "react-icons/io";
import { MdMail } from "react-icons/md";
import { ApplicationUserDto } from "../model/ApplicationUserDto";

interface UsersCardsProps {
    users: ApplicationUserDto[];
    onEditRoles: (user: ApplicationUserDto) => void;
    onConfirmMail: (user: ApplicationUserDto) => void;
    currentUserId?: string;
}

export function UsersCards({
    users,
    onEditRoles,
    onConfirmMail,
    currentUserId,
}: UsersCardsProps) {
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
        <Stack gap="md" h={"100%"}>
            {users.map((user) => (
                <Card key={user.id} withBorder radius="md" p="md">
                    <Stack gap="sm">
                        {/* User Info */}
                        <Group justify="space-between" align="flex-start">
                            <Group gap="sm" wrap="nowrap" flex={1}>
                                <Avatar size="md" radius="xl" color="indigo">
                                    <IoMdPerson size={20} />
                                </Avatar>
                                <Stack gap={4} flex={1}>
                                    <Text fw={500} size="sm">
                                        {user.userName}
                                    </Text>
                                    <Text size="xs" c="dimmed">
                                        ID: {user.id}
                                    </Text>
                                </Stack>
                            </Group>
                        </Group>

                        {/* Email */}
                        <Group gap="xs" wrap="nowrap">
                            <IoMdMail size={14} />
                            <Text size="sm" flex={1} truncate>
                                {user.email}
                            </Text>
                        </Group>

                        {/* Roles and Mail Status */}
                        <Group align="flex-start" justify="space-between">
                            <Group gap="xs" wrap="wrap">
                                {user.roles.length > 0 ? (
                                    user.roles.map((role, index) => (
                                        <Badge
                                            key={index}
                                            color={getRoleColor(role)}
                                            variant="light"
                                            size="sm"
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
                                <Badge
                                    color={user.mailConfirmed ? "green" : "red"}
                                    variant="outline"
                                    size="sm"
                                >
                                    {user.mailConfirmed
                                        ? "Mail Confirmed"
                                        : "Mail Pending"}
                                </Badge>
                            </Group>
                            {/* Actions */}
                            <Group gap="xs" wrap="nowrap">
                                {currentUserId !== undefined &&
                                    user.id !== currentUserId && (
                                        <ActionIcon
                                            variant="light"
                                            color="blue"
                                            onClick={() => onEditRoles(user)}
                                        >
                                            <IoMdPerson size={14} />
                                        </ActionIcon>
                                    )}
                                {!user.mailConfirmed && (
                                    <ActionIcon
                                        variant="light"
                                        color="blue"
                                        onClick={() => onConfirmMail(user)}
                                    >
                                        <MdMail size={14} />
                                    </ActionIcon>
                                )}
                            </Group>
                        </Group>
                    </Stack>
                </Card>
            ))}
        </Stack>
    );
}
