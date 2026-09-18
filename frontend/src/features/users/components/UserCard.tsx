import {
    ActionIcon,
    Avatar,
    Badge,
    Card,
    Group,
    Stack,
    Text,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { CiMail, CiUser } from "react-icons/ci";
import { UserDto } from "../../auth/types/UserDto";

interface Props {
    user: UserDto;
    /** Admins can edit anyone's roles but their own, so the button is dropped here. */
    isCurrentUser: boolean;
    onEditRoles: (user: UserDto) => void;
    onConfirmMail: (user: UserDto) => void;
}

const initialsOf = (userName: string) =>
    userName
        .split(/\s+/)
        .map((word) => word[0])
        .join("")
        .slice(0, 2)
        .toUpperCase();

/**
 * One user in the admin list, on the same card the profile page draws an account on:
 * the filled avatar, the name, and the roles as accent badges. Only an unconfirmed
 * mailbox gets a colour of its own, being the one state an admin may need to act on.
 */
export default function UserCard({
    user,
    isCurrentUser,
    onEditRoles,
    onConfirmMail,
}: Props) {
    const theme = useMantineTheme();

    return (
        <Card withBorder shadow="sm" padding="lg" radius="md">
            <Group gap="md" align="center" wrap="nowrap">
                <Avatar
                    size={44}
                    radius="xl"
                    variant="filled"
                    color={theme.primaryColor}
                    style={{ flexShrink: 0 }}
                >
                    {initialsOf(user.userName)}
                </Avatar>
                <Stack gap={4} flex={1} style={{ minWidth: 0 }}>
                    <Group gap="xs" align="center" wrap="nowrap">
                        <Title
                            order={4}
                            lineClamp={1}
                            className="wrapped-text"
                            style={{ minWidth: 0 }}
                        >
                            {user.userName}
                        </Title>
                        {!user.mailConfirmed && (
                            <Badge
                                variant="light"
                                color="orange"
                                style={{ flexShrink: 0 }}
                            >
                                Mail not confirmed
                            </Badge>
                        )}
                    </Group>
                    <Text c="dimmed" size="sm" className="wrapped-text">
                        {user.email}
                    </Text>
                    <Group gap="xs" wrap="wrap">
                        {user.roles.length > 0 ? (
                            user.roles.map((role) => (
                                <Badge
                                    key={role}
                                    variant="light"
                                    color={theme.primaryColor}
                                    size="sm"
                                >
                                    {role}
                                </Badge>
                            ))
                        ) : (
                            <Badge variant="light" color="gray" size="sm">
                                No roles
                            </Badge>
                        )}
                    </Group>
                </Stack>
                <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
                    {!isCurrentUser && (
                        <ActionIcon
                            size="lg"
                            variant="outline"
                            color={theme.primaryColor}
                            onClick={() => onEditRoles(user)}
                            aria-label={`Edit roles for ${user.userName}`}
                        >
                            <CiUser size={18} />
                        </ActionIcon>
                    )}
                    {!user.mailConfirmed && (
                        <ActionIcon
                            size="lg"
                            variant="outline"
                            color={theme.primaryColor}
                            onClick={() => onConfirmMail(user)}
                            aria-label={`Confirm mail for ${user.userName}`}
                        >
                            <CiMail size={18} />
                        </ActionIcon>
                    )}
                </Group>
            </Group>
        </Card>
    );
}
