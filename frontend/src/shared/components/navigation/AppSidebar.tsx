import {
    closestCenter,
    DndContext,
    DragEndEvent,
    KeyboardSensor,
    PointerSensor,
    useSensor,
    useSensors,
} from "@dnd-kit/core";
import { restrictToParentElement } from "@dnd-kit/modifiers";
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    useSortable,
    verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
    ActionIcon,
    Avatar,
    Box,
    CloseButton,
    Divider,
    Group,
    Kbd,
    Menu,
    NavLink,
    ScrollArea,
    Skeleton,
    Stack,
    Text,
    ThemeIcon,
    Title,
    Tooltip,
    UnstyledButton,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { spotlight } from "@mantine/spotlight";
import { observer } from "mobx-react";
import { createElement, CSSProperties, useState } from "react";
import { FiPlus, FiPlusSquare, FiZap } from "react-icons/fi";
import { GoSun } from "react-icons/go";
import { IoMoonOutline } from "react-icons/io5";
import { RiListOrdered2 } from "react-icons/ri";
import {
    TbCheck,
    TbChevronLeft,
    TbChevronRight,
    TbGripVertical,
    TbLayoutDashboard,
    TbLogout,
    TbPlug,
    TbPlus,
    TbSearch,
    TbSettings,
    TbUser,
} from "react-icons/tb";
import { useLocation, useNavigate } from "react-router-dom";
import useAuth from "../../../features/auth/hooks/useAuth";
import { dashboardController } from "../../../features/dashboard/api/dashboardController";
import { areIntegrationsEnabled } from "../../../features/integrations/config/integrationsFeature";
import { trackersController } from "../../../features/trackers/api/trackersController";
import { resolveTrackerIcon } from "../../constants/TrackerIcons";
import { TrackerFilters } from "../../constants/TrackerFilters";
import globalStore from "../../stores/GlobalStore";
import navigationStore from "../../stores/NavigationStore";

interface Props {
    collapsed: boolean;
    showCollapseToggle: boolean;
    // Shown everywhere the rail is expanded -- on desktop and in the mobile
    // drawer, which has no app header of its own to carry the wordmark.
    showBrand: boolean;
    onToggleCollapse: () => void;
    onNavigate: () => void;
    // Dismisses the drawer on mobile. The close affordance lives here rather than
    // in a top bar -- there is no top bar anymore.
    onClose: () => void;
}

// Every sidebar row -- nav link, collapsed icon button, and reorder handle row --
// renders at this exact height. Keeping it constant across all three is what stops
// the list from jumping when you collapse the rail or toggle reorder mode.
const ROW_HEIGHT = 44;
// The header and search blocks are likewise pinned so the first list item never
// changes vertical position between the collapsed and expanded rail.
const BLOCK_HEIGHT = 40;
// Row icon box + glyph. Sized up together with ROW_HEIGHT so the list breathes
// instead of feeling stuffed.
const ROW_ICON_BOX = 26;
const ROW_ICON_GLYPH = 18;

// The ⌘ / K hints in the search button. Mantine's Kbd is pill-shaped by default;
// pinning width to height keeps each key a clean square.
const KBD_STYLE: CSSProperties = {
    minWidth: 18,
    height: 18,
    padding: 0,
    display: "inline-flex",
    alignItems: "center",
    justifyContent: "center",
    lineHeight: 1,
};

