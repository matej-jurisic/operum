import {
    Button,
    Group,
    Stack,
    Text,
    ThemeIcon,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useCallback, useEffect, useState } from "react";
import { FiCheck, FiPlus } from "react-icons/fi";
import { TbLayoutDashboard } from "react-icons/tb";
import { useNavigate, useParams } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import Header from "../../../shared/components/Header";
import { dashboardController } from "../api/dashboardController";
import { AddWidgetModal } from "../components/AddWidgetModal";
import BoardActions from "../components/BoardActions";
import BoardFormModal from "../components/BoardFormModal";
import BoardSwitcher from "../components/BoardSwitcher";
import { DashboardGrid } from "../components/DashboardGrid";
import { EditWidgetModal } from "../components/EditWidgetModal";
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
        widgets,
        isLoading,
        refreshWidgets,
        addItem,
        addItemFromAnalytic,
        addQuickAddItem,
        addViewItem,
        addEntriesItem,
        updateItem,
        setViewSelection,
        removeItem,
        saveLayout,
    } = useDashboard();
    const theme = useMantineTheme();
    const [isConfiguring, setIsConfiguring] = useState(false);
    const [isAddOpen, setIsAddOpen] = useState(false);
    const [editingItemId, setEditingItemId] = useState<string>();

    // Stable, because the edit dialog loads the widget it was opened on in an effect keyed
    // on this: an identity that changed with every render of the board would send it back
    // for the same widget each time one did.
    const closeEditing = useCallback(() => setEditingItemId(undefined), []);

    // The board's name, the actions on it, the way out of arrange mode and the app's own
    // controls all share one row. On a phone that row only fits if the buttons on it drop
    // their labels and keep just their icons.
    const isMobile = useMediaQuery("(max-width: 48em)");

    useEffect(() => {
        refreshWidgets();
    }, [refreshWidgets]);

    const color =
        activeBoard.color && activeBoard.color in theme.colors
            ? activeBoard.color
            : theme.primaryColor;

    return (
        <Stack h="100%" gap="md">
            <Group
                w="100%"
                gap="xs"
                justify="space-between"
                wrap="nowrap"
                align="center"
            >
                {/* Takes whatever the header leaves it, down to nothing: the board
                    switcher ellipsizes its name rather than pushing the row wider,
                    so the buttons beside it keep their full width. */}
                <Group gap="xs" wrap="nowrap" style={{ minWidth: 0 }}>
                    <BoardSwitcher
                        boards={boards}
                        activeBoardId={activeBoard.id}
                        onSelect={onSelectBoard}
                        onCreate={onCreateBoard}
                    />
                    <BoardActions
                        color={color}
                        isConfiguring={isConfiguring}
                        isMobile={!!isMobile}
                        onEdit={onEditBoard}
                        onDelete={onDeleteBoard}
                        onAddItem={() => setIsAddOpen(true)}
                        onToggleArrange={() => setIsConfiguring((v) => !v)}
                    />
                    {/* The only way out of arrange mode that does not cost a
                        row of chrome while the board is just being read. On a
                        phone the tick carries it on its own. */}
                    {isConfiguring && (
                        <Button
                            size="sm"
                            radius="xl"
                            color={color}
                            px={isMobile ? "xs" : undefined}
                            leftSection={
                                isMobile ? undefined : <FiCheck size={16} />
                            }
                            aria-label={isMobile ? "Done arranging" : undefined}
                            onClick={() => setIsConfiguring(false)}
                            style={{ flexShrink: 0 }}
                        >
                            {isMobile ? <FiCheck size={16} /> : "Done"}
                        </Button>
                    )}
                </Group>
                <Header color={color} />
            </Group>

            {/* While widgets are (re)loading, the global request loader already
                covers the wait — rendering nothing here avoids stacking a
                second, differently-styled spinner on top of it. */}
            {isLoading ? null : widgets.length === 0 ? (
                <Stack align="center" gap="md" py={80}>
                    <ThemeIcon
                        size={72}
                        radius="xl"
                        variant="light"
                        color={color}
                    >
                        <TbLayoutDashboard size={36} />
                    </ThemeIcon>
                    <Text fw={700} size="xl">
                        Nothing on this board yet
                    </Text>
                    <Button
                        color={color}
                        leftSection={<FiPlus size={16} />}
                        onClick={() => setIsAddOpen(true)}
                    >
                        Get Started
                    </Button>
                </Stack>
            ) : (
                <DashboardGrid
                    widgets={widgets}
                    color={color}
                    isConfiguring={isConfiguring}
                    onLayoutSave={saveLayout}
                    onRemove={removeItem}
                    onEdit={setEditingItemId}
                    onViewSelect={setViewSelection}
                />
            )}

            {editingItemId && (
                <EditWidgetModal
                    itemId={editingItemId}
                    color={color}
                    onClose={closeEditing}
                    onSave={updateItem}
                />
            )}

            {isAddOpen && (
                <AddWidgetModal
                    color={color}
                    onClose={() => setIsAddOpen(false)}
                    onAdd={addItem}
                    onAddFromAnalytic={addItemFromAnalytic}
                    onAddQuickAdd={addQuickAddItem}
                    onAddView={addViewItem}
                    onAddEntries={addEntriesItem}
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

    // The global request loader already covers this fetch; rendering
    // nothing here avoids a second, differently-styled spinner on top of it.
    if (isLoadingBoards) {
        return null;
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
                    <Text fw={700} size="xl">
                        No boards yet
                    </Text>
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

    // The effect above is redirecting to a real board; this is a one-frame
    // gap, not a fetch, so it gets no spinner of its own either.
    if (!activeBoard) {
        return null;
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
