import {
    Button,
    Group,
    SegmentedControl,
    Stack,
    Text,
    ThemeIcon,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useDocumentTitle } from "../../../shared/hooks/useDocumentTitle";
import { useMediaQuery } from "@mantine/hooks";
import { observer } from "mobx-react";
import { createElement, useCallback, useEffect, useState } from "react";
import { FiCheck, FiPlus, FiZap } from "react-icons/fi";
import {
    TbDeviceDesktop,
    TbDeviceMobile,
    TbEyeOff,
    TbLayoutDashboard,
    TbLayoutGrid,
} from "react-icons/tb";
import { useNavigate, useParams } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import NotFound from "../../../shared/components/NotFound";
import SidebarBurger from "../../../shared/components/navigation/SidebarBurger";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import navigationStore from "../../../shared/stores/NavigationStore";
import { dashboardController } from "../api/dashboardController";
import BoardActions from "../components/BoardActions";
import BoardFormModal from "../components/BoardFormModal";
import { DashboardGrid } from "../components/DashboardGrid";
import { EditEntriesWidgetModal } from "../components/EditEntriesWidgetModal";
import { EditGroupModal } from "../components/EditGroupModal";
import { EditTextWidgetModal } from "../components/EditTextWidgetModal";
import { EditFilterModal } from "../components/EditFilterModal";
import { EditWidgetModal } from "../components/EditWidgetModal";
import { HiddenWidgetsModal } from "../components/HiddenWidgetsModal";
import { WidgetLibraryModal } from "../../widgets/components/WidgetLibraryModal";
import { DashboardProvider, useDashboard } from "../context/DashboardContext";
import {
    DashboardDto,
    DashboardItemDisplayMode,
    parseTabsContainerConfig,
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
        saveTabsContainer,
        removeItem,
        saveLayout,
    } = useDashboard();
    const theme = useMantineTheme();
    const [isConfiguring, setIsConfiguring] = useState(false);
    // Arranges the mobile layout inside a phone-width frame while the viewport stays wide.
    // Cleared whenever arrange mode ends so the next session opens on the desktop board.
    const [previewMobile, setPreviewMobile] = useState(false);
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

    const isMobile = useMediaQuery("(max-width: 48em)");
    // Only offered above the narrow breakpoint (900px), where "Desktop" actually shows the
    // nested board and the mobile frame is a genuine preview rather than the real layout.
    const canPreviewMobile = useMediaQuery("(min-width: 60em)");

    useEffect(() => {
        refreshWidgets();
    }, [refreshWidgets]);

    useEffect(() => {
        if (!isConfiguring) setPreviewMobile(false);
    }, [isConfiguring]);

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
                    {isConfiguring && canPreviewMobile && (
                        <SegmentedControl
                            size="sm"
                            radius="xl"
                            color={color}
                            value={previewMobile ? "mobile" : "desktop"}
                            onChange={(v) => setPreviewMobile(v === "mobile")}
                            data={[
                                {
                                    value: "desktop",
                                    label: (
                                        <span
                                            aria-label="Arrange desktop layout"
                                            style={{ display: "flex" }}
                                        >
                                            <TbDeviceDesktop size={16} />
                                        </span>
                                    ),
                                },
                                {
                                    value: "mobile",
                                    label: (
                                        <span
                                            aria-label="Arrange mobile layout"
                                            style={{ display: "flex" }}
                                        >
                                            <TbDeviceMobile size={16} />
                                        </span>
                                    ),
                                },
                            ]}
                            style={{ flexShrink: 0 }}
                        />
                    )}
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

            {/* Global request loader already covers the wait while (re)loading. */}
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
                        Nothing on this dashboard yet
                    </Text>
                    <Button
                        color={color}
                        leftSection={<FiPlus size={16} />}
                        onClick={() => setIsWidgetsOpen(true)}
                    >
                        Get started
                    </Button>
                </Stack>
            ) : (
                <DashboardGrid
                    widgets={widgets}
                    color={color}
                    isConfiguring={isConfiguring}
                    previewMobile={previewMobile}
                    onLayoutSave={saveLayout}
                    onRemove={removeItem}
                    onEdit={setEditingItemId}
                    onFilterSetValues={setFilterValues}
                    onSaveTabsContainer={saveTabsContainer}
                />
            )}

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
                <EditGroupModal
                    groupId={editingItemId}
                    initialName={parseTextWidgetConfig(editingWidget.config)?.text ?? ""}
                    widgets={widgets}
                    color={color}
                    onClose={closeEditing}
                />
            )}

            {editingItemId &&
                editingWidget &&
                editingWidget.type === WidgetTypes.TabsContainer && (
                    <EditTextWidgetModal
                        itemId={editingItemId}
                        kind="tabsContainer"
                        initialText={
                            parseTabsContainerConfig(editingWidget.config)?.title ?? ""
                        }
                        color={color}
                        onClose={closeEditing}
                        onSave={(id, text) =>
                            saveTabsContainer(id, {
                                title: text,
                                tabs: (
                                    parseTabsContainerConfig(editingWidget.config)?.tabs ?? []
                                ).map((t) => ({ id: t.id, name: t.name })),
                            })
                        }
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
                editingWidget.type !== WidgetTypes.TabsContainer &&
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
                        // A widget deleted in the Library cascades to its placements server-side.
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

    // AppLayout kicks off the initial load of navigationStore.dashboards; this just waits for it.
    const boards = navigationStore.dashboards;
    const hasTrackers = navigationStore.trackers.length > 0;
    const isLoadingBoards = !navigationStore.loaded;
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [isEditOpen, setIsEditOpen] = useState(false);
    const [isDeleteOpen, setIsDeleteOpen] = useState(false);

    const activeBoard = boards.find((b) => b.id === dashboardId);
    useDocumentTitle(activeBoard?.name ?? "Dashboard");

    // Resolve a bare /dashboard to the last board that was opened
    useEffect(() => {
        if (isLoadingBoards || boards.length === 0) return;

        if (activeBoard) {
            localStorage.setItem(LAST_BOARD_KEY, activeBoard.id);
            return;
        }

        // An id that matches no board gets the not-found page below instead
        if (dashboardId) return;

        const remembered = localStorage.getItem(LAST_BOARD_KEY);
        const target = boards.find((b) => b.id === remembered) ?? boards[0];
        navigate(`/dashboard/${target.id}`, { replace: true });
    }, [isLoadingBoards, boards, activeBoard, dashboardId, navigate]);

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

    // Global request loader already covers this fetch.
    if (isLoadingBoards) {
        return null;
    }

    if (boards.length === 0) {
        return (
            <Stack h="100%" gap="md">
                <Group>
                    <SidebarBurger />
                </Group>
                <Stack align="center" gap="md" py={80}>
                    <ThemeIcon
                        size={72}
                        radius="xl"
                        variant="light"
                        color={theme.primaryColor}
                    >
                        {hasTrackers ? (
                            <TbLayoutDashboard size={36} />
                        ) : (
                            <TbLayoutGrid size={36} />
                        )}
                    </ThemeIcon>
                    <Text fw={700} size="xl">
                        {hasTrackers ? "No dashboards yet" : "No trackers yet"}
                    </Text>
                    <Button
                        leftSection={
                            hasTrackers ? (
                                <FiPlus size={16} />
                            ) : (
                                <FiZap size={16} />
                            )
                        }
                        onClick={() =>
                            hasTrackers
                                ? setIsCreateOpen(true)
                                : navigationStore.startTrackerCreate("wizard")
                        }
                    >
                        Get started
                    </Button>
                </Stack>
                {createModal}
            </Stack>
        );
    }

    if (!activeBoard) {
        if (dashboardId) {
            return <NotFound path={`/dashboard/${dashboardId}`} />;
        }
        // The effect above is redirecting to a real board; this is a one-frame gap, not a fetch.
        return null;
    }

    return (
        <>
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
                title="Delete dashboard"
                confirmLabel="Delete"
                message={`"${activeBoard.name}" and all its items will be permanently deleted.`}
                severity="warning"
            />
        </>
    );
});

export default DashboardPage;