const AppSidebar = observer(
    ({
        collapsed,
        showCollapseToggle,
        showBrand,
        onToggleCollapse,
        onNavigate,
        onClose,
    }: Props) => {
        const theme = useMantineTheme();
        const navigate = useNavigate();
        const location = useLocation();
        const auth = useAuth();
        const { colorScheme, toggleColorScheme } = useMantineColorScheme();

        const isAdmin = globalStore.userHasRole("admin");
        const pathname = location.pathname;
        // Equal padding on both edges of every row group -- the source of the
        // left/right asymmetry was a scrollbar gutter only reserved on the right.
        const px = collapsed ? 0 : "xs";

        const go = (to: string) => {
            navigate(to);
            onNavigate();
        };

        const logout = async () => {
            await auth.logout();
            navigate("/home");
        };

        const isRouteActive = (base: string) =>
            pathname === base || pathname.startsWith(base + "/");

        const trackerItems = navigationStore.trackers.map((t) => ({
            id: t.id,
            label: t.name,
            active: isRouteActive(`/trackers/${t.id}`),
            color: t.color,
            icon: createElement(resolveTrackerIcon(t.icon), {
                size: ROW_ICON_GLYPH,
            }),
            onClick: () => go(`/trackers/${t.id}`),
        }));

        const dashboardItems = navigationStore.dashboards.map((d) => ({
            id: d.id,
            label: d.name,
            active: isRouteActive(`/dashboard/${d.id}`),
            color: d.color,
            icon: createElement(
                d.icon ? resolveTrackerIcon(d.icon) : TbLayoutDashboard,
                { size: ROW_ICON_GLYPH },
            ),
            onClick: () => go(`/dashboard/${d.id}`),
        }));

        const reorderTrackers = async (ids: string[]) => {
            const previous = navigationStore.trackers;
            navigationStore.setTrackers(
                ids
                    .map((id) => previous.find((t) => t.id === id))
                    .filter((t): t is (typeof previous)[number] => !!t),
            );
            try {
                await trackersController.reorderTrackers(
                    ids,
                    TrackerFilters.Accessible,
                );
            } catch {
                navigationStore.setTrackers(previous);
            }
        };

        const reorderDashboards = async (ids: string[]) => {
            const previous = navigationStore.dashboards;
            navigationStore.setDashboards(
                ids
                    .map((id) => previous.find((d) => d.id === id))
                    .filter((d): d is (typeof previous)[number] => !!d),
            );
            try {
                await dashboardController.reorderDashboards(ids);
            } catch {
                navigationStore.setDashboards(previous);
            }
        };

        return (
            <Stack h="100%" gap={0} py="xs">
                {/* Header: the wordmark, plus the collapse toggle on desktop and
                    a close button in the mobile drawer. */}
                {(showBrand || showCollapseToggle) && (
                    <Group
                        justify={
                            collapsed || !showBrand ? "center" : "space-between"
                        }
                        wrap="nowrap"
                        align="center"
                        h={BLOCK_HEIGHT}
                        px={px}
                        mb="xs"
                    >
                        {!collapsed && showBrand && (
                            <UnstyledButton onClick={() => go("/dashboard")}>
                                <Title order={3} c={theme.primaryColor}>
                                    Operum
                                </Title>
                            </UnstyledButton>
                        )}
                        {showCollapseToggle && (
                            <ActionIcon
                                variant="subtle"
                                color="gray"
                                onClick={onToggleCollapse}
                                aria-label={
                                    collapsed
                                        ? "Expand sidebar"
                                        : "Collapse sidebar"
                                }
                            >
                                {collapsed ? (
                                    <TbChevronRight size={18} />
                                ) : (
                                    <TbChevronLeft size={18} />
                                )}
                            </ActionIcon>
                        )}
                        <CloseButton
                            hiddenFrom="sm"
                            size="lg"
                            onClick={onClose}
                            aria-label="Close navigation"
                        />
                    </Group>
                )}

                {/* Search */}
                <Box px={px} mb="xs" h={BLOCK_HEIGHT}>
                    {collapsed ? (
                        <Group justify="center" h="100%">
                            <Tooltip label="Search  ⌘K" position="right" withArrow>
                                <ActionIcon
                                    variant="default"
                                    size="lg"
                                    onClick={spotlight.open}
                                    aria-label="Search"
                                >
                                    <TbSearch size={18} />
                                </ActionIcon>
                            </Tooltip>
                        </Group>
                    ) : (
                        <UnstyledButton
                            onClick={spotlight.open}
                            w="100%"
                            h="100%"
                            style={{
                                border: `1px solid ${
                                    theme.colors.gray[
                                        colorScheme === "dark" ? 7 : 3
                                    ]
                                }`,
                                borderRadius: theme.radius.sm,
                                padding: "0 10px",
                                display: "flex",
                                alignItems: "center",
                            }}
                        >
                            <Group justify="space-between" wrap="nowrap" w="100%">
                                <Group gap="xs" wrap="nowrap">
                                    <TbSearch size={16} />
                                    <Text size="sm" c="dimmed">
                                        Search
                                    </Text>
                                </Group>
                                <Group gap={4} wrap="nowrap">
                                    <Kbd size="xs" style={KBD_STYLE}>
                                        ⌘
                                    </Kbd>
                                    <Kbd size="xs" style={KBD_STYLE}>
                                        K
                                    </Kbd>
                                </Group>
                            </Group>
                        </UnstyledButton>
                    )}
                </Box>

                <Divider my="xs" />

                {/* Lists */}
                <ScrollArea flex={1} type="hover" scrollbarSize={6}>
                    <Box px={px}>
                        <SidebarList
                            title="Dashboards"
                            collapsed={collapsed}
                            loaded={navigationStore.loaded}
                            emptyLabel="No dashboards yet"
                            items={dashboardItems}
                            onReorder={reorderDashboards}
                            onAdd={() => navigationStore.startDashboardCreate()}
                        />
                        <Divider my="xs" />
                        <SidebarList
                            title="Trackers"
                            collapsed={collapsed}
                            loaded={navigationStore.loaded}
                            emptyLabel="No trackers yet"
                            items={trackerItems}
                            onReorder={reorderTrackers}
                            addMenuItems={[
                                {
                                    label: "Guided setup",
                                    icon: <FiZap size={16} />,
                                    onClick: () =>
                                        navigationStore.startTrackerCreate(
                                            "wizard",
                                        ),
                                },
                                {
                                    label: "Create new",
                                    icon: <FiPlus size={16} />,
                                    onClick: () =>
                                        navigationStore.startTrackerCreate(
                                            "blank",
                                        ),
                                },
                                {
                                    label: "Create from template",
                                    icon: <FiPlusSquare size={16} />,
                                    onClick: () =>
                                        navigationStore.startTrackerCreate(
                                            "template",
                                        ),
                                },
                            ]}
                        />
                    </Box>
                </ScrollArea>

                <Divider my="xs" />

                {/* Footer: theme, integrations, profile, admin, and logout folded
                    into one account menu so they cost a single row instead of
                    five. */}
                <Box px={px}>
                    <AccountMenu
                        collapsed={collapsed}
                        userName={globalStore.currentUser?.userName}
                        colorScheme={colorScheme}
                        isAdmin={isAdmin}
                        onToggleColorScheme={toggleColorScheme}
                        onIntegrations={() => go("/integrations")}
                        onProfile={() => go("/profile")}
                        onAdmin={() => go("/admin-panel")}
                        onLogout={logout}
                    />
                </Box>
            </Stack>
        );
    },
);

