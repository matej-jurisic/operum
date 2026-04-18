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
import { TbRefresh } from "react-icons/tb";
import { PiFileCsvDuotone } from "react-icons/pi";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { downloadBlob } from "../../../shared/utils/BlobDownloader";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../../views/context/ViewsContext";
import { entriesController } from "../api/entriesController";
import { useEntries } from "../context/EntriesContext";
import { EntryDto } from "../types/EntryDto";
import { ColumnVisibilityMenu } from "./ColumnVisibiltyMenu";
import { EntriesCards } from "./EntriesCards";
import { EntriesTable } from "./EntriesTable";
import EntryDetailsDialog from "./EntryDetailsDialog";
import EntryFormDialog from "./EntryFormDialog";
import ImportEntriesDialog from "./ImportEntriesDialog";

enum OpenDialogType {
    CreateEntry,
    DeleteEntry,
    UpdateEntry,
    DuplicateEntry,
    ImportEntries,
    BulkDelete,
    ViewDetails,
    ExportEntries,
}

const ExportCsv = async (trackerId: string, viewIds?: string[]) => {
    const response = await entriesController.exportCsv(trackerId, viewIds);

    downloadBlob(
        new Blob([response.data]),
        `tracker-export.csv`,
        response.headers["content-disposition"]
    );
};

export default function Entries() {
    const [selectedEntry, setSelectedEntry] = useState<EntryDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    const { tracker, selectedViewIds, canEditData } = useTracker();
    const { refreshFieldsIfDirty, fields } = useFields();
    const {
        entries,
        refreshEntries,
        isSelectMode,
        selectedEntryIds,
        setIsSelectMode,
        allEntriesSelected,
        toggleSelectAll,
        clearSelection,
        page,
        pageSize,
        totalCount,
        goToPage,
    } = useEntries();
    const { views } = useViews();
    const { deleteEntry, deleteEntries, recalculateEntries } = useTrackerOperations();

    const [isLoadingData, setIsLoadingData] = useState(false);

    // Check if mobile
    const isMobile = useMediaQuery("(max-width: 768px)");

    const totalPages = Math.ceil(totalCount / pageSize);

    const message = useMemo(() => {
        if (totalCount === 0) return "";
        const from = pageSize * (page - 1) + 1;
        const to = Math.min(totalCount, pageSize * page);
        return `Showing ${from} – ${to} of ${totalCount}`;
    }, [page, pageSize, totalCount]);

    const viewName = useMemo(() => {
        const activeViews = views.filter((x) => selectedViewIds.includes(x.id));
        return activeViews.length > 0
            ? activeViews.map((v) => v.name).join(", ")
            : undefined;
    }, [views, selectedViewIds]);

    // Load data on component mount or view change — always reset to page 1
    useEffect(() => {
        const loadData = async () => {
            setIsLoadingData(true);
            await refreshEntries(selectedViewIds, 1);
            await refreshFieldsIfDirty();
            setIsLoadingData(false);
        };
        loadData();
    }, [selectedViewIds]);

    return (
        <>
            <Skeleton visible={isLoadingData} h={"100%"}>
                <Stack gap="md" h={"100%"}>
                    <Group justify="space-between" w="100%">
                        <Group>
                            {canEditData && (
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
                            )}
                        </Group>
                        <Group justify="flex-end" wrap="nowrap">
                            {/* Bulk actions */}

                            {canEditData && isSelectMode && !isMobile && fields.some(f => f.isCalculated) && (
                                <Tooltip label="Rerun calculated fields">
                                    <ActionIcon
                                        variant="outline"
                                        color="violet"
                                        size="lg"
                                        onClick={() => recalculateEntries(Array.from(selectedEntryIds))}
                                        disabled={selectedEntryIds.size === 0}
                                    >
                                        <TbRefresh size={18} />
                                    </ActionIcon>
                                </Tooltip>
                            )}

                            {canEditData && isSelectMode && !isMobile && (
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
                            )}

                            <ActionIcon
                                variant={isSelectMode ? "filled" : "outline"}
                                color={tracker.color}
                                onClick={() => setIsSelectMode((prev) => !prev)}
                                disabled={entries.length === 0}
                                size="lg"
                            >
                                <MdSelectAll size={18} />
                            </ActionIcon>
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
                        </Group>
                    </Group>

                    {isSelectMode && isMobile && (
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
                            {isMobile && (
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
                                    {canEditData && fields.some(f => f.isCalculated) && (
                                        <ActionIcon
                                            variant="outline"
                                            color="violet"
                                            size="lg"
                                            onClick={() => recalculateEntries(Array.from(selectedEntryIds))}
                                            disabled={selectedEntryIds.size === 0}
                                        >
                                            <TbRefresh size={18} />
                                        </ActionIcon>
                                    )}
                                    {canEditData && (
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
                                    )}
                                </Group>
                            )}
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
                                    onDuplicate={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.DuplicateEntry
                                        );
                                    }}
                                    onDelete={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteEntry
                                        );
                                    }}
                                    entries={entries}
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
                                    onDuplicate={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.DuplicateEntry
                                        );
                                    }}
                                    onDelete={(entry) => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteEntry
                                        );
                                    }}
                                    entries={entries}
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
                    {totalCount > 0 && (
                        <Stack align="center" gap={"xs"}>
                            <Text size="sm">{message}</Text>
                            <Pagination
                                siblings={0}
                                value={page}
                                onChange={goToPage}
                                total={totalPages}
                                color={tracker.color}
                                size="md"
                            />
                        </Stack>
                    )}
                </Stack>
            </Skeleton>

            {/* Dialogs */}
            {selectedEntry && openDialogType === OpenDialogType.DeleteEntry && (
                <ConfirmationDialog
                    isOpen={selectedEntry !== undefined}
                    onClose={() => setSelectedEntry(undefined)}
                    onConfirm={async () => {
                        await deleteEntry(selectedEntry.id);
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
                        await deleteEntries(Array.from(selectedEntryIds));
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
            {openDialogType === OpenDialogType.DuplicateEntry && selectedEntry && (
                <EntryFormDialog
                    tracker={tracker}
                    title="Duplicate Entry"
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
                        await ExportCsv(tracker.id, selectedViewIds);
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
