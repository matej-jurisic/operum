import {
    Button,
    Group,
    Loader,
    Modal,
    Paper,
    Select,
    SegmentedControl,
    SimpleGrid,
    Stack,
    Tabs,
    Text,
    Textarea,
    TextInput,
    ThemeIcon,
    UnstyledButton,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { ReactNode, useEffect, useMemo, useRef, useState } from "react";
import { IconType } from "react-icons";
import { FiChevronRight, FiPlus, FiPlusSquare, FiSearch } from "react-icons/fi";
import { MdOutlineHorizontalRule } from "react-icons/md";
import {
    TbAdjustmentsHorizontal,
    TbChartHistogram,
    TbHeading,
    TbLayoutGrid,
    TbLayoutBoardSplit,
    TbLayoutNavbar,
    TbNote,
    TbTable,
} from "react-icons/tb";
import EmptyState from "../../../shared/components/EmptyState";
import {
    GoalDirection,
    GoalDirections,
} from "../../analytics/types/AnalyticDto";
import { CustomAnalyticForm } from "../../dashboard/components/CustomAnalyticForm";
import { EntriesWidgetForm } from "../../dashboard/components/EntriesWidgetForm";
import { HeaderWidgetForm } from "../../dashboard/components/HeaderWidgetForm";
import { NoteWidgetForm } from "../../dashboard/components/NoteWidgetForm";
import { PlaceFromLibraryForm } from "../../dashboard/components/PlaceFromLibraryForm";
import { QuickAddTrackerForm } from "../../dashboard/components/QuickAddTrackerForm";
import { FilterWidgetForm } from "../../dashboard/components/FilterWidgetForm";
import { useDashboard } from "../../dashboard/context/DashboardContext";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useWidgets } from "../context/WidgetsContext";
import {
    EntriesWidgetDefinitionDto,
    UpdateEntriesWidgetDto,
    UpdateWidgetDto,
    WidgetDto,
} from "../types/WidgetDto";
import { EntriesWidgetLibraryCard } from "./EntriesWidgetLibraryCard";
import { WidgetCard } from "./WidgetCard";

interface Props {
    color: string;
    onClose: () => void;
}

// Charts/Tables list reusable Widget Library definitions placed by reference; Controls/Layout are instant widgets with no saved definition.
type TabValue = "charts" | "tables" | "controls" | "layout";

type Panel =
    | { kind: "list" }
    | { kind: "new-chart" }
    | { kind: "new-table" }
    | { kind: "place-chart"; widget: WidgetDto }
    | { kind: "place-table"; entriesWidget: EntriesWidgetDefinitionDto }
    | { kind: "edit-chart"; widget: WidgetDto }
    | { kind: "edit-table"; entriesWidget: EntriesWidgetDefinitionDto }
    | { kind: "delete-chart"; widget: WidgetDto }
    | { kind: "delete-table"; entriesWidget: EntriesWidgetDefinitionDto }
    | {
          kind: "config";
          widgetKind: "quickAdd" | "filter" | "header" | "note";
      };

const TAB_META: { value: TabValue; label: string; icon: IconType }[] = [
    { value: "charts", label: "Charts", icon: TbChartHistogram },
    { value: "tables", label: "Tables", icon: TbTable },
    { value: "controls", label: "Controls", icon: FiPlusSquare },
    { value: "layout", label: "Layout", icon: TbLayoutGrid },
];

interface InstantOption {
    key:
        | "quickAdd"
        | "filter"
        | "header"
        | "divider"
        | "note"
        | "container"
        | "tabsContainer";
    title: string;
    icon: IconType;
}

const CONTROL_OPTIONS: InstantOption[] = [
    { key: "quickAdd", title: "Quick-add button", icon: FiPlusSquare },
    { key: "filter", title: "Filter", icon: TbAdjustmentsHorizontal },
];