export default AppSidebar;

// ─── helpers ──────────────────────────────────────────────────────────────────

/**
 * The footer's utility rows (theme, integrations, profile, admin, logout) folded
 * into one, so they cost a single row instead of five.
 */
function AccountMenu({
    collapsed,
    userName,
    colorScheme,
    isAdmin,
    onToggleColorScheme,
    onIntegrations,
    onProfile,
    onAdmin,
    onLogout,
}: {
    collapsed: boolean;
    userName?: string;
    colorScheme: string;
    isAdmin: boolean;
    onToggleColorScheme: () => void;
    onIntegrations: () => void;
    onProfile: () => void;
    onAdmin: () => void;
    onLogout: () => void;
}) {
    return (
        <Menu
            shadow="md"
            position={collapsed ? "right-end" : "top-start"}
            withinPortal
            width={collapsed ? undefined : "target"}
        >
            <Menu.Target>
                {collapsed ? (
                    <Group justify="center" align="center" h={ROW_HEIGHT}>
                        <Tooltip
                            label={userName ?? "Account"}
                            position="right"
                            withArrow
                        >
                            <ActionIcon
                                size="lg"
                                variant="subtle"
                                color="gray"
                                aria-label={userName ?? "Account"}
                            >
                                <Avatar
                                    size={ROW_ICON_BOX}
                                    radius="xl"
                                    color="gray"
                                >
                                    {userName?.[0]?.toUpperCase()}
                                </Avatar>
                            </ActionIcon>
                        </Tooltip>
                    </Group>
                ) : (
                    <UnstyledButton
                        h={ROW_HEIGHT}
                        px="xs"
                        style={{
                            display: "flex",
                            alignItems: "center",
                            gap: 8,
                            width: "100%",
                            borderRadius: "var(--mantine-radius-sm)",
                        }}
                    >
                        <Avatar size={ROW_ICON_BOX} radius="xl" color="gray">
                            {userName?.[0]?.toUpperCase()}
                        </Avatar>
                        <Text size="sm" truncate flex={1}>
                            {userName ?? "Account"}
                        </Text>
                        <TbChevronRight size={16} />
                    </UnstyledButton>
                )}
            </Menu.Target>
            <Menu.Dropdown>
                <Menu.Item
                    closeMenuOnClick={false}
                    leftSection={
                        colorScheme === "dark" ? (
                            <GoSun size={16} />
                        ) : (
                            <IoMoonOutline size={16} />
                        )
                    }
                    onClick={onToggleColorScheme}
                >
                    {colorScheme === "dark" ? "Light theme" : "Dark theme"}
                </Menu.Item>
                {areIntegrationsEnabled && (
                    <Menu.Item
                        leftSection={<TbPlug size={16} />}
                        onClick={onIntegrations}
                    >
                        Integrations
                    </Menu.Item>
                )}
                <Menu.Item
                    leftSection={<TbUser size={16} />}
                    onClick={onProfile}
                >
                    Profile
                </Menu.Item>
                {isAdmin && (
                    <Menu.Item
                        leftSection={<TbSettings size={16} />}
                        onClick={onAdmin}
                    >
                        Admin panel
                    </Menu.Item>
                )}
                <Menu.Divider />
                <Menu.Item
                    color="red"
                    leftSection={<TbLogout size={16} />}
                    onClick={onLogout}
                >
                    Logout
                </Menu.Item>
            </Menu.Dropdown>
        </Menu>
    );
}

