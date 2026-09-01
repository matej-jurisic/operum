import {
    Button,
    Group,
    Loader,
    Modal,
    Paper,
    Select,
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
import { CiFilter } from "react-icons/ci";
import { FiChevronRight, FiPlus, FiPlusSquare, FiSearch } from "react-icons/fi";
import { MdOutlineHorizontalRule } from "react-icons/md";
import { TbChartHistogram, TbHeading, TbLayoutGrid, TbNote, TbTable } from "react-icons/tb";
import EmptyState from "../../../shared/components/EmptyState";
import { CustomAnalyticForm } from "../../dashboard/components/CustomAnalyticForm";
import { EntriesWidgetForm } from "../../dashboard/components/EntriesWidgetForm";
import { HeaderWidgetForm } from "../../dashboard/components/HeaderWidgetForm";
import { NoteWidgetForm } from "../../dashboard/components/NoteWidgetForm";
import { PlaceFromLibraryForm } from "../../dashboard/components/PlaceFromLibraryForm";
import { QuickAddTrackerForm } from "../../dashboard/components/QuickAddTrackerForm";
import { ViewWidgetForm } from "../../dashboard/components/ViewWidgetForm";
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

// The one surface for putting anything on the current board. The Charts/Tables tabs list
// reusable Widget Library definitions -- placed on this board by reference, edited or
// deleted in place -- while Controls/Layout are instant widgets with no saved definition,
// configured and added in one go. Replaces the old split of "Add widget" (a kind picker)
// and "Widget Library" (management only) that sent the user through a nested, resizing
// modal just to place a saved widget.
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
    | { kind: "config"; widgetKind: "quickAdd" | "view" | "header" | "note" };

const TAB_META: { value: TabValue; label: string; icon: IconType }[] = [
    { value: "charts", label: "Charts", icon: TbChartHistogram },
    { value: "tables", label: "Tables", icon: TbTable },
    { value: "controls", label: "Controls", icon: FiPlusSquare },
    { value: "layout", label: "Layout", icon: TbLayoutGrid },
];

interface InstantOption {
    key: "quickAdd" | "view" | "header" | "divider" | "note";
    title: string;
    icon: IconType;
}

const CONTROL_OPTIONS: InstantOption[] = [
    { key: "quickAdd", title: "Quick-add button", icon: FiPlusSquare },
    { key: "view", title: "View selector", icon: CiFilter },
];

const LAYOUT_OPTIONS: InstantOption[] = [
    { key: "header", title: "Header", icon: TbHeading },
    { key: "divider", title: "Divider", icon: MdOutlineHorizontalRule },
    { key: "note", title: "Note", icon: TbNote },
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
                : panel.widgetKind === "view"
                ? "Add a view selector"
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
        addViewItem,
        addHeaderItem,
        addDividerItem,
        addNoteItem,
    } = useDashboard();

    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerFilter, setTrackerFilter] = useState<string | null>(null);
    const [search, setSearch] = useState("");
    const [tab, setTab] = useState<TabValue>("charts");
    const [panel, setPanel] = useState<Panel>({ kind: "list" });
    const [isAddingDivider, setIsAddingDivider] = useState(false);
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

    // Every add/place form leaves the modal open on failure (the api layer already said what
    // went wrong, and closing throws away what was filled in) and closes the whole modal on
    // success.
    const closeAfter =
        <T,>(handler: (dto: T) => Promise<void>) =>
        async (dto: T) => {
            await handler(dto);
            onClose();
        };

    const pickInstant = async (key: InstantOption["key"]) => {
        if (key !== "divider") {
            setPanel({ kind: "config", widgetKind: key });
            return;
        }
        setIsAddingDivider(true);
        try {
            await addDividerItem();
            onClose();
        } finally {
            setIsAddingDivider(false);
        }
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

    // The library modal stays a fixed-height canvas for the tabbed list. Add / edit /
    // delete and the "New ..." forms open in a second modal stacked on top, so the list
    // stays visible behind them instead of being replaced.
    const isList = panel.kind === "list";
    const lastSubPanelRef = useRef<Panel>({ kind: "list" });
    if (!isList) {
        lastSubPanelRef.current = panel;
    }
    // While the sub-modal plays its close transition `panel` is already back to "list";
    // keep rendering the last panel's content so the box doesn't empty out mid-fade.
    const subPanel = isList ? lastSubPanelRef.current : panel;
    const isWideSub =
        subPanel.kind === "new-chart" ||
        subPanel.kind === "new-table" ||
        subPanel.kind === "place-chart" ||
        subPanel.kind === "place-table";

    // Search + tracker filter share one row across the Charts and Tables tabs on desktop.
    // On mobile the modal is too narrow for that: search gets its own row, and the tracker
    // filter shares the next row with the tab's "New" button.
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

    // The list is one bordered surface of quiet rows rather than a grid of standalone
    // cards -- easier to scan down when there are a lot of saved widgets.
    const listContainer = (count: number, noun: string, rows: ReactNode) => (
        <Stack gap="xs">
            <Text size="xs" c="dimmed">
                {count} {count === 1 ? noun : `${noun}s`}
            </Text>
            <Paper withBorder radius="md" p={4}>
                <Stack gap={2}>{rows}</Stack>
            </Paper>
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
                        disabled={option.key === "divider" && isAddingDivider}
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
                    // Fixed height so the modal doesn't jump around as the search or
                    // tracker filter changes how many rows are shown. The body itself
                    // never scrolls -- the tab header and toolbar stay put while each
                    // tab's list area scrolls on its own. Full-screen on mobile, where
                    // the height is just whatever the viewport gives us.
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
                    // Unmount inactive panels so the per-panel flex styling below only ever
                    // applies to the visible tab (a kept-but-hidden panel would ignore its
                    // `hidden` state once we set an explicit `display`).
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
                    {/* On mobile the four labels don't fit on one line, so we show just the
                        icon for every tab and the label only for the selected one -- the
                        same treatment as the tracker page. */}
                    <Tabs.List mb="md" grow={!!isMobile}>
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
                                scrollRegion(listContainer(
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
                                scrollRegion(listContainer(
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
                // Lighter overlay so the library stays legible behind the stacked dialog
                // instead of stacking two full-strength scrims.
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

                {subPanel.kind === "config" && subPanel.widgetKind === "view" && (
                    <ViewWidgetForm onBack={backToList} onAdd={closeAfter(addViewItem)} />
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
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async () => {
        setIsSubmitting(true);
        try {
            await onSave({
                name: name.trim() || undefined,
                description: description.trim() || undefined,
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
