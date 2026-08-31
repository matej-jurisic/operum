import {
    Button,
    Group,
    Modal,
    Select,
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
import { useEffect, useMemo, useState } from "react";
import { IconType } from "react-icons";
import { CiFilter } from "react-icons/ci";
import { FiChevronRight, FiPlus, FiPlusSquare } from "react-icons/fi";
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

    const filteredWidgets = useMemo(
        () =>
            trackerFilter
                ? widgets.filter((w) => w.sources.some((s) => s.trackerId === trackerFilter))
                : widgets,
        [widgets, trackerFilter]
    );

    const filteredEntriesWidgets = useMemo(
        () =>
            trackerFilter
                ? entriesWidgets.filter((w) => w.trackerId === trackerFilter)
                : entriesWidgets,
        [entriesWidgets, trackerFilter]
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

    // Only the tabbed list wants the big, fixed-height canvas; the forms are ordinary and
    // look lost stretched to 90vh, so they get a dialog that sizes to their content.
    const isList = panel.kind === "list";
    const isWide =
        panel.kind === "new-chart" ||
        panel.kind === "new-table" ||
        panel.kind === "place-chart" ||
        panel.kind === "place-table";

    const trackerSelect = (
        <Select
            placeholder="All trackers"
            data={trackers.map((t) => ({ value: t.id, label: t.name }))}
            value={trackerFilter}
            onChange={setTrackerFilter}
            clearable
            searchable
            w={240}
        />
    );

    const instantGrid = (options: InstantOption[]) => (
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
            {options.map((option) => (
                <UnstyledButton
                    key={option.key}
                    onClick={() => pickInstant(option.key)}
                    disabled={option.key === "divider" && isAddingDivider}
                    p="md"
                    style={{
                        borderRadius: theme.radius.md,
                        border: `1px solid ${theme.colors.gray[6]}33`,
                    }}
                >
                    <Group wrap="nowrap">
                        <ThemeIcon size={40} radius="md" variant="light" color={color}>
                            <option.icon size={22} />
                        </ThemeIcon>
                        <Text fw={600} style={{ flex: 1 }}>
                            {option.title}
                        </Text>
                        <FiChevronRight size={18} />
                    </Group>
                </UnstyledButton>
            ))}
        </SimpleGrid>
    );

    return (
        <Modal
            opened
            onClose={onClose}
            title={panelTitle(panel)}
            size={isList ? "90%" : isWide ? "lg" : "md"}
            centered
            styles={
                isList
                    ? {
                          content: {
                              height: "90vh",
                              display: "flex",
                              flexDirection: "column",
                          },
                          body: { flex: 1, overflowY: "auto" },
                      }
                    : undefined
            }
        >
            {panel.kind === "list" && (
                <Tabs value={tab} onChange={(value) => setTab(value as TabValue)}>
                    <Tabs.List mb="md">
                        <Tabs.Tab value="charts" leftSection={<TbChartHistogram size={16} />}>
                            Charts
                        </Tabs.Tab>
                        <Tabs.Tab value="tables" leftSection={<TbTable size={16} />}>
                            Tables
                        </Tabs.Tab>
                        <Tabs.Tab value="controls" leftSection={<FiPlusSquare size={16} />}>
                            Controls
                        </Tabs.Tab>
                        <Tabs.Tab value="layout" leftSection={<TbLayoutGrid size={16} />}>
                            Layout
                        </Tabs.Tab>
                    </Tabs.List>

                    <Tabs.Panel value="charts">
                        <Stack gap="md">
                            <Group justify="space-between" wrap="wrap">
                                {trackerSelect}
                                <Button
                                    leftSection={<FiPlus size={16} />}
                                    onClick={() => setPanel({ kind: "new-chart" })}
                                >
                                    New chart
                                </Button>
                            </Group>

                            {!isLoading && filteredWidgets.length === 0 ? (
                                <EmptyState
                                    title="No charts yet"
                                    hint={
                                        trackerFilter
                                            ? "No charts read from this tracker."
                                            : "Build one with New chart -- it's saved here and placed on this board."
                                    }
                                />
                            ) : (
                                <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
                                    {filteredWidgets.map((widget) => (
                                        <WidgetCard
                                            key={widget.id}
                                            widget={widget}
                                            color={color}
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
                                    ))}
                                </SimpleGrid>
                            )}
                        </Stack>
                    </Tabs.Panel>

                    <Tabs.Panel value="tables">
                        <Stack gap="md">
                            <Group justify="space-between" wrap="wrap">
                                {trackerSelect}
                                <Button
                                    leftSection={<FiPlus size={16} />}
                                    onClick={() => setPanel({ kind: "new-table" })}
                                >
                                    New table
                                </Button>
                            </Group>

                            {!isLoading && filteredEntriesWidgets.length === 0 ? (
                                <EmptyState
                                    title="No entries tables yet"
                                    hint={
                                        trackerFilter
                                            ? "No tables read from this tracker."
                                            : "Build one with New table -- it's saved here and placed on this board."
                                    }
                                />
                            ) : (
                                <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
                                    {filteredEntriesWidgets.map((entriesWidget) => (
                                        <EntriesWidgetLibraryCard
                                            key={entriesWidget.id}
                                            entriesWidget={entriesWidget}
                                            color={color}
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
                                    ))}
                                </SimpleGrid>
                            )}
                        </Stack>
                    </Tabs.Panel>

                    <Tabs.Panel value="controls">{instantGrid(CONTROL_OPTIONS)}</Tabs.Panel>
                    <Tabs.Panel value="layout">{instantGrid(LAYOUT_OPTIONS)}</Tabs.Panel>
                </Tabs>
            )}

            {panel.kind === "new-chart" && (
                <CustomAnalyticForm
                    onBack={backToList}
                    onAdd={closeAfter(createAndPlaceWidget)}
                />
            )}

            {panel.kind === "new-table" && (
                <EntriesWidgetForm
                    onBack={backToList}
                    onAdd={closeAfter(createAndPlaceEntriesWidget)}
                />
            )}

            {panel.kind === "place-chart" && (
                <PlaceFromLibraryForm
                    onBack={backToList}
                    presetWidget={panel.widget}
                    onPlaceWidget={closeAfter(placeWidget)}
                    onPlaceEntriesWidget={closeAfter(placeEntriesWidget)}
                />
            )}

            {panel.kind === "place-table" && (
                <PlaceFromLibraryForm
                    onBack={backToList}
                    presetEntriesWidget={panel.entriesWidget}
                    onPlaceWidget={closeAfter(placeWidget)}
                    onPlaceEntriesWidget={closeAfter(placeEntriesWidget)}
                />
            )}

            {panel.kind === "config" && panel.widgetKind === "quickAdd" && (
                <QuickAddTrackerForm onBack={backToList} onAdd={closeAfter(addQuickAddItem)} />
            )}

            {panel.kind === "config" && panel.widgetKind === "view" && (
                <ViewWidgetForm onBack={backToList} onAdd={closeAfter(addViewItem)} />
            )}

            {panel.kind === "config" && panel.widgetKind === "header" && (
                <HeaderWidgetForm onBack={backToList} onAdd={closeAfter(addHeaderItem)} />
            )}

            {panel.kind === "config" && panel.widgetKind === "note" && (
                <NoteWidgetForm onBack={backToList} onAdd={closeAfter(addNoteItem)} />
            )}

            {panel.kind === "edit-chart" && (
                <RenameChartStep
                    widget={panel.widget}
                    onCancel={backToList}
                    onSave={async (dto) => {
                        await updateWidget(panel.widget.id, dto);
                        backToList();
                    }}
                />
            )}

            {panel.kind === "edit-table" && (
                <RenameEntriesStep
                    entriesWidget={panel.entriesWidget}
                    onCancel={backToList}
                    onSave={async (dto) => {
                        await updateEntriesWidget(panel.entriesWidget.id, dto);
                        backToList();
                    }}
                />
            )}

            {(panel.kind === "delete-chart" || panel.kind === "delete-table") && (
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
