import {
    ActionIcon,
    Badge,
    Button,
    Checkbox,
    Group,
    Menu,
    Pagination,
    Paper,
    ScrollArea,
    Skeleton,
    Stack,
    Table,
    Text,
    Tooltip,
    UnstyledButton,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { CiExport } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { IoMdEye } from "react-icons/io";
import { MdDelete, MdEdit, MdSelectAll } from "react-icons/md";
import { PiFileCsvDuotone } from "react-icons/pi";
import { RiFileListFill } from "react-icons/ri";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { EntryDto } from "../model/EntryDto";
import { downloadBlob } from "../util/BlobDownloader";
import {
    formatDateOnly,
    formatDateTime,
    formatDateTimeFromDate,
    formatTimeSpan,
} from "../util/TypeFormatter";
import ConfirmationDialog from "./ConfirmationDialog";
import EntryDetailsDialog from "./EntryDetailsDialog"; // New import
import EntryFormDialog from "./EntryFormDialog";
import ImportEntriesDialog from "./ImportEntriesDialog";

const gridColumMinWidth = {
    string: "100px",
    number: "100px",
    date: "80px",
    datetime: "160px",
    timespan: "80px",
    bool: "80px",
};

export const renderValue = (type: string | undefined, value: unknown) => {
    if (typeof value === "string") {
        if (type === "date") return formatDateOnly(value);
        if (type === "datetime") return formatDateTime(value);
        if (type === "timespan") return formatTimeSpan(value);
        return value;
    }
    if (typeof value === "number") return value;
    if (typeof value === "boolean") return value ? "Yes" : "No";
    return "";
};

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

    // Bulk selection state
    const [selectedEntryIds, setSelectedEntryIds] = useState<Set<string>>(
        new Set()
    );
    const [isSelectMode, setIsSelectMode] = useState(false);

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
    } = useTracker();

    // Column visibility state - all visible by default
    const [visibleColumns, setVisibleColumns] = useState<
        Record<string, boolean>
    >({});

    // Add a flag to track if visibility has been initialized
    const [visibilityInitialized, setVisibilityInitialized] = useState(false);
    const [isLoadingData, setIsLoadingData] = useState(false);

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

    // Selection handlers
    const toggleEntrySelection = (entryId: string) => {
        setSelectedEntryIds((prev) => {
            const newSet = new Set(prev);
            if (newSet.has(entryId)) {
                newSet.delete(entryId);
            } else {
                newSet.add(entryId);
            }
            return newSet;
        });
    };

    // New handler to toggle all entries
    const toggleSelectAll = () => {
        const allEntryIds = new Set(entries.map((entry) => entry.id));
        const allSelected = selectedEntryIds.size === allEntryIds.size;

        if (allSelected) {
            setSelectedEntryIds(new Set()); // Deselect all
        } else {
            setSelectedEntryIds(allEntryIds); // Select all
        }
    };

    const clearSelection = () => {
        setSelectedEntryIds(new Set());
        setIsSelectMode(false);
    };

    // Check if all entries are selected
    const allEntriesSelected = useMemo(() => {
        return entries.length > 0 && selectedEntryIds.size === entries.length;
    }, [entries, selectedEntryIds]);

    const someEntriesSelected = useMemo(() => {
        return (
            selectedEntryIds.size > 0 && selectedEntryIds.size < entries.length
        );
    }, [entries, selectedEntryIds]);

    // Toggle column visibility
    const toggleColumn = (columnId: string) => {
        setVisibleColumns((prev) => ({
            ...prev,
            [columnId]: !prev[columnId],
        }));
    };

    // Get visible fields only
    const visibleFields = useMemo(() => {
        return fields.filter((field) => visibleColumns[field.id]);
    }, [fields, visibleColumns]);

    // Create table headers from visible fields
    const tableHeaders = useMemo(() => {
        const headers = [];

        if (isSelectMode) {
            headers.push({
                id: "selection",
                label: (
                    <Group>
                        <Checkbox
                            checked={allEntriesSelected}
                            indeterminate={someEntriesSelected}
                            onChange={toggleSelectAll}
                        />
                        <Badge color={tracker.color} variant="filled">
                            {selectedEntryIds.size}
                        </Badge>
                    </Group>
                ),
                minWidth: "100px",
                width: "100px",
            });
        }

        headers.push(
            ...visibleFields.map((field) => ({
                id: field.id,
                label: field.name,
                minWidth: field.type
                    ? gridColumMinWidth[
                          field.type as keyof typeof gridColumMinWidth
                      ]
                    : "auto",
                width: "auto",
            }))
        );

        if (visibleColumns["createdAt"]) {
            headers.push({
                id: "createdAt",
                label: "Created At",
                minWidth: gridColumMinWidth.datetime,
                width: "auto",
            });
        }

        if (visibleColumns["actions"]) {
            headers.push({
                id: "actions",
                label: "Actions",
                minWidth: "125px",
                width: "125px",
            });
        }

        return headers;
    }, [
        visibleFields,
        visibleColumns,
        isSelectMode,
        allEntriesSelected,
        someEntriesSelected,
        toggleSelectAll,
    ]);

    // Create table rows from entries
    const tableRows = useMemo(() => {
        return paginatedEntries.map((entry) => {
            const fieldCells = visibleFields.map((field) => {
                const fieldValue = entry.fieldValues.find(
                    (fv) => fv.fieldId === field.id
                );
                return renderValue(fieldValue?.fieldType, fieldValue?.value);
            });

            return (
                <Table.Tr key={entry.id} h={53}>
                    {/* Selection checkbox */}
                    {isSelectMode && (
                        <Table.Td>
                            <Checkbox
                                checked={selectedEntryIds.has(entry.id)}
                                onChange={() => toggleEntrySelection(entry.id)}
                                size="sm"
                            />
                        </Table.Td>
                    )}

                    {fieldCells.map((cellValue, index) => (
                        <Table.Td key={visibleFields[index].id} maw={300}>
                            <Text size="sm" truncate="end">
                                {cellValue}
                            </Text>
                        </Table.Td>
                    ))}
                    {visibleColumns["createdAt"] && (
                        <Table.Td>
                            <Text size="sm" c="dimmed">
                                {formatDateTimeFromDate(
                                    new Date(entry.createdAt)
                                )}
                            </Text>
                        </Table.Td>
                    )}
                    {visibleColumns["actions"] && (
                        <Table.Td>
                            <Group gap="xs">
                                {/* New ActionIcon for viewing details */}
                                <ActionIcon
                                    variant="outline"
                                    color={tracker.color}
                                    onClick={() => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.ViewDetails
                                        );
                                    }}
                                >
                                    <RiFileListFill size={16} />
                                </ActionIcon>
                                <ActionIcon
                                    variant="outline"
                                    color="green"
                                    onClick={() => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.UpdateEntry
                                        );
                                    }}
                                >
                                    <MdEdit size={16} />
                                </ActionIcon>
                                <ActionIcon
                                    variant="outline"
                                    color="red"
                                    onClick={() => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteEntry
                                        );
                                    }}
                                >
                                    <MdDelete size={16} />
                                </ActionIcon>
                            </Group>
                        </Table.Td>
                    )}
                </Table.Tr>
            );
        });
    }, [
        paginatedEntries,
        visibleFields,
        visibleColumns,
        isSelectMode,
        selectedEntryIds,
        tracker.color,
    ]);

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

    // Initialize column visibility when fields are loaded (only once)
    useEffect(() => {
        if (fields.length > 0 && !visibilityInitialized) {
            const initialVisibility: Record<string, boolean> = {};
            fields.forEach((field) => {
                initialVisibility[field.id] = true;
            });
            // Always show these system columns
            initialVisibility["createdAt"] = true;
            initialVisibility["actions"] = true;

            setVisibleColumns(initialVisibility);
            setVisibilityInitialized(true);
        }
    }, [fields, visibilityInitialized]);

    return (
        <>
            <Skeleton visible={isLoadingData} h={"100vh"}>
                <Stack gap="md">
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

                            {isSelectMode && (
                                <ActionIcon
                                    variant="outline"
                                    color="red"
                                    size={"lg"}
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
                                size={"lg"}
                            >
                                <MdSelectAll size={18} />
                            </ActionIcon>

                            <Menu
                                shadow="md"
                                position="bottom-end"
                                closeOnItemClick={false}
                                width={200}
                            >
                                <Menu.Target>
                                    <ActionIcon
                                        variant="outline"
                                        color={tracker.color}
                                        size={"lg"}
                                        disabled={fields.length === 0}
                                    >
                                        <IoMdEye size={18} />
                                    </ActionIcon>
                                </Menu.Target>

                                <Menu.Dropdown>
                                    {fields.map((field) => (
                                        <Menu.Item key={field.id} p={0}>
                                            <UnstyledButton
                                                p="xs"
                                                w="100%"
                                                onClick={() =>
                                                    toggleColumn(field.id)
                                                }
                                            >
                                                <Group justify="space-between">
                                                    <Text
                                                        className="wrapped-text"
                                                        size="sm"
                                                        maw={"70%"}
                                                    >
                                                        {field.name}
                                                    </Text>
                                                    <Checkbox
                                                        size="sm"
                                                        color={tracker.color}
                                                        checked={
                                                            visibleColumns[
                                                                field.id
                                                            ] || false
                                                        }
                                                        onChange={() => {}}
                                                    />
                                                </Group>
                                            </UnstyledButton>
                                        </Menu.Item>
                                    ))}

                                    <Menu.Divider />

                                    <Menu.Item p={0}>
                                        <UnstyledButton
                                            p="xs"
                                            w="100%"
                                            onClick={() =>
                                                toggleColumn("createdAt")
                                            }
                                        >
                                            <Group justify="space-between">
                                                <Text size="sm">
                                                    Created At
                                                </Text>
                                                <Checkbox
                                                    size="sm"
                                                    color={tracker.color}
                                                    checked={
                                                        visibleColumns[
                                                            "createdAt"
                                                        ] || false
                                                    }
                                                    onChange={() => {}}
                                                    tabIndex={-1}
                                                />
                                            </Group>
                                        </UnstyledButton>
                                    </Menu.Item>

                                    <Menu.Item p={0}>
                                        <UnstyledButton
                                            p="xs"
                                            w="100%"
                                            onClick={() =>
                                                toggleColumn("actions")
                                            }
                                        >
                                            <Group justify="space-between">
                                                <Text size="sm">Actions</Text>
                                                <Checkbox
                                                    color={tracker.color}
                                                    size="sm"
                                                    checked={
                                                        visibleColumns[
                                                            "actions"
                                                        ] || false
                                                    }
                                                    onChange={() => {}}
                                                    tabIndex={-1}
                                                />
                                            </Group>
                                        </UnstyledButton>
                                    </Menu.Item>
                                </Menu.Dropdown>
                            </Menu>
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

                    <ScrollArea flex={1}>
                        {entries.length > 0 && !isLoadingData ? (
                            <Table.ScrollContainer minWidth={0}>
                                <Table
                                    striped
                                    stickyHeader
                                    highlightOnHover
                                    withColumnBorders
                                    withTableBorder
                                    verticalSpacing={"sm"}
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            {tableHeaders.map((header) => (
                                                <Table.Th
                                                    key={header.id}
                                                    miw={header.minWidth}
                                                    w={header.width}
                                                >
                                                    {typeof header.label ===
                                                    "string" ? (
                                                        <Text
                                                            fw={600}
                                                            size="sm"
                                                        >
                                                            {header.label}
                                                        </Text>
                                                    ) : (
                                                        header.label
                                                    )}
                                                </Table.Th>
                                            ))}
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>{tableRows}</Table.Tbody>
                                </Table>
                            </Table.ScrollContainer>
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
            {/* Single entry delete dialog */}
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

            {/* Bulk delete dialog */}
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
