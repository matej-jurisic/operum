import {
    ActionIcon,
    Badge,
    Button,
    Checkbox,
    Group,
    Menu,
    Pagination,
    Stack,
    Table,
    Text,
    UnstyledButton,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { IoMdEye } from "react-icons/io";
import { MdDelete, MdEdit, MdSelectAll } from "react-icons/md";
import { PiFileCsvDuotone } from "react-icons/pi";
import { RxCross2 } from "react-icons/rx";
import { useTracker } from "../context/TrackerContext";
import { EntryDto } from "../model/EntryDto";
import { FieldValueDto } from "../model/FieldValueDto";
import { TrackerDto } from "../model/TrackerDto";
import {
    formatDateOnly,
    formatDateTime,
    formatDateTimeFromDate,
    formatTimeSpan,
} from "../util/TypeFormatter";
import ConfirmationDialog from "./ConfirmationDialog";
import EntryFormDialog from "./EntryFormDialog";
import ImportEntriesDialog from "./ImportEntriesDialog";

interface EntriesListProps {
    tracker: TrackerDto;
}

const gridColumnSizes = {
    string: "auto",
    number: "100px",
    date: "80px",
    datetime: "160px",
    timespan: "80px",
    bool: "80px",
};

const renderValue = (v: FieldValueDto) => {
    if (v.value === null) return "";
    if (typeof v.value === "string") {
        if (v.fieldType === "date") return formatDateOnly(v.value);
        if (v.fieldType === "datetime") return formatDateTime(v.value);
        if (v.fieldType === "timespan") return formatTimeSpan(v.value);
        return v.value;
    }
    if (typeof v.value === "number") return v.value;
    if (typeof v.value === "boolean") return v.value.toString();
    return "Unexpected data type";
};

enum OpenDialogType {
    CreateEntry,
    DeleteEntry,
    UpdateEntry,
    ImportEntries,
    BulkDelete,
}

export default function EntriesList(props: EntriesListProps) {
    const [selectedEntry, setSelectedEntry] = useState<EntryDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [currentPage, setCurrentPage] = useState(1);

    // Bulk selection state
    const [selectedEntryIds, setSelectedEntryIds] = useState<Set<string>>(
        new Set()
    );
    const [isSelectMode, setIsSelectMode] = useState(false);

    const { fields, DeleteEntry, DeleteEntries } = useTracker(); // Assuming DeleteEntries exists

    // Column visibility state - all visible by default
    const [visibleColumns, setVisibleColumns] = useState<
        Record<string, boolean>
    >({});

    // Add a flag to track if visibility has been initialized
    const [visibilityInitialized, setVisibilityInitialized] = useState(false);

    const { entries, refreshEntriesIfDirty, refreshFieldsIfDirty } =
        useTracker();

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

    const enterSelectMode = () => {
        setIsSelectMode(true);
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
                        <Badge color={props.tracker.color} variant="filled">
                            {selectedEntryIds.size}
                        </Badge>
                    </Group>
                ),
                width: "100px",
            });
        }

        headers.push(
            ...visibleFields.map((field) => ({
                id: field.id,
                label: field.name,
                width: field.type
                    ? gridColumnSizes[
                          field.type as keyof typeof gridColumnSizes
                      ]
                    : "auto",
            }))
        );

        if (visibleColumns["createdAt"]) {
            headers.push({
                id: "createdAt",
                label: "Created At",
                width: gridColumnSizes.datetime,
            });
        }

        if (visibleColumns["actions"]) {
            headers.push({ id: "actions", label: "Actions", width: "87px" });
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
                return fieldValue ? renderValue(fieldValue) : "Value not set.";
            });

            return (
                <Table.Tr
                    key={entry.id}
                    h={43}
                    bg={
                        selectedEntryIds.has(entry.id)
                            ? `${props.tracker.color}.0`
                            : undefined
                    }
                >
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
                        <Table.Td key={visibleFields[index].id}>
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
        props.tracker.color,
    ]);

    // Load data on component mount
    useEffect(() => {
        const loadData = async () => {
            await refreshEntriesIfDirty();
            await refreshFieldsIfDirty();
        };
        loadData();
    }, [refreshEntriesIfDirty, refreshFieldsIfDirty]);

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
            <Stack gap="md">
                <Group justify="space-between" w="100%">
                    <Group>
                        <Button
                            color={props.tracker.color}
                            leftSection={<FiPlus size={18} />}
                            onClick={() => {
                                if (fields.length === 0) return;
                                setOpenDialogType(OpenDialogType.CreateEntry);
                            }}
                        >
                            Create Entry
                        </Button>
                        <Button
                            onClick={() => {
                                if (fields.length === 0) return;
                                setOpenDialogType(OpenDialogType.ImportEntries);
                            }}
                            color={props.tracker.color}
                            leftSection={<PiFileCsvDuotone size={24} />}
                        >
                            Import Entries
                        </Button>

                        {/* Bulk actions */}
                        {!isSelectMode ? (
                            <Button
                                variant="outline"
                                color={props.tracker.color}
                                leftSection={<MdSelectAll size={18} />}
                                onClick={enterSelectMode}
                                disabled={entries.length === 0}
                            >
                                Select
                            </Button>
                        ) : (
                            <Group>
                                <Button
                                    variant="outline"
                                    leftSection={<RxCross2 size={18} />}
                                    color="gray"
                                    onClick={clearSelection}
                                >
                                    Cancel
                                </Button>
                                <Button
                                    variant="outline"
                                    color="red"
                                    leftSection={<MdDelete size={18} />}
                                    onClick={() =>
                                        setOpenDialogType(
                                            OpenDialogType.BulkDelete
                                        )
                                    }
                                    disabled={selectedEntryIds.size === 0}
                                >
                                    Delete Selected
                                </Button>
                            </Group>
                        )}
                    </Group>

                    <Menu
                        shadow="md"
                        position="bottom-end"
                        closeOnItemClick={false}
                        width={200}
                    >
                        <Menu.Target>
                            <Button
                                variant="outline"
                                color={props.tracker.color}
                                leftSection={<IoMdEye size={18} />}
                            >
                                Show/Hide Columns
                            </Button>
                        </Menu.Target>

                        <Menu.Dropdown>
                            {fields.map((field) => (
                                <Menu.Item key={field.id} p={0}>
                                    <UnstyledButton
                                        p="xs"
                                        w="100%"
                                        onClick={() => toggleColumn(field.id)}
                                    >
                                        <Group justify="space-between">
                                            <Text size="sm">{field.name}</Text>
                                            <Checkbox
                                                size="sm"
                                                checked={
                                                    visibleColumns[field.id] ||
                                                    false
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
                                    onClick={() => toggleColumn("createdAt")}
                                >
                                    <Group justify="space-between">
                                        <Text size="sm">Created At</Text>
                                        <Checkbox
                                            size="sm"
                                            checked={
                                                visibleColumns["createdAt"] ||
                                                false
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
                                    onClick={() => toggleColumn("actions")}
                                >
                                    <Group justify="space-between">
                                        <Text size="sm">Actions</Text>
                                        <Checkbox
                                            size="sm"
                                            checked={
                                                visibleColumns["actions"] ||
                                                false
                                            }
                                            onChange={() => {}}
                                            tabIndex={-1}
                                        />
                                    </Group>
                                </UnstyledButton>
                            </Menu.Item>
                        </Menu.Dropdown>
                    </Menu>
                </Group>

                {entries.length > 0 ? (
                    <Table.ScrollContainer minWidth={0}>
                        <Table
                            striped
                            highlightOnHover
                            withTableBorder
                            withColumnBorders
                            highlightOnHoverColor={`${props.tracker.color}.0`}
                        >
                            <Table.Thead>
                                <Table.Tr>
                                    {tableHeaders.map((header) => (
                                        <Table.Th
                                            key={header.id}
                                            w={header.width}
                                            miw={header.width}
                                        >
                                            {typeof header.label ===
                                            "string" ? (
                                                <Text fw={600} size="sm">
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
                ) : (
                    <Text size="lg" c="dimmed" ta="center" py="xl">
                        No entries found. Create your first entry to get
                        started.
                    </Text>
                )}

                {totalPages > 1 && (
                    <Group justify="center">
                        <Pagination
                            value={currentPage}
                            onChange={setCurrentPage}
                            total={totalPages}
                            siblings={0}
                            color={props.tracker.color}
                            size="md"
                        />
                    </Group>
                )}
            </Stack>

            {/* Single entry delete dialog */}
            {selectedEntry && openDialogType === OpenDialogType.DeleteEntry && (
                <ConfirmationDialog
                    isOpen={selectedEntry !== undefined}
                    onClose={() => setSelectedEntry(undefined)}
                    onConfirm={async () => {
                        await DeleteEntry(props.tracker.id, selectedEntry.id);
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
                            props.tracker.id,
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
                    tracker={props.tracker}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                />
            )}

            {openDialogType === OpenDialogType.UpdateEntry && selectedEntry && (
                <EntryFormDialog
                    tracker={props.tracker}
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
                    tracker={props.tracker}
                />
            )}
        </>
    );
}
