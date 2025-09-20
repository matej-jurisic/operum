import {
    ActionIcon,
    Badge,
    Button,
    Group,
    Menu,
    Pagination,
    Paper,
    ScrollArea,
    Skeleton,
    Stack,
    Text,
    Tooltip,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useMemo, useState } from "react";
import { CiExport } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { MdCheck, MdClose, MdDelete, MdSelectAll } from "react-icons/md";
import { PiFileCsvDuotone } from "react-icons/pi";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { EntryDto } from "../model/EntryDto";
import { downloadBlob } from "../util/BlobDownloader";
import { ColumnVisibilityMenu } from "./ColumnVisibiltyMenu";
import ConfirmationDialog from "./ConfirmationDialog";
import { EntriesCards } from "./EntriesCards";
import { EntriesTable } from "./EntriesTable";
import EntryDetailsDialog from "./EntryDetailsDialog";
import EntryFormDialog from "./EntryFormDialog";
import ImportEntriesDialog from "./ImportEntriesDialog";

enum OpenDialogType {
    CreateEntry,
    DeleteEntry,
    UpdateEntry,
    ImportEntries,
    BulkDelete,
    ViewDetails,
    ExportEntries,
}

const ExportCsv = async (trackerId: string, viewId?: string) => {
    const response = await api.get(
        `/trackers/${trackerId}/entries/export-csv`,
        {
            params: viewId ? { viewId } : {},
            responseType: "blob",
        }
    );

    downloadBlob(
        new Blob([response.data]),
        `tracker-export.csv`,
        response.headers["content-disposition"]
    );
};

