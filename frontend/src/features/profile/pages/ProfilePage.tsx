import {
    Avatar,
    Badge,
    Button,
    Card,
    Divider,
    Group,
    PasswordInput,
    ScrollArea,
    SimpleGrid,
    Stack,
    Text,
    TextInput,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useState } from "react";
import { TbDatabase, TbLayoutGrid, TbUsers } from "react-icons/tb";
import { useNavigate } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import Header from "../../../shared/components/Header";
import globalStore from "../../../shared/stores/GlobalStore";
import useAuth from "../../auth/hooks/useAuth";
import { profileController, UserProfileStatsDto } from "../api/profileController";

export default function ProfilePage() {
    const theme = useMantineTheme();
    const navigate = useNavigate();
    const { setUserData, clearUserData } = useAuth();
    const user = globalStore.currentUser!;

    const [stats, setStats] = useState<UserProfileStatsDto | null>(null);
    const [showDeleteDialog, setShowDeleteDialog] = useState(false);

    const usernameForm = useForm({
        initialValues: { userName: user.userName },
        validate: {
            userName: (v) =>
                v.length < 3
                    ? "At least 3 characters"
                    : v.length > 20
                    ? "At most 20 characters"
                    : null,
        },
    });

    const passwordForm = useForm({
        initialValues: {
            currentPassword: "",
            newPassword: "",
            confirmPassword: "",
        },
        validate: {
            currentPassword: (v) => (!v ? "Required" : null),
            newPassword: (v) =>
                v.length < 6
                    ? "At least 6 characters"
                    : !/\d/.test(v)
                    ? "Must contain a digit"
                    : null,
            confirmPassword: (v, values) =>
                v !== values.newPassword ? "Passwords do not match" : null,
        },
    });

    useEffect(() => {
        profileController.getStats().then((r) => {
            if (r.isSuccess) setStats(r.data);
        });
    }, []);

    const handleUsernameSubmit = async (values: { userName: string }) => {
        const res = await profileController.updateUsername(values.userName);
        if (res.isSuccess) {
            setUserData({
                id: user.id,
                userName: res.data.userName ?? values.userName,
                roles: user.roles,
            });
        }
    };

    const handlePasswordSubmit = async (values: typeof passwordForm.values) => {
        const res = await profileController.changePassword(
            values.currentPassword,
            values.newPassword
        );
        if (res.isSuccess) passwordForm.reset();
    };

    const handleDeleteAccount = async () => {
        await profileController.deleteAccount();
        clearUserData();
        navigate("/home");
    };

    const initials = user.userName
        .split(/\s+/)
        .map((w) => w[0])
        .join("")
        .slice(0, 2)
        .toUpperCase();

    const statCards = [
        {
            label: "Trackers Owned",
            value: stats?.trackersOwned ?? "—",
            icon: <TbLayoutGrid size={20} />,
            color: "blue",
        },
        {
            label: "Shared With Me",
            value: stats?.sharedWithMe ?? "—",
            icon: <TbUsers size={20} />,
            color: "teal",
        },
        {
            label: "Total Entries",
            value: stats?.totalEntries ?? "—",
            icon: <TbDatabase size={20} />,
            color: "grape",
        },
    ];

    return (
        <>
            <Stack gap="md" h="100%">
                <Group justify="space-between" w="100%">
                    <Title order={2} c={theme.primaryColor}>
                        Profile
                    </Title>
                    <Header />
                </Group>

                <ScrollArea flex={1}>
                    <Stack gap="xl" maw={680}>
                        {/* Identity card */}
                        <Card withBorder radius="md" p="xl">
                            <Group gap="xl" align="flex-start">
                                <Avatar
                                    size={72}
                                    radius="xl"
                                    color={theme.primaryColor}
                                    variant="filled"
                                >
                                    {initials}
                                </Avatar>
                                <Stack gap={6} flex={1}>
                                    <Text fw={700} size="xl">
                                        {user.userName}
                                    </Text>
                                    {user.email && (
                                        <Text size="sm" c="dimmed">
                                            {user.email}
                                        </Text>
                                    )}
                                    <Group gap="xs" mt={4}>
                                        {user.roles.map((r) => (
                                            <Badge
                                                key={r}
                                                variant="light"
                                                color={theme.primaryColor}
                                                size="sm"
                                            >
                                                {r}
                                            </Badge>
                                        ))}
                                    </Group>
                                </Stack>
                            </Group>
                        </Card>

                        {/* Stats */}
                        <SimpleGrid cols={{ base: 1, sm: 3 }}>
                            {statCards.map((s) => (
                                <Card
                                    key={s.label}
                                    withBorder
                                    radius="md"
                                    p="lg"
                                    style={{
                                        borderTop: `3px solid var(--mantine-color-${s.color}-5)`,
                                    }}
                                >
                                    <Stack gap={4}>
                                        <Text
                                            size="xs"
                                            c="dimmed"
                                            fw={600}
                                            tt="uppercase"
                                            style={{ letterSpacing: "0.05em" }}
                                        >
                                            {s.label}
                                        </Text>
                                        <Text fw={700} size="xl">
                                            {s.value}
                                        </Text>
                                    </Stack>
                                </Card>
                            ))}
                        </SimpleGrid>

                        {/* Change username */}
                        <Card withBorder radius="md" p="lg">
                            <form
                                onSubmit={usernameForm.onSubmit(
                                    handleUsernameSubmit
                                )}
                            >
                                <Stack gap="md">
                                    <Text fw={600}>Username</Text>
                                    <TextInput
                                        label="Username"
                                        {...usernameForm.getInputProps(
                                            "userName"
                                        )}
                                    />
                                    <Group justify="flex-end">
                                        <Button
                                            type="submit"
                                            variant="outline"
                                        >
                                            Save Username
                                        </Button>
                                    </Group>
                                </Stack>
                            </form>
                        </Card>

                        {/* Change password */}
                        <Card withBorder radius="md" p="lg">
                            <form
                                onSubmit={passwordForm.onSubmit(
                                    handlePasswordSubmit
                                )}
                            >
                                <Stack gap="md">
                                    <Text fw={600}>Change Password</Text>
                                    <PasswordInput
                                        label="Current Password"
                                        {...passwordForm.getInputProps(
                                            "currentPassword"
                                        )}
                                    />
                                    <PasswordInput
                                        label="New Password"
                                        {...passwordForm.getInputProps(
                                            "newPassword"
                                        )}
                                    />
                                    <PasswordInput
                                        label="Confirm New Password"
                                        {...passwordForm.getInputProps(
                                            "confirmPassword"
                                        )}
                                    />
                                    <Group justify="flex-end">
                                        <Button
                                            type="submit"
                                            variant="outline"
                                        >
                                            Change Password
                                        </Button>
                                    </Group>
                                </Stack>
                            </form>
                        </Card>

                        {/* Danger zone */}
                        <Card
                            withBorder
                            radius="md"
                            p="lg"
                            style={{
                                borderColor:
                                    "var(--mantine-color-red-5)",
                            }}
                        >
                            <Stack gap="md">
                                <Text fw={600} c="red">
                                    Danger Zone
                                </Text>
                                <Divider color="red" opacity={0.3} />
                                <Group justify="space-between" align="center">
                                    <Stack gap={2}>
                                        <Text fw={500} size="sm">
                                            Delete Account
                                        </Text>
                                        <Text size="xs" c="dimmed">
                                            Permanently deletes your account and
                                            all trackers you own. Cannot be
                                            undone.
                                        </Text>
                                    </Stack>
                                    <Button
                                        color="red"
                                        variant="outline"
                                        onClick={() =>
                                            setShowDeleteDialog(true)
                                        }
                                    >
                                        Delete Account
                                    </Button>
                                </Group>
                            </Stack>
                        </Card>
                    </Stack>
                </ScrollArea>
            </Stack>

            {showDeleteDialog && (
                <ConfirmationDialog
                    isOpen
                    severity="important"
                    message="Are you sure you want to permanently delete your account? All trackers you own will also be deleted. This cannot be undone."
                    onClose={() => setShowDeleteDialog(false)}
                    onConfirm={handleDeleteAccount}
                />
            )}
        </>
    );
}
