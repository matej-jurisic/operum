import {
    ActionIcon,
    Button,
    Group,
    Pagination,
    Stack,
    Table,
    Text,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { FiFile, FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { EntryDto } from "../model/EntryDto";
import { FieldValueDto } from "../model/FieldValueDto";
import { TrackerDto } from "../model/TrackerDto";
import {
    formatDateOnly,
    formatDateTime,
    formatTimeSpan,
} from "../util/TypeFormatter";
import ConfirmationDialog from "./ConfirmationDialog";
import EntryFormDialog from "./EntryFormDialog";
import ImportEntriesDialog from "./ImportEntriesDialog";

interface EntriesListProps {
    tracker: TrackerDto;
}

const DeleteEntry = async (trackerId: string, entryId: string) => {
    await api.delete(`/trackers/${trackerId}/entries/${entryId}`);
};

const renderValue = (v: FieldValueDto) => {
    if (v.value === null) return "Value not set.";
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

    const {
        entries,
        refreshEntriesIfDirty,
        refreshEntries,
        markAnalyticsDirty,
    } = useTracker();

    const pageSize = 10; // Increased page size for table format
    const totalPages = useMemo(() => {
        return Math.ceil(entries.length / pageSize);
    }, [entries]);
    
    const paginatedEntries = useMemo(() => {
        return entries.slice(
            (currentPage - 1) * pageSize,
            currentPage * pageSize
        );
    }, [currentPage, entries]);

    useEffect(() => {
        refreshEntriesIfDirty();
    }, []);

    // Create table headers from fields
    const tableHeaders = useMemo(() => {
        return [
            ...fields.map(field => field.name),
            'Created At',
            'Actions'
        ];
    }, [fields]);

    // Create table rows from entries
    const tableRows = useMemo(() => {
        return paginatedEntries.map((entry) => {
            const fieldCells = fields.map(field => {
                const fieldValue = entry.fieldValues.find(fv => fv.fieldId === field.id);
                return fieldValue ? renderValue(fieldValue) : "Value not set.";
            });

            return (
                <Table.Tr key={entry.id}>
                    {fieldCells.map((cellValue, index) => (
                        <Table.Td key={index}>
                            <Text size="sm" truncate="end" maw={200}>
                                {cellValue}
                            </Text>
                        </Table.Td>
                    ))}
                    <Table.Td>
                        <Text size="sm" c="dimmed">
                            {new Date(entry.createdAt).toLocaleString()}
                        </Text>
                    </Table.Td>
                    <Table.Td>
                        <Group gap="xs">
                            <ActionIcon
                                variant="outline"
                                color="green"
                                size="sm"
                                onClick={() => {
                                    setSelectedEntry(entry);
                                    setOpenDialogType(OpenDialogType.UpdateEntry);
                                }}
                            >
                                <MdEdit size={16} />
                            </ActionIcon>
                            <ActionIcon
                                variant="outline"
                                color="red"
                                size="sm"
                                onClick={() => {
                                    setSelectedEntry(entry);
                                    setOpenDialogType(OpenDialogType.DeleteEntry);
                                }}
                            >
                                <MdDelete size={16} />
                            </ActionIcon>
                        </Group>
                    </Table.Td>
                </Table.Tr>
            );
        });
    }, [paginatedEntries, fields]);

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
                            leftSection={<FiFile size={18} />}
                        >
                            Import Entries
                        </Button>
                    </Group>
                    <Group>
                        <Pagination
                            onChange={setCurrentPage}
                            total={totalPages}
                            color={props.tracker.color}
                        />
                    </Group>
                </Group>

                {entries.length > 0 ? (
                    <Table.ScrollContainer minWidth={800}>
                        <Table striped highlightOnHover withTableBorder withColumnBorders>
                            <Table.Thead>
                                <Table.Tr>
                                    {tableHeaders.map((header, index) => (
                                        <Table.Th key={index}>
                                            <Text fw={600} size="sm">
                                                {header}
                                            </Text>
                                        </Table.Th>
                                    ))}
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {tableRows}
                            </Table.Tbody>
                        </Table>
                    </Table.ScrollContainer>
                ) : (
                    <Text size="lg" c="dimmed" ta="center" py="xl">
                        No entries found. Create your first entry to get started.
                    </Text>
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