export default function EntriesList() {
    const [selectedEntry, setSelectedEntry] = useState<EntryDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [currentPage, setCurrentPage] = useState(1);

    const {
        fields,
        entries,
        refreshEntriesIfDirty,
        refreshFieldsIfDirty,
        DeleteEntry,
        DeleteEntries,
        views,
        tracker,
        selectedViewId,
        selectedEntryIds,
        isSelectMode,
        allEntriesSelected,
        toggleSelectAll,
        clearSelection,
        setIsSelectMode,
    } = useTracker();
    const [isLoadingData, setIsLoadingData] = useState(false);

    // Check if mobile
    const isMobile = useMediaQuery("(max-width: 768px)");

    const pageSize = 10;
    const totalPages = useMemo(() => {
        return Math.ceil(entries.length / pageSize);
    }, [entries]);

    const paginatedEntries = useMemo(() => {
        return entries.slice(
            (currentPage - 1) * pageSize,
            currentPage * pageSize
        );
    }, [currentPage, entries]);

    const viewName = useMemo(() => {
        return views.find((x) => x.id === selectedViewId)?.name;
    }, [views, selectedViewId]);

    // Load data on component mount
    useEffect(() => {
        const loadData = async () => {
            setIsLoadingData(true);
            await refreshEntriesIfDirty();
            await refreshFieldsIfDirty();
            setIsLoadingData(false);
        };
        loadData();
    }, [selectedViewId]);

    return (
        <>
            <Skeleton visible={isLoadingData} h={"100%"}>
                <Stack gap="md" h={"100%"}>
                    <Group justify="space-between" w="100%">
                        <Group>
                            <Menu shadow="md" position="bottom-start">
                                <Menu.Target>
                                    <Tooltip
                                        label={
                                            fields.length === 0
                                                ? "Cannot create entry: No fields available"
                                                : ""
                                        }
                                        disabled={fields.length > 0}
                                        withArrow
                                    >
                                        <Button
                                            variant="outline"
                                            color={tracker.color}
                                            disabled={fields.length === 0}
                                            leftSection={<FiPlus size={18} />}
                                        >
                                            Create
                                        </Button>
                                    </Tooltip>
                                </Menu.Target>

                                <Menu.Dropdown>
                                    <Menu.Item
                                        leftSection={<FiPlus size={16} />}
                                        onClick={() =>
                                            setOpenDialogType(
                                                OpenDialogType.CreateEntry
                                            )
                                        }
                                    >
                                        Create Entry
                                    </Menu.Item>
                                    <Menu.Item
                                        leftSection={
                                            <PiFileCsvDuotone size={16} />
                                        }
                                        onClick={() =>
                                            setOpenDialogType(
                                                OpenDialogType.ImportEntries
                                            )
                                        }
                                    >
                                        Import Entries
                                    </Menu.Item>
                                </Menu.Dropdown>
                            </Menu>
                        </Group>
                        <Group justify="flex-end" wrap="nowrap">
                            {/* Bulk actions */}

                            <ColumnVisibilityMenu />

                            <ActionIcon
                                variant="outline"
                                color={tracker.color}
                                size={"lg"}
                                onClick={() =>
                                    setOpenDialogType(
                                        OpenDialogType.ExportEntries
                                    )
                                }
                            >
                                <CiExport size={18} />
                            </ActionIcon>
                            <ActionIcon
                                variant={isSelectMode ? "filled" : "outline"}
                                color={tracker.color}
                                onClick={() => setIsSelectMode((prev) => !prev)}
                                disabled={entries.length === 0}
                                size="lg"
                            >
                                <MdSelectAll size={18} />
                            </ActionIcon>
                        </Group>
                    </Group>

                    {isSelectMode && (
                        <Group justify="space-between" w="100%" align="center">
                            <Group>
                                {isMobile && isSelectMode && (
                                    <Badge
                                        color={tracker.color}
                                        variant="filled"
                                    >
                                        {selectedEntryIds.size} selected
                                    </Badge>
                                )}
                            </Group>
                            <Group>
                                <Button
                                    variant={
                                        allEntriesSelected
                                            ? "filled"
                                            : "outline"
                                    }
                                    color={tracker.color}
                                    leftSection={
                                        allEntriesSelected ? (
                                            <MdClose size={18} />
                                        ) : (
                                            <MdCheck size={18} />
                                        )
                                    }
                                    onClick={toggleSelectAll}
                                >
                                    Select All
                                </Button>
                                <ActionIcon
                                    variant="outline"
                                    color="red"
                                    size="lg"
                                    onClick={() =>
                                        setOpenDialogType(
                                            OpenDialogType.BulkDelete
                                        )
                                    }
                                    disabled={selectedEntryIds.size === 0}
                                >
                                    <MdDelete size={18} />
                                </ActionIcon>
                            </Group>
                        </Group>
                    )}

                    <ScrollArea flex={1}>
                        {entries.length > 0 && !isLoadingData ? (
                            isMobile ? (
                                <EntriesCards
                                    onViewDetails={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.ViewDetails
                                        );
                                    }}
                                    onEdit={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.UpdateEntry
                                        );
                                    }}
                                    onDelete={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteEntry
                                        );
                                    }}
                                    entries={paginatedEntries}
                                />
                            ) : (
                                <EntriesTable
                                    onViewDetails={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.ViewDetails
                                        );
                                    }}
                                    onEdit={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.UpdateEntry
                                        );
                                    }}
                                    onDelete={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteEntry
                                        );
                                    }}
                                    entries={paginatedEntries}
                                />
                            )
                        ) : isLoadingData ? (
                            <></>
                        ) : (
                            <Paper withBorder p="xl" radius="md">
                                <Stack gap="md" align="center">
                                    <Text size="lg" fw={500} c="dimmed">
                                        No Entries Available
                                    </Text>
                                    <Text ta="center" c="dimmed">
                                        Entries will appear here when you create
                                        them.
                                    </Text>
                                </Stack>
                            </Paper>
                        )}
                    </ScrollArea>
                    <Pagination
                        value={currentPage}
                        onChange={setCurrentPage}
                        total={totalPages}
                        siblings={0}
                        color={tracker.color}
                        size="md"
                    />
                </Stack>
            </Skeleton>

            {/* Dialogs */}
            {selectedEntry && openDialogType === OpenDialogType.DeleteEntry && (
                <ConfirmationDialog
                    isOpen={selectedEntry !== undefined}
                    onClose={() => setSelectedEntry(undefined)}
                    onConfirm={async () => {
                        await DeleteEntry(tracker.id, selectedEntry.id);
                        setSelectedEntry(undefined);
                    }}
                    severity="warning"
                    message="Are you sure you want to delete this entry?"
                />
            )}

            {openDialogType === OpenDialogType.BulkDelete && (
                <ConfirmationDialog
                    isOpen={true}
                    onClose={() => setOpenDialogType(undefined)}
                    onConfirm={async () => {
                        await DeleteEntries(
                            tracker.id,
                            Array.from(selectedEntryIds)
                        );
                        clearSelection();
                        setOpenDialogType(undefined);
                    }}
                    severity="warning"
                    message={`Are you sure you want to delete ${
                        selectedEntryIds.size
                    } selected ${
                        selectedEntryIds.size === 1 ? "entry" : "entries"
                    }?`}
                />
            )}

            {openDialogType === OpenDialogType.CreateEntry && (
                <EntryFormDialog
                    tracker={tracker}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                />
            )}

            {openDialogType === OpenDialogType.UpdateEntry && selectedEntry && (
                <EntryFormDialog
                    tracker={tracker}
                    entryId={selectedEntry.id}
                    initialValues={selectedEntry.fieldValues.reduce(
                        (acc, field) => {
                            acc[field.fieldName] = field.value;
                            return acc;
                        },
                        {} as Record<string, unknown>
                    )}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                />
            )}
            {openDialogType === OpenDialogType.ImportEntries && (
                <ImportEntriesDialog
                    onClose={() => setOpenDialogType(undefined)}
                    tracker={tracker}
                />
            )}

            {openDialogType === OpenDialogType.ViewDetails && selectedEntry && (
                <EntryDetailsDialog
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedEntry(undefined);
                    }}
                    entryId={selectedEntry.id}
                    tracker={tracker}
                />
            )}

            {openDialogType === OpenDialogType.ExportEntries && (
                <ConfirmationDialog
                    isOpen
                    onClose={() => setOpenDialogType(undefined)}
                    onConfirm={async () => {
                        await ExportCsv(tracker.id, selectedViewId);
                        setOpenDialogType(undefined);
                    }}
                    title="Export data"
                    message={
                        <Text>
                            Would you like to export entries for{" "}
                            <Text component="span" fw={700} c="blue">
                                {tracker.name}
                            </Text>
                            {viewName && (
                                <>
                                    {" "}
                                    with selected view{" "}
                                    <Text component="span" fw={700} c="green">
                                        {viewName}
                                    </Text>
                                </>
                            )}{" "}
                            to a CSV file?
                        </Text>
                    }
                />
            )}
        </>
    );
}