const LAYOUT_OPTIONS: InstantOption[] = [
    { key: "header", title: "Header", icon: TbHeading },
    { key: "divider", title: "Divider", icon: MdOutlineHorizontalRule },
    { key: "note", title: "Note", icon: TbNote },
    { key: "container", title: "Container", icon: TbLayoutBoardSplit },
    { key: "tabsContainer", title: "Tabs container", icon: TbLayoutNavbar },
];

function panelTitle(panel: Panel): string {
    switch (panel.kind) {
        case "list":
            return "Widgets";
        case "new-chart":
            return "New chart";
        case "new-table":
            return "New entries table";
        case "place-chart":
        case "place-table":
            return "Add to board";
        case "edit-chart":
            return "Edit widget";
        case "edit-table":
            return "Edit entries table";
        case "delete-chart":
            return "Delete widget";
        case "delete-table":
            return "Delete entries table";
        case "config":
            return panel.widgetKind === "quickAdd"
                ? "Add a quick-add button"
                : panel.widgetKind === "filter"
                ? "Add a filter"
                : panel.widgetKind === "header"
                ? "Add a header"
                : "Add a note";
    }
}

export function WidgetLibraryModal({ color, onClose }: Props) {
    const theme = useMantineTheme();
    const isMobile = useMediaQuery("(max-width: 48em)");
    const {
        widgets,
        entriesWidgets,
        isLoading,
        refresh,
        updateWidget,
        deleteWidget,
        updateEntriesWidget,
        deleteEntriesWidget,
    } = useWidgets();
    const {
        createAndPlaceWidget,
        placeWidget,
        createAndPlaceEntriesWidget,
        placeEntriesWidget,
        addQuickAddItem,
        addFilterItem,
        addHeaderItem,
        addDividerItem,
        addNoteItem,
        addContainerItem,
        addTabsContainerItem,
    } = useDashboard();

    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerFilter, setTrackerFilter] = useState<string | null>(null);
    const [search, setSearch] = useState("");
    const [tab, setTab] = useState<TabValue>("charts");
    const [panel, setPanel] = useState<Panel>({ kind: "list" });
    const [addingInstantKey, setAddingInstantKey] =
        useState<InstantOption["key"] | null>(null);
    const [isDeleting, setIsDeleting] = useState(false);

    useEffect(() => {
        refresh();
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
    }, [refresh]);

    const query = search.trim().toLowerCase();

    const filteredWidgets = useMemo(
        () =>
            widgets
                .filter((w) => !trackerFilter || w.sources.some((s) => s.trackerId === trackerFilter))
                .filter(
                    (w) =>
                        !query ||
                        w.name.toLowerCase().includes(query) ||
                        w.resultType.toLowerCase().includes(query) ||
                        w.sources.some((s) => s.trackerName.toLowerCase().includes(query))
                )
                .sort((a, b) => a.name.localeCompare(b.name)),
        [widgets, trackerFilter, query]
    );

    const filteredEntriesWidgets = useMemo(
        () =>
            entriesWidgets
                .filter((w) => !trackerFilter || w.trackerId === trackerFilter)
                .filter(
                    (w) =>
                        !query ||
                        (w.name ?? "").toLowerCase().includes(query) ||
                        w.trackerName.toLowerCase().includes(query)
                )
                .sort((a, b) =>
                    (a.name || a.trackerName).localeCompare(b.name || b.trackerName)
                ),
        [entriesWidgets, trackerFilter, query]
    );

    const backToList = () => setPanel({ kind: "list" });

    // Leaves the modal open on failure so the filled-in form isn't lost; closes on success.
    const closeAfter =
        <A extends unknown[]>(handler: (...args: A) => Promise<unknown>) =>
        async (...args: A) => {
            await handler(...args);
            onClose();
        };

    const pickInstant = async (key: InstantOption["key"]) => {
        // These carry no configuration to fill in first, so they're placed straight away.
        if (key === "divider" || key === "container" || key === "tabsContainer") {
            setAddingInstantKey(key);
            try {
                await (key === "container"
                    ? addContainerItem()
                    : key === "tabsContainer"
                    ? addTabsContainerItem()
                    : addDividerItem());
                onClose();
            } finally {
                setAddingInstantKey(null);
            }
            return;
        }
        setPanel({ kind: "config", widgetKind: key });
    };

    const handleDelete = async () => {
        setIsDeleting(true);
        try {
            if (panel.kind === "delete-chart") {
                await deleteWidget(panel.widget.id);
            } else if (panel.kind === "delete-table") {
                await deleteEntriesWidget(panel.entriesWidget.id);
            }
            backToList();
        } finally {
            setIsDeleting(false);
        }
    };

    // Add/edit/delete forms open in a second modal stacked on top of the list.
    const isList = panel.kind === "list";
    const lastSubPanelRef = useRef<Panel>({ kind: "list" });
    if (!isList) {
        lastSubPanelRef.current = panel;
    }
    // Keeps rendering the last panel's content during the close transition so the box doesn't empty out mid-fade.
    const subPanel = isList ? lastSubPanelRef.current : panel;
    const isWideSub =
        subPanel.kind === "new-chart" ||
        subPanel.kind === "new-table" ||
        subPanel.kind === "place-chart" ||
        subPanel.kind === "place-table" ||
        (subPanel.kind === "config" && subPanel.widgetKind === "filter");

    const searchInput = (
        <TextInput
            placeholder="Search by name"
            leftSection={<FiSearch size={15} />}
            value={search}
            onChange={(event) => setSearch(event.currentTarget.value)}
            style={{ flex: 1, minWidth: isMobile ? 0 : 200 }}
        />
    );
    const filterSelect = (
        <Select
            placeholder="All trackers"
            data={trackers.map((t) => ({ value: t.id, label: t.name }))}
            value={trackerFilter}
            onChange={setTrackerFilter}
            clearable
            searchable
            w={isMobile ? undefined : 190}
            style={isMobile ? { flex: 1 } : undefined}
        />
    );
    const listToolbar = (newButton: ReactNode) =>
        isMobile ? (
            <Stack gap="sm">
                {searchInput}
                <Group gap="sm" wrap="nowrap" align="center">
                    {filterSelect}
                    {newButton}
                </Group>
            </Stack>
        ) : (
            <Group gap="sm" wrap="wrap" align="center">
                {searchInput}
                {filterSelect}
                {newButton}
            </Group>
        );

    const cardGrid = (count: number, noun: string, cards: ReactNode) => (
        <Stack gap="xs">
            <Text size="xs" c="dimmed">
                {count} {count === 1 ? noun : `${noun}s`}
            </Text>
            <SimpleGrid
                type="container"
                cols={{ base: 1, "460px": 2, "720px": 3 }}
                spacing="sm"
            >
                {cards}
            </SimpleGrid>
        </Stack>
    );

    // The toolbar stays pinned at the top of the tab; only the rows below it scroll.
    const scrollRegion = (children: ReactNode) => (
        <div style={{ flex: 1, minHeight: 0, overflowY: "auto", paddingRight: 4 }}>
            {children}
        </div>
    );

    const instantList = (options: InstantOption[]) => (
        <Paper withBorder radius="md" p={4}>
            <Stack gap={2}>
                {options.map((option) => (
                    <UnstyledButton
                        key={option.key}
                        onClick={() => pickInstant(option.key)}
                        disabled={addingInstantKey === option.key}
                        px="sm"
                        py="xs"
                        style={{ borderRadius: theme.radius.sm, width: "100%" }}
                    >
                        <Group wrap="nowrap" gap="sm">
                            <ThemeIcon size={34} radius="md" variant="light" color={color}>
                                <option.icon size={18} />
                            </ThemeIcon>
                            <Text fw={500} style={{ flex: 1 }}>
                                {option.title}
                            </Text>
                            <FiChevronRight size={18} />
                        </Group>
                    </UnstyledButton>
                ))}
            </Stack>
        </Paper>
    );

    return (
        <>
            <Modal
                opened
                onClose={onClose}
                title="Widgets"
                size={960}
                centered
                fullScreen={isMobile}
                styles={{
                    // Fixed height so the modal doesn't jump around as filtered results change row count.
                    content: {
                        height: isMobile ? "100%" : "min(92vh, 840px)",
                        display: "flex",
                        flexDirection: "column",
                    },
                    body: {
                        flex: 1,
                        minHeight: 0,
                        display: "flex",
                        flexDirection: "column",
                        overflow: "hidden",
                    },
                }}
            >
                <Tabs
                    value={tab}
                    onChange={(value) => setTab(value as TabValue)}
                    // Must unmount inactive panels: a hidden panel with explicit `display` set ignores Mantine's `hidden` state.
                    keepMounted={false}
                    styles={{
                        root: {
                            flex: 1,
                            minHeight: 0,
                            display: "flex",
                            flexDirection: "column",
                        },
                        panel: { flex: 1, minHeight: 0 },
                    }}
                >
                    <Tabs.List mb="md">
                        {TAB_META.map(({ value, label, icon: Icon }) => (
                            <Tabs.Tab
                                key={value}
                                value={value}
                                px={isMobile ? "xs" : undefined}
                                leftSection={<Icon size={16} />}
                            >
                                {(!isMobile || tab === value) && label}
                            </Tabs.Tab>
                        ))}
                    </Tabs.List>

                    <Tabs.Panel value="charts" style={{ display: "flex", flexDirection: "column" }}>
                        <Stack gap="md" style={{ flex: 1, minHeight: 0 }}>
                            {listToolbar(
                                <Button
                                    leftSection={<FiPlus size={16} />}
                                    onClick={() => setPanel({ kind: "new-chart" })}
                                    style={{ flexShrink: 0 }}
                                >
                                    {isMobile ? "New" : "New chart"}
                                </Button>
                            )}

                            {isLoading ? (
                                <Group justify="center" py="xl">
                                    <Loader size="sm" />
                                </Group>
                            ) : filteredWidgets.length === 0 ? (
                                <EmptyState
                                    title="No charts yet"
                                    hint={
                                        query || trackerFilter
                                            ? "No charts match this search."
                                            : "Build one with New chart. It's saved here and placed on this board."
                                    }
                                />
                            ) : (
                                scrollRegion(cardGrid(
                                    filteredWidgets.length,
                                    "chart",
                                    filteredWidgets.map((widget) => (
                                        <WidgetCard
                                            key={widget.id}
                                            widget={widget}
                                            color={color}
                                            isMobile={!!isMobile}
                                            onAdd={() =>
                                                setPanel({ kind: "place-chart", widget })
                                            }
                                            onEdit={() =>
                                                setPanel({ kind: "edit-chart", widget })
                                            }
                                            onDelete={() =>
                                                setPanel({ kind: "delete-chart", widget })
                                            }
                                        />
                                    ))
                                ))
                            )}
                        </Stack>
                    </Tabs.Panel>

                    <Tabs.Panel value="tables" style={{ display: "flex", flexDirection: "column" }}>
                        <Stack gap="md" style={{ flex: 1, minHeight: 0 }}>
                            {listToolbar(
                                <Button
                                    leftSection={<FiPlus size={16} />}
                                    onClick={() => setPanel({ kind: "new-table" })}
                                    style={{ flexShrink: 0 }}
                                >
                                    {isMobile ? "New" : "New table"}
                                </Button>
                            )}

                            {isLoading ? (
                                <Group justify="center" py="xl">
                                    <Loader size="sm" />
                                </Group>
                            ) : filteredEntriesWidgets.length === 0 ? (
                                <EmptyState
                                    title="No entries tables yet"
                                    hint={
                                        query || trackerFilter
                                            ? "No tables match this search."
                                            : "Build one with New table. It's saved here and placed on this board."
                                    }
                                />
                            ) : (
                                scrollRegion(cardGrid(
                                    filteredEntriesWidgets.length,
                                    "table",
                                    filteredEntriesWidgets.map((entriesWidget) => (
                                        <EntriesWidgetLibraryCard
                                            key={entriesWidget.id}
                                            entriesWidget={entriesWidget}
                                            color={color}
                                            isMobile={!!isMobile}
                                            onAdd={() =>
                                                setPanel({
                                                    kind: "place-table",
                                                    entriesWidget,
                                                })
                                            }
                                            onEdit={() =>
                                                setPanel({
                                                    kind: "edit-table",
                                                    entriesWidget,
                                                })
                                            }
                                            onDelete={() =>
                                                setPanel({
                                                    kind: "delete-table",
                                                    entriesWidget,
                                                })
                                            }
                                        />
                                    ))
                                ))
                            )}
                        </Stack>
                    </Tabs.Panel>

                    <Tabs.Panel value="controls" style={{ display: "flex", flexDirection: "column" }}>
                        {scrollRegion(instantList(CONTROL_OPTIONS))}
                    </Tabs.Panel>
                    <Tabs.Panel value="layout" style={{ display: "flex", flexDirection: "column" }}>
                        {scrollRegion(instantList(LAYOUT_OPTIONS))}
                    </Tabs.Panel>
                </Tabs>
            </Modal>

            <Modal
                opened={!isList}
                onClose={backToList}
                title={panelTitle(subPanel)}
                size={isWideSub ? "lg" : "md"}
                centered
                fullScreen={isMobile}
                // Lighter overlay avoids stacking two full-strength scrims over the library.
                overlayProps={{ backgroundOpacity: 0.35 }}
            >
                {subPanel.kind === "new-chart" && (
                    <CustomAnalyticForm
                        onBack={backToList}
                        onAdd={closeAfter(createAndPlaceWidget)}
                    />
                )}

                {subPanel.kind === "new-table" && (
                    <EntriesWidgetForm
                        onBack={backToList}
                        onAdd={closeAfter(createAndPlaceEntriesWidget)}
                    />
                )}

                {subPanel.kind === "place-chart" && (
                    <PlaceFromLibraryForm
                        onBack={backToList}
                        presetWidget={subPanel.widget}
                        onPlaceWidget={closeAfter(placeWidget)}
                        onPlaceEntriesWidget={closeAfter(placeEntriesWidget)}
                    />
                )}

                {subPanel.kind === "place-table" && (
                    <PlaceFromLibraryForm
                        onBack={backToList}
                        presetEntriesWidget={subPanel.entriesWidget}
                        onPlaceWidget={closeAfter(placeWidget)}
                        onPlaceEntriesWidget={closeAfter(placeEntriesWidget)}
                    />
                )}

                {subPanel.kind === "config" && subPanel.widgetKind === "quickAdd" && (
                    <QuickAddTrackerForm onBack={backToList} onAdd={closeAfter(addQuickAddItem)} />
                )}

                {subPanel.kind === "config" && subPanel.widgetKind === "filter" && (
                    <FilterWidgetForm
                        color={color}
                        onBack={backToList}
                        onAdd={closeAfter(addFilterItem)}
                    />
                )}

                {subPanel.kind === "config" && subPanel.widgetKind === "header" && (
                    <HeaderWidgetForm onBack={backToList} onAdd={closeAfter(addHeaderItem)} />
                )}

                {subPanel.kind === "config" && subPanel.widgetKind === "note" && (
                    <NoteWidgetForm onBack={backToList} onAdd={closeAfter(addNoteItem)} />
                )}

                {subPanel.kind === "edit-chart" && (
                    <RenameChartStep
                        widget={subPanel.widget}
                        onCancel={backToList}
                        onSave={async (dto) => {
                            await updateWidget(subPanel.widget.id, dto);
                            backToList();
                        }}
                    />
                )}

                {subPanel.kind === "edit-table" && (
                    <RenameEntriesStep
                        entriesWidget={subPanel.entriesWidget}
                        onCancel={backToList}
                        onSave={async (dto) => {
                            await updateEntriesWidget(subPanel.entriesWidget.id, dto);
                            backToList();
                        }}
                    />
                )}

                {(subPanel.kind === "delete-chart" || subPanel.kind === "delete-table") && (
                    <Stack gap="lg">
                        <Text>
                            This removes it from every dashboard it's placed on, not just the
                            Library. This can't be undone.
                        </Text>
                        <Group justify="flex-end">
                            <Button variant="default" onClick={backToList}>
                                Cancel
                            </Button>
                            <Button color="red" loading={isDeleting} onClick={handleDelete}>
                                Delete
                            </Button>
                        </Group>
                    </Stack>
                )}
            </Modal>
        </>
    );
}