interface ListItem {
    id: string;
    label: string;
    active: boolean;
    color?: string;
    icon: React.ReactNode;
    onClick: () => void;
}

/**
 * One navigation row. Expanded it is a Mantine NavLink; collapsed it is a
 * tooltip'd, centered icon button so nothing sits off-axis in the rail.
 */
function NavItem({
    collapsed,
    label,
    icon,
    color,
    active,
    onClick,
}: {
    collapsed: boolean;
    label: string;
    icon: React.ReactNode;
    color?: string;
    active?: boolean;
    onClick: () => void;
}) {
    if (collapsed) {
        return (
            <Group justify="center" align="center" h={ROW_HEIGHT}>
                <Tooltip label={label} position="right" withArrow>
                    <ActionIcon
                        onClick={onClick}
                        size="lg"
                        variant={active ? "light" : "subtle"}
                        color={active ? color : "gray"}
                        aria-label={label}
                    >
                        {icon}
                    </ActionIcon>
                </Tooltip>
            </Group>
        );
    }
    return (
        <NavLink
            label={label}
            active={active}
            color={color}
            leftSection={icon}
            onClick={onClick}
            styles={{
                root: { height: ROW_HEIGHT, minHeight: ROW_HEIGHT },
            }}
        />
    );
}

/** A list row while the lists are in reorder mode: a drag handle + name, no nav. */
function SortableRow({ item }: { item: ListItem }) {
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
        useSortable({ id: item.id });

    const style: CSSProperties = {
        transform: CSS.Translate.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    };

    return (
        <Group
            ref={setNodeRef}
            style={style}
            gap="xs"
            wrap="nowrap"
            align="center"
            h={ROW_HEIGHT}
            px="xs"
        >
            <ActionIcon
                variant="subtle"
                color="gray"
                {...attributes}
                {...listeners}
                style={{ cursor: "grab", touchAction: "none" }}
                aria-label={`Drag to reorder ${item.label}`}
            >
                <TbGripVertical size={18} />
            </ActionIcon>
            <ThemeIcon
                size={ROW_ICON_BOX}
                radius="sm"
                variant="light"
                color={item.color ?? "gray"}
            >
                {item.icon}
            </ThemeIcon>
            <Text size="sm" truncate flex={1}>
                {item.label}
            </Text>
        </Group>
    );
}

