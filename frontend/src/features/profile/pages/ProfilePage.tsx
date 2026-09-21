import {
    Autocomplete,
    Avatar,
    Badge,
    Button,
    Card,
    Divider,
    Group,
    PasswordInput,
    ScrollArea,
    Select,
    SimpleGrid,
    Stack,
    Text,
    TextInput,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { notifySuccess } from "../../../shared/utils/notify";
import { PASSWORD_MIN_LENGTH, validatePassword, validatePasswordConfirmation } from "../../auth/utils/passwordRules";
import { useDocumentTitle } from "../../../shared/hooks/useDocumentTitle";
import { useForm } from "@mantine/form";
import { useClipboard } from "@mantine/hooks";
import { observer } from "mobx-react";
import { useEffect, useState } from "react";
import {
    TbCheck,
    TbCopy,
    TbDatabase,
    TbLayoutGrid,
    TbUsers,
} from "react-icons/tb";
import { useNavigate } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import SidebarBurger from "../../../shared/components/navigation/SidebarBurger";
import {
    FALLBACK_PAGE,
    writeDefaultPage,
} from "../../../shared/constants/defaultPage";
import globalStore from "../../../shared/stores/GlobalStore";
import navigationStore from "../../../shared/stores/NavigationStore";
import useAuth from "../../auth/hooks/useAuth";
import { trackersController } from "../../trackers/api/trackersController";
import {
    profileController,
    UserProfileStatsDto,
} from "../api/profileController";

const ProfilePage = observer(function ProfilePage() {
    useDocumentTitle("Profile");
    const theme = useMantineTheme();
    const navigate = useNavigate();
    const { setUserData, clearUserData } = useAuth();
    const user = globalStore.currentUser!;

    const [stats, setStats] = useState<UserProfileStatsDto | null>(null);
    const [showDeleteDialog, setShowDeleteDialog] = useState(false);
    const [isCopyingSchema, setIsCopyingSchema] = useState(false);
    const clipboard = useClipboard({ timeout: 2000 });
    const [defaultPage, setDefaultPage] = useState(
        user.defaultPage ?? FALLBACK_PAGE,
    );

    const pageOptions = [
        { value: FALLBACK_PAGE, label: "Last opened dashboard" },
        ...(navigationStore.dashboards.length
            ? [
                  {
                      group: "Dashboards",
                      items: navigationStore.dashboards.map((d) => ({
                          value: `/dashboard/${d.id}`,
                          label: d.name,
                      })),
                  },
              ]
            : []),
        ...(navigationStore.trackers.length
            ? [
                  {
                      group: "Trackers",
                      items: navigationStore.trackers.map((t) => ({
                          value: `/trackers/${t.id}`,
                          label: t.name,
                      })),
                  },
              ]
            : []),
    ];

    const handleDefaultPageChange = async (value: string | null) => {
        const next = value ?? FALLBACK_PAGE;
        setDefaultPage(next);
        const res = await profileController.updateDefaultPage(next);
        if (res.isSuccess) {
            globalStore.setCurrentUser({ ...user, defaultPage: next });
            writeDefaultPage(next);
        }
    };

    const handleCopySchema = async () => {
        setIsCopyingSchema(true);
        try {
            const res = await trackersController.getTrackerSchema();
            clipboard.copy(JSON.stringify(res.data, null, 2));
        } finally {
            setIsCopyingSchema(false);
        }
    };

    const timezoneForm = useForm({
        initialValues: { timeZone: user.timeZone ?? "" },
    });

    const usernameForm = useForm({
        initialValues: { userName: user.userName },
        validate: {
            userName: (v) =>
                v.length < 3
                    ? "Username must be at least 3 characters long"
                    : v.length > 20
                      ? "Username must be at most 20 characters long"
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
            currentPassword: (v) => (!v ? "Current password is required" : null),
            newPassword: validatePassword,
            confirmPassword: (v, values) =>
                validatePasswordConfirmation(v, values.newPassword),
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
            notifySuccess("Username updated");
        }
    };

    const handleTimezoneSubmit = async (values: { timeZone: string }) => {
        const res = await profileController.updateTimezone(values.timeZone);
        if (res.isSuccess) {
            globalStore.setCurrentUser({ ...user, timeZone: values.timeZone });
            notifySuccess("Time zone updated");
        }
    };

    const handlePasswordSubmit = async (values: typeof passwordForm.values) => {
        const res = await profileController.changePassword(
            values.currentPassword,
            values.newPassword,
        );
        if (res.isSuccess) {
            passwordForm.reset();
            notifySuccess("Password changed");
        }
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
            label: "Trackers owned",
            value: stats?.trackersOwned ?? "-",
            icon: <TbLayoutGrid size={20} />,
            color: "blue",
        },
        {
            label: "Shared with me",
            value: stats?.sharedWithMe ?? "-",
            icon: <TbUsers size={20} />,
            color: "teal",
        },
        {
            label: "Total entries",
            value: stats?.totalEntries ?? "-",
            icon: <TbDatabase size={20} />,
            color: "grape",
        },
    ];

    return (
        <>
            <Stack gap="md" h="100%">
                <Group gap="sm" wrap="nowrap">
                    <SidebarBurger />
                    <Title order={2} c={theme.primaryColor}>
                        Profile
                    </Title>
                </Group>

                <ScrollArea flex={1}>
                    <Stack align="center">
                        <Stack gap="xl" maw={680}>
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
                                                style={{
                                                    letterSpacing: "0.05em",
                                                }}
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

                            <Card withBorder radius="md" p="lg">
                                <form
                                    onSubmit={usernameForm.onSubmit(
                                        handleUsernameSubmit,
                                    )}
                                >
                                    <Stack gap="md">
                                        <Text fw={600}>Username</Text>
                                        <TextInput
                                            label="Username"
                                            {...usernameForm.getInputProps(
                                                "userName",
                                            )}
                                        />
                                        <Group justify="flex-end">
                                            <Button
                                                type="submit"
                                                variant="outline"
                                            >
                                                Save username
                                            </Button>
                                        </Group>
                                    </Stack>
                                </form>
                            </Card>

                            <Card withBorder radius="md" p="lg">
                                <form
                                    onSubmit={timezoneForm.onSubmit(
                                        handleTimezoneSubmit,
                                    )}
                                >
                                    <Stack gap="md">
                                        <Text fw={600}>Timezone</Text>
                                        <Autocomplete
                                            label="Timezone"
                                            placeholder="e.g. Europe/Zagreb"
                                            data={Intl.supportedValuesOf("timeZone")}
                                            {...timezoneForm.getInputProps("timeZone")}
                                        />
                                        <Group justify="flex-end">
                                            <Button
                                                type="submit"
                                                variant="outline"
                                            >
                                                Save time zone
                                            </Button>
                                        </Group>
                                    </Stack>
                                </form>
                            </Card>

                            <Card withBorder radius="md" p="lg">
                                <Stack gap="md">
                                    <Text fw={600}>Default page</Text>
                                    <Select
                                        label="Opens on load"
                                        data={pageOptions}
                                        value={defaultPage}
                                        allowDeselect={false}
                                        checkIconPosition="right"
                                        onChange={handleDefaultPageChange}
                                    />
                                </Stack>
                            </Card>

                            <Card withBorder radius="md" p="lg">
                                <Stack gap="md">
                                    <Text fw={600}>Tracker schema</Text>
                                    <Group justify="space-between" align="center">
                                        <Text size="sm" c="dimmed">
                                            Names only, no entries or ids.
                                        </Text>
                                        <Button
                                            variant="outline"
                                            loading={isCopyingSchema}
                                            leftSection={
                                                clipboard.copied ? (
                                                    <TbCheck size={16} />
                                                ) : (
                                                    <TbCopy size={16} />
                                                )
                                            }
                                            onClick={handleCopySchema}
                                        >
                                            {clipboard.copied
                                                ? "Copied"
                                                : "Copy schema"}
                                        </Button>
                                    </Group>
                                </Stack>
                            </Card>

                            <Card withBorder radius="md" p="lg">
                                <form
                                    onSubmit={passwordForm.onSubmit(
                                        handlePasswordSubmit,
                                    )}
                                >
                                    <Stack gap="md">
                                        <Text fw={600}>Change password</Text>
                                        <PasswordInput
                                            label="Current password"
                                            {...passwordForm.getInputProps(
                                                "currentPassword",
                                            )}
                                        />
                                        <PasswordInput
                                            label="New password"
                                            description={`At least ${PASSWORD_MIN_LENGTH} characters`}
                                            {...passwordForm.getInputProps(
                                                "newPassword",
                                            )}
                                        />
                                        <PasswordInput
                                            label="Confirm new password"
                                            {...passwordForm.getInputProps(
                                                "confirmPassword",
                                            )}
                                        />
                                        <Group justify="flex-end">
                                            <Button
                                                type="submit"
                                                variant="outline"
                                            >
                                                Change password
                                            </Button>
                                        </Group>
                                    </Stack>
                                </form>
                            </Card>

                            <Card
                                withBorder
                                radius="md"
                                p="lg"
                                style={{
                                    borderColor: "var(--mantine-color-red-5)",
                                }}
                            >
                                <Stack gap="md">
                                    <Text fw={600} c="red">
                                        Danger zone
                                    </Text>
                                    <Divider color="red" opacity={0.3} />
                                    <Group
                                        justify="space-between"
                                        align="center"
                                    >
                                        <Text fw={500} size="sm">
                                            Delete account
                                        </Text>
                                        <Button
                                            color="red"
                                            variant="outline"
                                            onClick={() =>
                                                setShowDeleteDialog(true)
                                            }
                                        >
                                            Delete account
                                        </Button>
                                    </Group>
                                </Stack>
                            </Card>
                        </Stack>
                    </Stack>
                </ScrollArea>
            </Stack>

            {showDeleteDialog && (
                <ConfirmationDialog
                    isOpen
                    severity="warning"
                    title="Delete account"
                    confirmLabel="Delete account"
                    message="Permanently delete your account and every tracker you own? This cannot be undone."
                    onClose={() => setShowDeleteDialog(false)}
                    onConfirm={handleDeleteAccount}
                />
            )}
        </>
    );
});

export default ProfilePage;
