import {
    Button,
    Group,
    Stack,
    Text,
    ThemeIcon,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { observer } from "mobx-react";
import { createElement, useCallback, useEffect, useState } from "react";
import { FiCheck, FiPlus } from "react-icons/fi";
import { TbEyeOff, TbLayoutDashboard } from "react-icons/tb";
import { useNavigate, useParams } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import SidebarBurger from "../../../shared/components/navigation/SidebarBurger";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import navigationStore from "../../../shared/stores/NavigationStore";
import { dashboardController } from "../api/dashboardController";
import BoardActions from "../components/BoardActions";
import BoardFormModal from "../components/BoardFormModal";
import { DashboardGrid } from "../components/DashboardGrid";
import { EditEntriesWidgetModal } from "../components/EditEntriesWidgetModal";
import { EditTextWidgetModal } from "../components/EditTextWidgetModal";
import { EditFilterModal } from "../components/EditFilterModal";
import { EditWidgetModal } from "../components/EditWidgetModal";
import { HiddenWidgetsModal } from "../components/HiddenWidgetsModal";
import { WidgetsProvider } from "../../widgets/context/WidgetsContext";
import { WidgetLibraryModal } from "../../widgets/components/WidgetLibraryModal";
import { DashboardProvider, useDashboard } from "../context/DashboardContext";
import {
    DashboardDto,
    DashboardItemDisplayMode,
    parseTextWidgetConfig,
    WidgetTypes,
} from "../types/DashboardDto";

const LAST_BOARD_KEY = "operum.lastBoardId";

interface ContentProps {
    activeBoard: DashboardDto;
    onEditBoard: () => void;
    onDeleteBoard: () => void;
}

function DashboardContent({
    activeBoard,
    onEditBoard,
    onDeleteBoard,
}: ContentProps) {
    const {
        widgets,
        isLoading,
        refreshWidgets,
        updateItem,
        updateEntriesItem,
        setFilterValues,
        updateFilterItem,
        setTextContent,
        removeItem,
        saveLayout,
    } = useDashboard();
    const theme = useMantineTheme();
    const [isConfiguring, setIsConfiguring] = useState(false);
    const [isWidgetsOpen, setIsWidgetsOpen] = useState(false);
    const [isHiddenOpen, setIsHiddenOpen] = useState(false);
    const [editingItemId, setEditingItemId] = useState<string>();
    const editingWidget = widgets.find((w) => w.id === editingItemId);

    // Widgets set to Hidden on a grid are dropped from it, so this count is the only cue
    // that they still exist. Only Analytic/Entries widgets carry a display mode.
    const hiddenCount = widgets.filter(
        (w) =>
            w.layout.displayMode === DashboardItemDisplayMode.Hidden ||
            w.mobileLayout.displayMode === DashboardItemDisplayMode.Hidden,
    ).length;

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
        <Stack h="100%" gap="md" pb="md">
            <Group
                w="100%"
                gap="xs"
                justify="space-between"
                wrap="nowrap"
                align="center"
            >
                {/* Board name ellipsizes rather than pushing the actions off-screen. */}
                <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                    <SidebarBurger />
                    <ThemeIcon
                        size={32}
                        radius="md"
                        variant="light"
                        color={color}
                        style={{ flexShrink: 0 }}
                    >
                        {createElement(resolveTrackerIcon(activeBoard.icon), {
                            size: 18,
                        })}
                    </ThemeIcon>
                    <Title order={3} c={color} lineClamp={1} style={{ minWidth: 0 }}>
                        {activeBoard.name}
                    </Title>
                </Group>

                <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
                    {/* Hidden widgets are off the grid entirely, so this is the
                        only way back to them. Shown only when there are any. */}
                    {hiddenCount > 0 && (
                        <Button
                            size="sm"
                            radius="xl"
                            variant="outline"
                            color={color}
                            px={isMobile ? "xs" : undefined}
                            leftSection={
                                isMobile ? undefined : <TbEyeOff size={16} />
                            }
                            aria-label={
                                isMobile
                                    ? `Hidden widgets (${hiddenCount})`
                                    : undefined
                            }
                            onClick={() => setIsHiddenOpen(true)}
                            style={{ flexShrink: 0 }}
                        >
                            {isMobile ? (
                                <Group gap={4} wrap="nowrap">
                                    <TbEyeOff size={16} />
                                    {hiddenCount}
                                </Group>
                            ) : (
                                `Hidden (${hiddenCount})`
                            )}
                        </Button>
                    )}
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
                    <BoardActions
                        color={color}
                        isConfiguring={isConfiguring}
                        isMobile={!!isMobile}
                        onEdit={onEditBoard}
                        onDelete={onDeleteBoard}
                        onToggleArrange={() => setIsConfiguring((v) => !v)}
                        onOpenWidgets={() => setIsWidgetsOpen(true)}
                    />
                </Group>
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
                        onClick={() => setIsWidgetsOpen(true)}
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
                    onFilterSetValues={setFilterValues}
                />
            )}

            {/* A Header/Note widget's text, a Container's title and an Entries widget's own
                settings all live in the widget the board already holds, so none of these
                edit dialogs needs a fetch of its own; an Analytic widget's sources still go
                through EditWidgetModal's own load, since those aren't part of the board's
                widget list. */}
            {editingItemId && editingWidget && editingWidget.type === WidgetTypes.Header && (
                <EditTextWidgetModal
                    itemId={editingItemId}
                    kind="header"
                    initialText={parseTextWidgetConfig(editingWidget.config)?.text ?? ""}
                    color={color}
                    onClose={closeEditing}
                    onSave={setTextContent}
                />
            )}

            {editingItemId && editingWidget && editingWidget.type === WidgetTypes.Note && (
                <EditTextWidgetModal
                    itemId={editingItemId}
                    kind="note"
                    initialText={parseTextWidgetConfig(editingWidget.config)?.text ?? ""}
                    color={color}
                    onClose={closeEditing}
                    onSave={setTextContent}
                />
            )}

            {editingItemId && editingWidget && editingWidget.type === WidgetTypes.Container && (
                <EditTextWidgetModal
                    itemId={editingItemId}
                    kind="container"
                    initialText={parseTextWidgetConfig(editingWidget.config)?.text ?? ""}
                    color={color}
                    onClose={closeEditing}
                    onSave={setTextContent}
                />
            )}

            {editingItemId && editingWidget && editingWidget.type === WidgetTypes.Entries && (
                <EditEntriesWidgetModal
                    itemId={editingItemId}
                    color={color}
                    onClose={closeEditing}
                    onSave={updateEntriesItem}
                />
            )}

            {editingItemId &&
                editingWidget &&
                editingWidget.type === WidgetTypes.Filter && (
                    <EditFilterModal
                        itemId={editingItemId}
                        color={color}
                        onClose={closeEditing}
                        onSave={updateFilterItem}
                    />
                )}

            {editingItemId &&
                editingWidget &&
                editingWidget.type !== WidgetTypes.Header &&
                editingWidget.type !== WidgetTypes.Note &&
                editingWidget.type !== WidgetTypes.Container &&
                editingWidget.type !== WidgetTypes.Entries &&
                editingWidget.type !== WidgetTypes.Filter && (
                    <EditWidgetModal
                        itemId={editingItemId}
                        color={color}
                        onClose={closeEditing}
                        onSave={updateItem}
                    />
                )}

            {isHiddenOpen && (
                <HiddenWidgetsModal
                    widgets={widgets}
                    color={color}
                    onEdit={setEditingItemId}
                    onClose={() => setIsHiddenOpen(false)}
                />
            )}

            {isWidgetsOpen && (
                <WidgetLibraryModal
                    color={color}
                    onClose={() => {
                        setIsWidgetsOpen(false);
                        // Placing/creating a widget adds it to the board, and a widget
                        // deleted in the Library cascades to its placements on the server,
                        // so the board needs re-pulling either way.
                        refreshWidgets();
                    }}
                />
            )}
        </Stack>
    );
}