function SidebarList({
    title,
    collapsed,
    loaded,
    emptyLabel,
    items,
    onReorder,
    onAdd,
    addMenuItems,
}: {
    title: string;
    collapsed: boolean;
    loaded: boolean;
    emptyLabel: string;
    items: ListItem[];
    onReorder: (ids: string[]) => void;
    onAdd?: () => void;
    addMenuItems?: { label: string; icon: React.ReactNode; onClick: () => void }[];
}) {
    // Each list owns its reorder toggle, sitting beside its add button -- there
    // is no shared rail-level control.
    const [reordering, setReordering] = useState(false);

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates,
        }),
    );

    if (!loaded) {
        return (
            <Stack gap={0}>
                {[0, 1, 2].map((i) => (
                    <Group
                        key={i}
                        h={ROW_HEIGHT}
                        align="center"
                        justify={collapsed ? "center" : "flex-start"}
                        px={collapsed ? 0 : "xs"}
                    >
                        <Skeleton
                            h={collapsed ? 28 : 24}
                            w={collapsed ? 28 : "75%"}
                        />
                    </Group>
                ))}
            </Stack>
        );
    }

    // Reordering needs the expanded rail and at least two rows to move.
    const canReorder = !collapsed && items.length > 1;
    const isReordering = reordering && canReorder;
    const singular = title.replace(/s$/, "").toLowerCase();

    const handleDragEnd = (event: DragEndEvent) => {
        const { active, over } = event;
        if (!over || active.id === over.id) return;
        const ids = items.map((it) => it.id);
        const oldIndex = ids.indexOf(String(active.id));
        const newIndex = ids.indexOf(String(over.id));
        onReorder(arrayMove(ids, oldIndex, newIndex));
    };

    return (
        <Box>
            {!collapsed && (
                <Group
                    justify="space-between"
                    wrap="nowrap"
                    align="center"
                    h={32}
                    pl="sm"
                    pr={4}
                    mb={6}
                >
                    <Text size="sm" fw={700} c="dimmed" tt="uppercase">
                        {title}
                    </Text>
                    <Group gap={4} wrap="nowrap">
                        {canReorder && (
                            <Tooltip
                                label={
                                    reordering
                                        ? "Done reordering"
                                        : `Reorder ${title.toLowerCase()}`
                                }
                                withArrow
                            >
                                <ActionIcon
                                    size="sm"
                                    variant={reordering ? "light" : "default"}
                                    color={reordering ? undefined : "gray"}
                                    onClick={() => setReordering((v) => !v)}
                                    aria-label={
                                        reordering
                                            ? "Done reordering"
                                            : `Reorder ${title.toLowerCase()}`
                                    }
                                >
                                    {reordering ? (
                                        <TbCheck size={14} />
                                    ) : (
                                        <RiListOrdered2 size={14} />
                                    )}
                                </ActionIcon>
                            </Tooltip>
                        )}
                        {!reordering &&
                            (addMenuItems ? (
                                <Menu
                                    shadow="md"
                                    position="bottom-end"
                                    withinPortal
                                >
                                    <Menu.Target>
                                        <ActionIcon
                                            size="sm"
                                            variant="default"
                                            color="gray"
                                            aria-label={`New ${singular}`}
                                        >
                                            <TbPlus size={14} />
                                        </ActionIcon>
                                    </Menu.Target>
                                    <Menu.Dropdown>
                                        {addMenuItems.map((mi) => (
                                            <Menu.Item
                                                key={mi.label}
                                                leftSection={mi.icon}
                                                onClick={mi.onClick}
                                            >
                                                {mi.label}
                                            </Menu.Item>
                                        ))}
                                    </Menu.Dropdown>
                                </Menu>
                            ) : onAdd ? (
                                <Tooltip label={`New ${singular}`} withArrow>
                                    <ActionIcon
                                        size="sm"
                                        variant="default"
                                        color="gray"
                                        onClick={onAdd}
                                        aria-label={`New ${singular}`}
                                    >
                                        <TbPlus size={14} />
                                    </ActionIcon>
                                </Tooltip>
                            ) : null)}
                    </Group>
                </Group>
            )}

            {items.length === 0 ? (
                !collapsed && (
                    <Text size="sm" c="dimmed" px="sm">
                        {emptyLabel}
                    </Text>
                )
            ) : isReordering ? (
                <DndContext
                    sensors={sensors}
                    collisionDetection={closestCenter}
                    onDragEnd={handleDragEnd}
                    modifiers={[restrictToParentElement]}
                >
                    <SortableContext
                        items={items.map((it) => it.id)}
                        strategy={verticalListSortingStrategy}
                    >
                        {items.map((item) => (
                            <SortableRow key={item.id} item={item} />
                        ))}
                    </SortableContext>
                </DndContext>
            ) : (
                items.map((item) => (
                    <NavItem
                        key={item.id}
                        collapsed={collapsed}
                        label={item.label}
                        active={item.active}
                        color={item.color}
                        icon={
                            collapsed ? (
                                item.icon
                            ) : (
                                <ThemeIcon
                                    size={ROW_ICON_BOX}
                                    radius="sm"
                                    variant="light"
                                    color={item.color ?? "gray"}
                                >
                                    {item.icon}
                                </ThemeIcon>
                            )
                        }
                        onClick={item.onClick}
                    />
                ))
            )}
        </Box>
    );
}
