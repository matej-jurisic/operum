import {
    ActionIcon,
    Button,
    Center,
    Divider,
    Group,
    Loader,
    Menu,
    Stack,
    Text,
    ThemeIcon,
    Tooltip,
    useMantineTheme,
} from "@mantine/core";
import { useCallback, useEffect, useState } from "react";
import { CiSettings } from "react-icons/ci";
import { FiMoreVertical, FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import { TbLayoutDashboard } from "react-icons/tb";
import { useNavigate, useParams } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import Header from "../../../shared/components/Header";
import { AnalyticsGrid } from "../../analytics/components/AnalyticsGrid";
import { dashboardController } from "../api/dashboardController";
import { AddDashboardItemModal } from "../components/AddDashboardItemModal";
import BoardFormModal from "../components/BoardFormModal";
import BoardSwitcher from "../components/BoardSwitcher";
import { DashboardProvider, useDashboard } from "../context/DashboardContext";
import { DashboardDto } from "../types/DashboardDto";

const LAST_BOARD_KEY = "operum.lastBoardId";

interface ContentProps {
    boards: DashboardDto[];
    activeBoard: DashboardDto;
    onSelectBoard: (boardId: string) => void;
    onCreateBoard: () => void;
    onEditBoard: () => void;
    onDeleteBoard: () => void;
}

function DashboardContent({
    boards,
    activeBoard,
    onSelectBoard,
    onCreateBoard,
    onEditBoard,
    onDeleteBoard,
}: ContentProps) {
    const {
        analytics,
        isLoading,
        refreshAnalytics,
        addItem,
        removeItem,
        reorderItems,
    } = useDashboard();
    const theme = useMantineTheme();
    const [isConfiguring, setIsConfiguring] = useState(false);
    const [isAddOpen, setIsAddOpen] = useState(false);

    useEffect(() => {
        refreshAnalytics();
    }, [refreshAnalytics]);

    const color =
        activeBoard.color && activeBoard.color in theme.colors
            ? activeBoard.color
            : theme.primaryColor;

    return (
        <Stack h="100%" gap="md">
            <Group w="100%" justify="flex-end">
                <Header color={color} />
            </Group>

            <Group gap="xs" wrap="nowrap" align="center">
                <BoardSwitcher
                    boards={boards}
                    activeBoardId={activeBoard.id}
                    onSelect={onSelectBoard}
                    onCreate={onCreateBoard}
                />
                <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
                    <Menu shadow="md" position="bottom-end" withinPortal>
                        <Menu.Target>
                            <ActionIcon
                                size="lg"
                                variant="outline"
                                color="gray"
                                aria-label="Board settings"
                            >
                                <FiMoreVertical size={18} />
                            </ActionIcon>
                        </Menu.Target>
                        <Menu.Dropdown>
                            <Menu.Item
                                leftSection={<MdEdit size={16} />}
                                onClick={onEditBoard}
                            >
                                Edit board
                            </Menu.Item>
                            <Menu.Item
                                color="red"
                                leftSection={<MdDelete size={16} />}
                                onClick={onDeleteBoard}
                            >
                                Delete board
                            </Menu.Item>
                        </Menu.Dropdown>
                    </Menu>
                    <Tooltip label="Arrange analytics" withArrow>
                        <ActionIcon
                            size="lg"
                            color={color}
                            variant={isConfiguring ? "filled" : "outline"}
                            onClick={() => setIsConfiguring((v) => !v)}
                            aria-label="Arrange analytics"
                        >
                            <CiSettings size={18} />
                        </ActionIcon>
                    </Tooltip>
                </Group>
            </Group>

            <Divider />

            {isConfiguring && (
                <Group justify="flex-end">
                    <Button
                        variant="outline"
                        color={color}
                        leftSection={<FiPlus size={18} />}
                        onClick={() => setIsAddOpen(true)}
                    >
                        Add analytic
                    </Button>
                </Group>
            )}

            {analytics.length === 0 && !isLoading ? (
                <Stack align="center" gap="md" py={80}>
                    <ThemeIcon
                        size={72}
                        radius="xl"
                        variant="light"
                        color={color}
                    >
                        <TbLayoutDashboard size={36} />
                    </ThemeIcon>
                    <Stack align="center" gap={4}>
                        <Text fw={700} size="xl">
                            No analytics added yet
                        </Text>
                        <Text size="sm" c="dimmed">
                            Add analytics from your trackers to display them
                            here
                        </Text>
                    </Stack>
                    <Button
                        color={color}
                        leftSection={<FiPlus size={16} />}
                        onClick={() => {
                            setIsConfiguring(true);
                            setIsAddOpen(true);
                        }}
                    >
                        Get Started
                    </Button>
                </Stack>
            ) : isLoading ? (
                <Center style={{ flex: 1 }}>
                    <Loader color={color} />
                </Center>
            ) : (
                <AnalyticsGrid
                    analytics={analytics}
                    color={color}
                    isConfiguring={isConfiguring}
                    onReorder={reorderItems}
                    onRemove={removeItem}
                />
            )}

            {isAddOpen && (
                <AddDashboardItemModal
                    onClose={() => setIsAddOpen(false)}
                    onAdd={addItem}
                />
            )}
        </Stack>
    );
}

export default function DashboardPage() {
    const { dashboardId } = useParams<{ dashboardId: string }>();
    const navigate = useNavigate();
    const theme = useMantineTheme();

    const [boards, setBoards] = useState<DashboardDto[]>([]);
    const [isLoadingBoards, setIsLoadingBoards] = useState(true);
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [isEditOpen, setIsEditOpen] = useState(false);
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);

    const loadBoards = useCallback(async () => {
        const res = await dashboardController.getDashboards();
        setBoards(res.data ?? []);
        setIsLoadingBoards(false);
    }, []);

    useEffect(() => {
        loadBoards();
    }, [loadBoards]);

    const activeBoard = boards.find((b) => b.id === dashboardId);

    // Resolve a bare /dashboard (or a stale id) to the last board that was opened
    useEffect(() => {
        if (isLoadingBoards || boards.length === 0) return;

        if (activeBoard) {
            localStorage.setItem(LAST_BOARD_KEY, activeBoard.id);
            return;
        }

        const remembered = localStorage.getItem(LAST_BOARD_KEY);
        const target = boards.find((b) => b.id === remembered) ?? boards[0];
        navigate(`/dashboard/${target.id}`, { replace: true });
    }, [isLoadingBoards, boards, activeBoard, navigate]);

    const handleCreate = async (values: {
        name: string;
        color?: string;
        icon?: string;
    }) => {
        try {
            const res = await dashboardController.createDashboard(values);
            setIsCreateOpen(false);
            await loadBoards();
            navigate(`/dashboard/${res.data.id}`);
        } catch {
            // The api layer already surfaced the error
        }
    };

    const handleEdit = async (values: {
        name: string;
        color?: string;
        icon?: string;
    }) => {
        if (!activeBoard) return;
        try {
            await dashboardController.updateDashboard(activeBoard.id, values);
            setIsEditOpen(false);
            await loadBoards();
        } catch {
            // The api layer already surfaced the error
        }
    };

    const handleDelete = async () => {
        if (!activeBoard) return;

        try {
            await dashboardController.deleteDashboard(activeBoard.id);
        } catch {
            return;
        } finally {
            setIsDeleteOpen(false);
        }

        const remaining = boards.filter((b) => b.id !== activeBoard.id);
        localStorage.removeItem(LAST_BOARD_KEY);
        await loadBoards();
        navigate(
            remaining.length > 0
                ? `/dashboard/${remaining[0].id}`
                : "/dashboard",
            { replace: true },
        );
    };

    const createModal = isCreateOpen && (
        <BoardFormModal
            onClose={() => setIsCreateOpen(false)}
            onSubmit={handleCreate}
        />
    );

    if (isLoadingBoards) {
        return (
            <Center h="100%">
                <Loader />
            </Center>
        );
    }

    if (boards.length === 0) {
        return (
            <Stack h="100%" gap="md">
                <Group w="100%" justify="flex-end">
                    <Header color={theme.primaryColor} />
                </Group>
                <Stack align="center" gap="md" py={80}>
                    <ThemeIcon
                        size={72}
                        radius="xl"
                        variant="light"
                        color={theme.primaryColor}
                    >
                        <TbLayoutDashboard size={36} />
                    </ThemeIcon>
                    <Stack align="center" gap={4}>
                        <Text fw={700} size="xl">
                            No boards yet
                        </Text>
                        <Text size="sm" c="dimmed">
                            Create a board to aggregate analytics from your
                            trackers
                        </Text>
                    </Stack>
                    <Button
                        leftSection={<FiPlus size={16} />}
                        onClick={() => setIsCreateOpen(true)}
                    >
                        Get Started
                    </Button>
                </Stack>
                {createModal}
            </Stack>
        );
    }

    // The effect above is redirecting to a real board
    if (!activeBoard) {
        return (
            <Center h="100%">
                <Loader />
            </Center>
        );
    }

    return (
        <>
            <DashboardProvider
                key={activeBoard.id}
                dashboardId={activeBoard.id}
            >
                <DashboardContent
                    boards={boards}
                    activeBoard={activeBoard}
                    onSelectBoard={(id) => navigate(`/dashboard/${id}`)}
                    onCreateBoard={() => setIsCreateOpen(true)}
                    onEditBoard={() => setIsEditOpen(true)}
                    onDeleteBoard={() => setIsDeleteOpen(true)}
                />
            </DashboardProvider>

            {createModal}

            {isEditOpen && (
                <BoardFormModal
                    board={activeBoard}
                    onClose={() => setIsEditOpen(false)}
                    onSubmit={handleEdit}
                />
            )}

            <ConfirmationDialog
                isOpen={isDeleteOpen}
                onClose={() => setIsDeleteOpen(false)}
                onConfirm={handleDelete}
                title={`Delete "${activeBoard.name}"?`}
                message="This will permanently delete the board and all its items."
                severity="warning"
            />
        </>
    );
}