function RenameChartStep({
    widget,
    onCancel,
    onSave,
}: {
    widget: WidgetDto;
    onCancel: () => void;
    onSave: (dto: UpdateWidgetDto) => Promise<void>;
}) {
    const [name, setName] = useState(widget.name);
    const [description, setDescription] = useState(widget.description ?? "");
    const [goalTarget, setGoalTarget] = useState(widget.goalTarget ?? "");
    const [goalDirection, setGoalDirection] = useState<GoalDirection>(
        (widget.goalDirection as GoalDirection) ?? GoalDirections.HigherIsBetter,
    );
    const [isSubmitting, setIsSubmitting] = useState(false);

    const isGoal = widget.resultType === "Goal";

    const handleSubmit = async () => {
        setIsSubmitting(true);
        try {
            await onSave({
                name: name.trim() || undefined,
                description: description.trim() || undefined,
                goalTarget: isGoal ? goalTarget.trim() : undefined,
                goalDirection: isGoal ? goalDirection : undefined,
            });
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <Stack gap="md">
            <TextInput
                label="Name"
                maxLength={100}
                autoFocus
                value={name}
                onChange={(event) => setName(event.currentTarget.value)}
            />
            <Textarea
                label="Description"
                maxLength={500}
                autosize
                minRows={2}
                value={description}
                onChange={(event) => setDescription(event.currentTarget.value)}
            />
            {isGoal && (
                <TextInput
                    label="Target"
                    description="A number, or hh:mm:ss for a duration."
                    maxLength={20}
                    value={goalTarget}
                    onChange={(event) => setGoalTarget(event.currentTarget.value)}
                />
            )}
            {isGoal && (
                <SegmentedControl
                    value={goalDirection}
                    onChange={(value) => setGoalDirection(value as GoalDirection)}
                    data={[
                        { label: "Higher is better", value: GoalDirections.HigherIsBetter },
                        { label: "Lower is better", value: GoalDirections.LowerIsBetter },
                    ]}
                />
            )}
            <Group justify="flex-end" mt="xs">
                <Button variant="default" onClick={onCancel}>
                    Cancel
                </Button>
                <Button loading={isSubmitting} onClick={handleSubmit}>
                    Save
                </Button>
            </Group>
        </Stack>
    );
}

function RenameEntriesStep({
    entriesWidget,
    onCancel,
    onSave,
}: {
    entriesWidget: EntriesWidgetDefinitionDto;
    onCancel: () => void;
    onSave: (dto: UpdateEntriesWidgetDto) => Promise<void>;
}) {
    const [name, setName] = useState(entriesWidget.name);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async () => {
        setIsSubmitting(true);
        try {
            await onSave({ name: name.trim() || undefined });
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <Stack gap="md">
            <TextInput
                label="Name"
                description={`Left blank, the table falls back to "${entriesWidget.trackerName}"`}
                maxLength={100}
                autoFocus
                value={name}
                onChange={(event) => setName(event.currentTarget.value)}
            />
            <Group justify="flex-end" mt="xs">
                <Button variant="default" onClick={onCancel}>
                    Cancel
                </Button>
                <Button loading={isSubmitting} onClick={handleSubmit}>
                    Save
                </Button>
            </Group>
        </Stack>
    );
}
