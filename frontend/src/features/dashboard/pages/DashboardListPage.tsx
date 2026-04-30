import {
    ActionIcon,
    Button,
    Card,
    Center,
    Group,
    Loader,
    Modal,
    ScrollArea,
    SimpleGrid,
    Stack,
    Text,
    TextInput,
    ThemeIcon,
    Title,
    UnstyledButton,
    useMantineTheme,
} from "@mantine/core";
import { createElement, useEffect, useState } from "react";
import { FaCircle } from "react-icons/fa";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import { TbLayoutDashboard } from "react-icons/tb";
import { useNavigate } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import Header from "../../../shared/components/Header";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import IconPicker from "../../trackers/components/IconPicker";
import { dashboardController } from "../api/dashboardController";
import { DashboardDto } from "../types/DashboardDto";

const PAGE_COLOR = "violet";

const colorOptions = [
    "indigo",
    "blue",
    "cyan",
    "grape",
    "green",
    "lime",
    "orange",
    "pink",
    "red",
    "teal",
    "yellow",
    "violet",
];

export default function DashboardListPage() {
    const navigate = useNavigate();
    const theme = useMantineTheme();
    const [dashboards, setDashboards] = useState<DashboardDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [newName, setNewName] = useState("");
    const [newColor, setNewColor] = useState("indigo");
    const [newIcon, setNewIcon] = useState<string | undefined>(undefined);
    const [isCreating, setIsCreating] = useState(false);
    const [deleteTarget, setDeleteTarget] = useState<DashboardDto | null>(null);

    const load = async () => {
        const res = await dashboardController.getDashboards();
        setDashboards(res.data ?? []);
        setIsLoading(false);
    };

    useEffect(() => {
        load();
    }, []);

    const handleCreate = async () => {
        if (!newName.trim()) return;
        setIsCreating(true);
        const res = await dashboardController.createDashboard({
            name: newName.trim(),
            color: newColor,
            icon: newIcon,
        });
        setIsCreating(false);
        setIsCreateOpen(false);
        resetCreateForm();
        navigate(`/dashboard/${res.data.id}`);
    };

    const resetCreateForm = () => {
        setNewName("");
        setNewColor("indigo");
        setNewIcon(undefined);
    };

    const handleDelete = async () => {
        if (!deleteTarget) return;
        await dashboardController.deleteDashboard(deleteTarget.id);
        setDeleteTarget(null);
        load();
    };

    return (
        <Stack h="100%" gap="md">
            <Group w="100%" justify="space-between">
                <Title order={2} c={PAGE_COLOR}>
                    Dashboards
                </Title>
                <Header color={PAGE_COLOR} />
            </Group>
            <Group justify="flex-end">
                <Button
                    color={PAGE_COLOR}
                    variant="outline"
                    leftSection={<FiPlus size={18} />}
                    onClick={() => setIsCreateOpen(true)}
                >
                    Create
                </Button>
            </Group>

            {isLoading ? (
                <Center style={{ flex: 1 }}>
                    <Loader color={PAGE_COLOR} />
                </Center>
            ) : dashboards.length === 0 ? (
                <Stack align="center" gap="md" py={80}>
                    <ThemeIcon
                        size={72}
                        radius="xl"
                        variant="light"
                        color={PAGE_COLOR}
                    >
                        <TbLayoutDashboard size={36} />
                    </ThemeIcon>
                    <Stack align="center" gap={4}>
                        <Text fw={700} size="xl">
                            No dashboards yet
                        </Text>
                        <Text size="sm" c="dimmed">
                            Create a dashboard to aggregate analytics from your
                            trackers
                        </Text>
                    </Stack>
                    <Button
                        color={PAGE_COLOR}
                        leftSection={<FiPlus size={16} />}
                        onClick={() => setIsCreateOpen(true)}
                    >
                        Get Started
                    </Button>
                </Stack>
            ) : (
                <ScrollArea flex={1}>
                    <SimpleGrid cols={{ base: 2, sm: 3, md: 5 }} spacing="md">
                        {dashboards.map((dashboard) => {
                            const color =
                                dashboard.color &&
                                dashboard.color in theme.colors
                                    ? dashboard.color
                                    : "indigo";

                            return (
                                <Card
                                    key={dashboard.id}
                                    shadow="sm"
                                    padding="lg"
                                    radius="md"
                                    withBorder
                                    style={{
                                        borderTop: `3px solid var(--mantine-color-${color}-5)`,
                                        cursor: "pointer",
                                    }}
                                    onClick={() =>
                                        navigate(`/dashboard/${dashboard.id}`)
                                    }
                                >
                                    <Stack
                                        align="center"
                                        gap="sm"
                                        style={{ position: "relative" }}
                                    >
                                        <ActionIcon
                                            size="lg"
                                            variant="outline"
                                            color={"red"}
                                            style={{
                                                position: "absolute",
                                                top: 0,
                                                right: 0,
                                            }}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                setDeleteTarget(dashboard);
                                            }}
                                        >
                                            <MdDelete size={16} />
                                        </ActionIcon>
                                        <ThemeIcon
                                            size={52}
                                            radius="md"
                                            variant="light"
                                            color={color}
                                        >
                                            {createElement(
                                                resolveTrackerIcon(
                                                    dashboard.icon,
                                                ),
                                                { size: 26 },
                                            )}
                                        </ThemeIcon>
                                        <Stack gap={2} align="center">
                                            <Title
                                                order={4}
                                                ta="center"
                                                lineClamp={1}
                                            >
                                                {dashboard.name}
                                            </Title>
                                            <Text size="sm" c="dimmed">
                                                {dashboard.items.length}{" "}
                                                analytic
                                                {dashboard.items.length !== 1
                                                    ? "s"
                                                    : ""}
                                            </Text>
                                        </Stack>
                                    </Stack>
                                </Card>
                            );
                        })}
                    </SimpleGrid>
                </ScrollArea>
            )}

            <Modal
                opened={isCreateOpen}
                onClose={() => {
                    setIsCreateOpen(false);
                    resetCreateForm();
                }}
                title="New dashboard"
                centered
            >
                <Stack gap="md">
                    <TextInput
                        label="Dashboard Name"
                        placeholder="Enter dashboard name"
                        value={newName}
                        onChange={(e) => setNewName(e.currentTarget.value)}
                        onKeyDown={(e) => e.key === "Enter" && handleCreate()}
                        autoFocus
                    />
                    <Stack gap="xs">
                        <Text size="sm" fw={500}>
                            Dashboard Color
                        </Text>
                        <Group gap="xs">
                            {colorOptions.map((c) => (
                                <UnstyledButton
                                    key={c}
                                    onClick={() => setNewColor(c)}
                                    style={{
                                        borderRadius: "50%",
                                        padding: 2,
                                        border:
                                            newColor === c
                                                ? `2px solid ${theme.colors[c]?.[6]}`
                                                : "2px solid transparent",
                                        lineHeight: 0,
                                    }}
                                >
                                    <FaCircle
                                        size={22}
                                        color={theme.colors[c]?.[6]}
                                    />
                                </UnstyledButton>
                            ))}
                        </Group>
                    </Stack>
                    <IconPicker
                        value={newIcon}
                        onChange={setNewIcon}
                        color={newColor}
                    />
                    <Group justify="flex-end">
                        <Button
                            variant="default"
                            onClick={() => {
                                setIsCreateOpen(false);
                                resetCreateForm();
                            }}
                        >
                            Cancel
                        </Button>
                        <Button
                            color={PAGE_COLOR}
                            disabled={!newName.trim()}
                            loading={isCreating}
                            onClick={handleCreate}
                        >
                            Create
                        </Button>
                    </Group>
                </Stack>
            </Modal>

            <ConfirmationDialog
                isOpen={!!deleteTarget}
                onClose={() => setDeleteTarget(null)}
                onConfirm={handleDelete}
                title={
                    deleteTarget ? `Delete "${deleteTarget.name}"?` : "Delete?"
                }
                message="This will permanently delete the dashboard and all its items."
                severity="warning"
            />
        </Stack>
    );
}