const DashboardPage = observer(function DashboardPage() {
    const { dashboardId } = useParams<{ dashboardId: string }>();
    const navigate = useNavigate();
    const theme = useMantineTheme();

    // The board list lives on navigationStore -- the sidebar, the command palette
    // and this page all read the same array, so a board created or renamed from
    // anywhere is visible here without a fetch of its own. AppLayout kicks off the
    // initial load; this just waits for it.
    const boards = navigationStore.dashboards;
    const isLoadingBoards = !navigationStore.loaded;
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [isEditOpen, setIsEditOpen] = useState(false);
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);

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
            await navigationStore.refreshDashboards();
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
            await navigationStore.refreshDashboards();
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
        await navigationStore.refreshDashboards();
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
            {/* The Widget Library's saved definitions, so WidgetLibraryModal can list and
                place them without a fetch of its own -- see PlaceFromLibraryForm. */}
            <WidgetsProvider>
                <DashboardProvider
                    key={activeBoard.id}
                    dashboardId={activeBoard.id}
                >
                    <DashboardContent
                        activeBoard={activeBoard}
                        onEditBoard={() => setIsEditOpen(true)}
                        onDeleteBoard={() => setIsDeleteOpen(true)}
                    />
                </DashboardProvider>
            </WidgetsProvider>

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
});

export default DashboardPage;
