import {
    ActionIcon,
    Badge,
    Button,
    Group,
    Menu,
    Modal,
    Pagination,
    ScrollArea,
    Skeleton,
    Stack,
    Text,
    Tooltip,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useMemo, useRef, useState } from "react";
import { CiExport } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { MdCheck, MdClose, MdDelete, MdSelectAll } from "react-icons/md";
import { PiFileCsvDuotone } from "react-icons/pi";
import { TbNotes, TbRefresh } from "react-icons/tb";
import { useNavigate, useParams } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import EmptyState from "../../../shared/components/EmptyState";
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
import { NoteView } from "./NoteView";

enum OpenDialogType {
    CreateEntry,
    DeleteEntry,
    UpdateEntry,
    DuplicateEntry,
    ImportEntries,
    BulkDelete,
    ViewDetails,
    ExportEntries,
    NoteView,
}

const ExportCsv = async (trackerId: string, viewId?: string | null) => {
    const response = await entriesController.exportCsv(trackerId, viewId);

    downloadBlob(
        new Blob([response.data]),
        `tracker-export.csv`,
        response.headers["content-disposition"],
    );
};

interface EntriesProps {
    autoOpenCreate?: boolean;
}

export default function Entries({ autoOpenCreate = false }: EntriesProps) {
    const { trackerId } = useParams();
    const navigate = useNavigate();
    const hasAutoOpened = useRef(false);

    const [selectedEntry, setSelectedEntry] = useState<EntryDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    const { tracker, selectedViewId, canEditData } = useTracker();
    const { refreshFieldsIfDirty, fields } = useFields();
    const {
        entries,
        entriesDirty,
        refreshEntries,
        isSelectMode,
        selectedCount,
        selectAllMatching,
        selectAllMatchingEntries,
        deselectAll,
        getSelection,
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
    const { deleteEntry, deleteEntries, recalculateEntries } =
        useTrackerOperations();

    const [isLoadingData, setIsLoadingData] = useState(false);

    const isMobile = useMediaQuery("(max-width: 768px)");

    const totalPages = Math.ceil(totalCount / pageSize);

    const message = useMemo(() => {
        if (totalCount === 0) return "";
        const from = pageSize * (page - 1) + 1;
        const to = Math.min(totalCount, pageSize * page);
        return `Showing ${from} to ${to} of ${totalCount}`;
    }, [page, pageSize, totalCount]);

    const viewName = useMemo(() => {
        return views.find((x) => x.id === selectedViewId)?.name;
    }, [views, selectedViewId]);

    useEffect(() => {
        if (!entriesDirty) return;
        const loadData = async () => {
            setIsLoadingData(true);
            await refreshEntries(selectedViewId, 1);
            await refreshFieldsIfDirty();
            setIsLoadingData(false);
        };
        loadData();
    }, [entriesDirty]);

    useEffect(() => {
        if (autoOpenCreate && !hasAutoOpened.current) {
            hasAutoOpened.current = true;
            setOpenDialogType(OpenDialogType.CreateEntry);
        }
    }, [autoOpenCreate]);

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
                                                leftSection={
                                                    <FiPlus size={18} />
                                                }
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
                                                    OpenDialogType.CreateEntry,
                                                )
                                            }
                                        >
                                            Create entry
                                        </Menu.Item>
                                        <Menu.Item
                                            leftSection={
                                                <PiFileCsvDuotone size={16} />
                                            }
                                            onClick={() =>
                                                setOpenDialogType(
                                                    OpenDialogType.ImportEntries,
                                                )
                                            }
                                        >
                                            Import entries
                                        </Menu.Item>
                                    </Menu.Dropdown>
                                </Menu>
                            )}
                        </Group>
                        <Group justify="flex-end" wrap="nowrap">
                            {canEditData &&
                                isSelectMode &&
                                !isMobile &&
                                fields.some((f) => f.isCalculated) && (
                                    <Tooltip label="Rerun calculated fields">
                                        <ActionIcon
                                            variant="outline"
                                            color="violet"
                                            size="lg"
                                            onClick={() =>
                                                recalculateEntries(
                                                    getSelection(),
                                                )
                                            }
                                            disabled={selectedCount === 0}
                                            aria-label="Rerun calculated fields"
                                        >
                                            <TbRefresh size={18} />
                                        </ActionIcon>
                                    </Tooltip>
                                )}

                            {canEditData && isSelectMode && !isMobile && (
                                <Tooltip label="Delete selected entries">
                                    <ActionIcon
                                        variant="outline"
                                        color="red"
                                        size="lg"
                                        onClick={() =>
                                            setOpenDialogType(
                                                OpenDialogType.BulkDelete,
                                            )
                                        }
                                        disabled={selectedCount === 0}
                                        aria-label="Delete selected entries"
                                    >
                                        <MdDelete size={18} />
                                    </ActionIcon>
                                </Tooltip>
                            )}

                            <Tooltip label="Select entries">
                                <ActionIcon
                                    variant={isSelectMode ? "filled" : "outline"}
                                    color={tracker.color}
                                    onClick={() => setIsSelectMode((prev) => !prev)}
                                    disabled={entries.length === 0}
                                    size="lg"
                                    aria-label="Select entries"
                                >
                                    <MdSelectAll size={18} />
                                </ActionIcon>
                            </Tooltip>
                            <ColumnVisibilityMenu />

                            <Tooltip label="Export entries">
                                <ActionIcon
                                    variant="outline"
                                    color={tracker.color}
                                    size={"lg"}
                                    onClick={() =>
                                        setOpenDialogType(
                                            OpenDialogType.ExportEntries,
                                        )
                                    }
                                    aria-label="Export entries"
                                >
                                    <CiExport size={18} />
                                </ActionIcon>
                            </Tooltip>

                            <Tooltip label="Note view">
                                <ActionIcon
                                    variant="outline"
                                    color={tracker.color}
                                    size="lg"
                                    onClick={() =>
                                        setOpenDialogType(OpenDialogType.NoteView)
                                    }
                                    aria-label="Note view"
                                >
                                    <TbNotes size={18} />
                                </ActionIcon>
                            </Tooltip>
                        </Group>
                    </Group>

                    <>
                        {isSelectMode && isMobile && (
                            <Group
                                justify="space-between"
                                w="100%"
                                align="center"
                            >
                                <Group>
                                    {isMobile && isSelectMode && (
                                        <Badge
                                            color={tracker.color}
                                            variant="filled"
                                        >
                                            {selectedCount} selected
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
                                            Select all
                                        </Button>
                                        {canEditData &&
                                            fields.some(
                                                (f) => f.isCalculated,
                                            ) && (
                                                <ActionIcon
                                                    variant="outline"
                                                    color="violet"
                                                    size="lg"
                                                    onClick={() =>
                                                        recalculateEntries(
                                                            getSelection(),
                                                        )
                                                    }
                                                    disabled={
                                                        selectedCount === 0
                                                    }
                                                    aria-label="Rerun calculated fields"
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
                                                        OpenDialogType.BulkDelete,
                                                    )
                                                }
                                                disabled={selectedCount === 0}
                                                aria-label="Delete selected entries"
                                            >
                                                <MdDelete size={18} />
                                            </ActionIcon>
                                        )}
                                    </Group>
                                )}
                            </Group>
                        )}

                        {/* Offers selecting the rest once every checkbox on the current page is ticked. */}
                        {isSelectMode &&
                            (selectAllMatching ||
                                (allEntriesSelected &&
                                    totalCount > entries.length)) && (
                                <Group justify="center" gap="xs" w="100%">
                                    <Text size="sm" c="dimmed">
                                        {selectAllMatching
                                            ? `All ${selectedCount} ${
                                                  selectedCount === 1
                                                      ? "entry"
                                                      : "entries"
                                              }${
                                                  viewName
                                                      ? ` in ${viewName}`
                                                      : ""
                                              } are selected.`
                                            : `All ${entries.length} entries on this page are selected.`}
                                    </Text>
                                    <Button
                                        variant="subtle"
                                        size="compact-sm"
                                        color={tracker.color}
                                        onClick={
                                            selectAllMatching
                                                ? deselectAll
                                                : selectAllMatchingEntries
                                        }
                                    >
                                        {selectAllMatching
                                            ? "Clear selection"
                                            : `Select all ${totalCount}`}
                                    </Button>
                                </Group>
                            )}

                        <ScrollArea flex={1}>
                            {entries.length > 0 && !isLoadingData ? (
                                isMobile ? (
                                    <EntriesCards
                                        onViewDetails={(entry) => {
                                            setSelectedEntry(entry);
                                            setOpenDialogType(
                                                OpenDialogType.ViewDetails,
                                            );
                                        }}
                                        onEdit={(entry) => {
                                            setSelectedEntry(entry);
                                            setOpenDialogType(
                                                OpenDialogType.UpdateEntry,
                                            );
                                        }}
                                        onDuplicate={(entry) => {
                                            setSelectedEntry(entry);
                                            setOpenDialogType(
                                                OpenDialogType.DuplicateEntry,
                                            );
                                        }}
                                        onDelete={(entry) => {
                                            setSelectedEntry(entry);
                                            setOpenDialogType(
                                                OpenDialogType.DeleteEntry,
                                            );
                                        }}
                                        entries={entries}
                                    />
                                ) : (
                                    <EntriesTable
                                        onViewDetails={(entry) => {
                                            setSelectedEntry(entry);
                                            setOpenDialogType(
                                                OpenDialogType.ViewDetails,
                                            );
                                        }}
                                        onEdit={(entry) => {
                                            setSelectedEntry(entry);
                                            setOpenDialogType(
                                                OpenDialogType.UpdateEntry,
                                            );
                                        }}
                                        onDuplicate={(entry) => {
                                            setSelectedEntry(entry);
                                            setOpenDialogType(
                                                OpenDialogType.DuplicateEntry,
                                            );
                                        }}
                                        onDelete={(entry) => {
                                            setSelectedEntry(entry);
                                            setOpenDialogType(
                                                OpenDialogType.DeleteEntry,
                                            );
                                        }}
                                        entries={entries}
                                    />
                                )
                            ) : isLoadingData ? (
                                <></>
                            ) : (
                                <EmptyState
                                    title="No entries yet"
                                    hint="Entries are the records you log against this tracker."
                                />
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
                    </>
                </Stack>
            </Skeleton>

            {selectedEntry && openDialogType === OpenDialogType.DeleteEntry && (
                <ConfirmationDialog
                    isOpen={selectedEntry !== undefined}
                    onClose={() => setSelectedEntry(undefined)}
                    onConfirm={async () => {
                        await deleteEntry(selectedEntry.id);
                        setSelectedEntry(undefined);
                    }}
                    severity="warning"
                    title="Delete entry"
                    confirmLabel="Delete"
                    message="Are you sure you want to delete this entry?"
                />
            )}

            {openDialogType === OpenDialogType.BulkDelete && (
                <ConfirmationDialog
                    isOpen={true}
                    onClose={() => setOpenDialogType(undefined)}
                    onConfirm={async () => {
                        await deleteEntries(getSelection());
                        clearSelection();
                        setOpenDialogType(undefined);
                    }}
                    severity="warning"
                    title="Delete entries"
                    confirmLabel="Delete"
                    message={`Are you sure you want to delete ${selectedCount} selected ${
                        selectedCount === 1 ? "entry" : "entries"
                    }?`}
                />
            )}

            {openDialogType === OpenDialogType.CreateEntry && (
                <EntryFormDialog
                    tracker={tracker}
                    onClose={() => {
                        setOpenDialogType(undefined);
                        if (autoOpenCreate) {
                            navigate(`/trackers/${trackerId}/entries`, {
                                replace: true,
                            });
                        }
                    }}
                />
            )}

            {openDialogType === OpenDialogType.UpdateEntry && selectedEntry && (
                <EntryFormDialog
                    tracker={tracker}
                    entryId={selectedEntry.id}
                    initialValues={selectedEntry.fieldValues.reduce(
                        (acc, field) => {
                            acc[field.fieldName] =
                                field.fieldType === "reference"
                                    ? (field.referencedEntryId ?? "")
                                    : field.value;
                            return acc;
                        },
                        {} as Record<string, unknown>,
                    )}
                    referenceLabels={selectedEntry.fieldValues.reduce(
                        (acc, field) => {
                            if (
                                field.fieldType === "reference" &&
                                field.referencedEntryId
                            ) {
                                acc[field.fieldName] =
                                    (field.value as string) ?? "";
                            }
                            return acc;
                        },
                        {} as Record<string, string>,
                    )}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                />
            )}
            {openDialogType === OpenDialogType.DuplicateEntry &&
                selectedEntry && (
                    <EntryFormDialog
                        tracker={tracker}
                        title="Duplicate entry"
                        initialValues={selectedEntry.fieldValues.reduce(
                            (acc, field) => {
                                acc[field.fieldName] = field.value;
                                return acc;
                            },
                            {} as Record<string, unknown>,
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
                    title="Export entries"
                    confirmLabel="Export"
                    message={
                        <Text>
                            Would you like to export entries for{" "}
                            <Text component="span" fw={700}>
                                {tracker.name}
                            </Text>
                            {viewName && (
                                <>
                                    {" "}
                                    with selected view{" "}
                                    <Text component="span" fw={700}>
                                        {viewName}
                                    </Text>
                                </>
                            )}{" "}
                            to a CSV file?
                        </Text>
                    }
                />
            )}

            <Modal
                opened={openDialogType === OpenDialogType.NoteView}
                onClose={() => setOpenDialogType(undefined)}
                title="Note view"
                size="xl"
                centered
                styles={{
                    body: {
                        display: "flex",
                        flexDirection: "column",
                        height: "80vh",
                    },
                }}
            >
                <NoteView
                    onClose={() => {
                        setOpenDialogType(undefined);
                        refreshEntries(selectedViewId);
                    }}
                />
            </Modal>
        </>
    );
}
