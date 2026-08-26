import {
    Button,
    Group,
    Menu,
    Modal,
    SimpleGrid,
    Select,
    Stack,
    Text,
    ThemeIcon,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { MdAdd } from "react-icons/md";
import { TbChartHistogram, TbLayoutGrid, TbTable } from "react-icons/tb";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import Header from "../../../shared/components/Header";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateEntriesWidgetForm } from "../components/CreateEntriesWidgetForm";
import { CreateWidgetForm } from "../components/CreateWidgetForm";
import { EntriesWidgetLibraryCard } from "../components/EntriesWidgetLibraryCard";
import RenameEntriesWidgetDialog from "../components/RenameEntriesWidgetDialog";
import RenameWidgetDialog from "../components/RenameWidgetDialog";
import { WidgetCard } from "../components/WidgetCard";
import { WidgetsProvider, useWidgets } from "../context/WidgetsContext";
import { EntriesWidgetDefinitionDto, WidgetDto } from "../types/WidgetDto";

type PendingDelete =
    | { kind: "widget"; widget: WidgetDto }
    | { kind: "entries"; entriesWidget: EntriesWidgetDefinitionDto };

function WidgetsContent() {
    const theme = useMantineTheme();
    const { widgets, entriesWidgets, isLoading, refresh, createWidget, createEntriesWidget, deleteWidget, deleteEntriesWidget } =
        useWidgets();

    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerFilter, setTrackerFilter] = useState<string | null>(null);
    const [createKind, setCreateKind] = useState<"chart" | "entries" | null>(null);
    const [editingWidget, setEditingWidget] = useState<WidgetDto | null>(null);
    const [editingEntriesWidget, setEditingEntriesWidget] = useState<EntriesWidgetDefinitionDto | null>(null);
    const [pendingDelete, setPendingDelete] = useState<PendingDelete | null>(null);

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

    const isEmpty = !isLoading && filteredWidgets.length === 0 && filteredEntriesWidgets.length === 0;

    const handleDeleteConfirm = async () => {
        if (!pendingDelete) return;
        if (pendingDelete.kind === "widget") {
            await deleteWidget(pendingDelete.widget.id);
        } else {
            await deleteEntriesWidget(pendingDelete.entriesWidget.id);
        }
        setPendingDelete(null);
    };

    return (
        <Stack h="100%" gap="md">
            <Group w="100%" justify="space-between" wrap="wrap">
                <Group gap="sm">
                    <ThemeIcon size={36} radius="md" variant="light" color={theme.primaryColor}>
                        <TbLayoutGrid size={20} />
                    </ThemeIcon>
                    <Title order={2}>Widget Library</Title>
                </Group>
                <Header color={theme.primaryColor} />
            </Group>

            <Group justify="space-between" wrap="wrap">
                <Select
                    placeholder="All trackers"
                    data={trackers.map((t) => ({ value: t.id, label: t.name }))}
                    value={trackerFilter}
                    onChange={setTrackerFilter}
                    clearable
                    searchable
                    w={260}
                />

                <Menu position="bottom-end">
                    <Menu.Target>
                        <Button leftSection={<MdAdd size={18} />}>Add widget</Button>
                    </Menu.Target>
                    <Menu.Dropdown>
                        <Menu.Item
                            leftSection={<TbChartHistogram size={16} />}
                            onClick={() => setCreateKind("chart")}
                        >
                            New chart
                        </Menu.Item>
                        <Menu.Item
                            leftSection={<TbTable size={16} />}
                            onClick={() => setCreateKind("entries")}
                        >
                            New entries table
                        </Menu.Item>
                    </Menu.Dropdown>
                </Menu>
            </Group>

            {isEmpty ? (
                <Stack align="center" gap="md" py={80}>
                    <ThemeIcon size={72} radius="xl" variant="light" color={theme.primaryColor}>
                        <TbLayoutGrid size={36} />
                    </ThemeIcon>
                    <Text fw={700} size="xl">
                        {trackerFilter ? "No widgets for this tracker yet" : "No widgets yet"}
                    </Text>
                    <Button leftSection={<MdAdd size={16} />} onClick={() => setCreateKind("chart")}>
                        Get Started
                    </Button>
                </Stack>
            ) : (
                <Stack gap="lg">
                    {filteredWidgets.length > 0 && (
                        <Stack gap="sm">
                            <Text fw={600} size="sm" c="dimmed">
                                Charts
                            </Text>
                            <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
                                {filteredWidgets.map((widget) => (
                                    <WidgetCard
                                        key={widget.id}
                                        widget={widget}
                                        color={theme.primaryColor}
                                        onEdit={() => setEditingWidget(widget)}
                                        onDelete={() => setPendingDelete({ kind: "widget", widget })}
                                    />
                                ))}
                            </SimpleGrid>
                        </Stack>
                    )}

                    {filteredEntriesWidgets.length > 0 && (
                        <Stack gap="sm">
                            <Text fw={600} size="sm" c="dimmed">
                                Entries tables
                            </Text>
                            <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
                                {filteredEntriesWidgets.map((entriesWidget) => (
                                    <EntriesWidgetLibraryCard
                                        key={entriesWidget.id}
                                        entriesWidget={entriesWidget}
                                        color={theme.primaryColor}
                                        onEdit={() => setEditingEntriesWidget(entriesWidget)}
                                        onDelete={() =>
                                            setPendingDelete({ kind: "entries", entriesWidget })
                                        }
                                    />
                                ))}
                            </SimpleGrid>
                        </Stack>
                    )}
                </Stack>
            )}

            <Modal
                opened={createKind === "chart"}
                onClose={() => setCreateKind(null)}
                title="New chart"
                size="lg"
                centered
            >
                <CreateWidgetForm
                    onCancel={() => setCreateKind(null)}
                    onSubmit={async (dto) => {
                        await createWidget(dto);
                        setCreateKind(null);
                    }}
                />
            </Modal>

            <Modal
                opened={createKind === "entries"}
                onClose={() => setCreateKind(null)}
                title="New entries table"
                centered
            >
                <CreateEntriesWidgetForm
                    onCancel={() => setCreateKind(null)}
                    onSubmit={async (dto) => {
                        await createEntriesWidget(dto);
                        setCreateKind(null);
                    }}
                />
            </Modal>

            {editingWidget && (
                <RenameWidgetDialog widget={editingWidget} onClose={() => setEditingWidget(null)} />
            )}

            {editingEntriesWidget && (
                <RenameEntriesWidgetDialog
                    entriesWidget={editingEntriesWidget}
                    onClose={() => setEditingEntriesWidget(null)}
                />
            )}

            <ConfirmationDialog
                isOpen={!!pendingDelete}
                onClose={() => setPendingDelete(null)}
                onConfirm={handleDeleteConfirm}
                title={
                    pendingDelete?.kind === "widget"
                        ? `Delete "${pendingDelete.widget.name}"?`
                        : pendingDelete
                        ? `Delete "${pendingDelete.entriesWidget.name || pendingDelete.entriesWidget.trackerName}"?`
                        : undefined
                }
                message="This removes it from every dashboard it's placed on, not just the Library. This can't be undone."
                severity="important"
            />
        </Stack>
    );
}

export default function WidgetsPage() {
    return (
        <WidgetsProvider>
            <WidgetsContent />
        </WidgetsProvider>
    );
}
