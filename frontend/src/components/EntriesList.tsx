import {
    ActionIcon,
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
import { MdDelete, MdEdit } from "react-icons/md";
import { PiFileCsvDuotone } from "react-icons/pi";
import api from "../api/api";
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

const DeleteEntry = async (trackerId: string, entryId: string) => {
    await api.delete(`/trackers/${trackerId}/entries/${entryId}`);
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
}

export default function EntriesList(props: EntriesListProps) {
    const [selectedEntry, setSelectedEntry] = useState<EntryDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [currentPage, setCurrentPage] = useState(1);
    const { fields } = useTracker();

    // Column visibility state - all visible by default
    const [visibleColumns, setVisibleColumns] = useState<
        Record<string, boolean>
    >({});

    const {
        entries,
        refreshEntriesIfDirty,
        refreshEntries,
        markAnalyticsDirty,
    } = useTracker();

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

    const [isLoading, setIsLoading] = useState(true);

    // Initialize column visibility when fields change
    useEffect(() => {
        const initialVisibility: Record<string, boolean> = {};
        fields.forEach((field) => {
            initialVisibility[field.id] = true;
        });
        // Always show these system columns
        initialVisibility["createdAt"] = true;
        initialVisibility["actions"] = true;
        setVisibleColumns(initialVisibility);
    }, [fields]);

    useEffect(() => {
        const loadEntries = async () => {
            setIsLoading(true);
            await refreshEntriesIfDirty();
            setIsLoading(false);
        };
        loadEntries();
    }, []);

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
        const headers = [
            ...visibleFields.map((field) => ({
                id: field.id,
                label: field.name,
                width: field.type
                    ? gridColumnSizes[
                          field.type as keyof typeof gridColumnSizes
                      ]
                    : "auto",
            })),
        ];

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
    }, [visibleFields, visibleColumns]);

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
                <Table.Tr key={entry.id} h={43}>
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
    }, [paginatedEntries, visibleFields, visibleColumns]);

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

                {!isLoading && entries.length > 0 ? (
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
                                            <Text fw={600} size="sm">
                                                {header.label}
                                            </Text>
                                        </Table.Th>
                                    ))}
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>{tableRows}</Table.Tbody>
                        </Table>
                    </Table.ScrollContainer>
                ) : !isLoading ? (
                    <Text size="lg" c="dimmed" ta="center" py="xl">
                        No entries found. Create your first entry to get
                        started.
                    </Text>
                ) : null}

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

            {selectedEntry && openDialogType === OpenDialogType.DeleteEntry && (
                <ConfirmationDialog
                    isOpen={selectedEntry !== undefined}
                    onClose={() => setSelectedEntry(undefined)}
                    onConfirm={async () => {
                        await DeleteEntry(props.tracker.id, selectedEntry.id);
                        refreshEntries();
                        markAnalyticsDirty();
                        setSelectedEntry(undefined);
                    }}
                    severity="warning"
                    message="Are you sure you want to delete this entry?"
                />
            )}

            {openDialogType === OpenDialogType.CreateEntry && (
                <EntryFormDialog
                    tracker={props.tracker}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                    onEntrySaved={async () => {
                        markAnalyticsDirty();
                        refreshEntries();
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
                    onEntrySaved={async () => {
                        markAnalyticsDirty();
                        refreshEntries();
                        setOpenDialogType(undefined);
                    }}
                />
            )}
            {openDialogType === OpenDialogType.ImportEntries && (
                <ImportEntriesDialog
                    onClose={() => setOpenDialogType(undefined)}
                    trackerId={props.tracker.id}
                    onImport={async () => {
                        markAnalyticsDirty();
                        refreshEntries();
                        setOpenDialogType(undefined);
                    }}
                />
            )}
        </>
    );
}
