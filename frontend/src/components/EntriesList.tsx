import {
    Button,
    Card,
    Group,
    Input,
    Pagination,
    SimpleGrid,
    Stack,
    Text,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { FiPlus } from "react-icons/fi";
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
}

export default function EntriesList(props: EntriesListProps) {
    const [selectedEntry, setSelectedEntry] = useState<EntryDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [currentPage, setCurrentPage] = useState(1);

    const {
        entries,
        refreshEntriesIfDirty,
        refreshEntries,
        markAnalyticsDirty,
    } = useTracker();

    const pageSize = 3;
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

    return (
        <>
            <Stack gap="md">
                <Group justify={"space-between"} w="100%">
                    <Button
                        color={props.tracker.color}
                        leftSection={<FiPlus size={18} />}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateEntry)
                        }
                    >
                        Create Entry
                    </Button>
                    <Group>
                        <Pagination
                            onChange={setCurrentPage}
                            total={totalPages}
                            color={props.tracker.color}
                        />
                    </Group>
                </Group>

                {paginatedEntries.map((entry) => (
                    <Card key={entry.id} p="md" radius="md" withBorder>
                        <Stack gap={"md"} align="stretch">
                            <SimpleGrid
                                cols={{ base: 2, sm: 2, md: 3, lg: 4 }}
                                spacing="md"
                                verticalSpacing="sm"
                            >
                                {entry.fieldValues.map((field) => (
                                    <Input.Wrapper
                                        key={field.fieldId}
                                        label={field.fieldName}
                                    >
                                        <Input
                                            readOnly
                                            value={renderValue(field)}
                                        />
                                    </Input.Wrapper>
                                ))}
                            </SimpleGrid>
                            <Group gap={"md"} justify="center" w={"100%"}>
                                <Text size="sm" c="dimmed">
                                    {new Date(entry.createdAt).toLocaleString()}
                                </Text>
                                <Button
                                    variant="outline"
                                    color="red"
                                    onClick={() => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteEntry
                                        );
                                    }}
                                >
                                    <MdDelete size={18} />
                                </Button>
                                <Button
                                    variant="outline"
                                    color="green"
                                    onClick={() => {
                                        setSelectedEntry(entry);
                                        setOpenDialogType(
                                            OpenDialogType.UpdateEntry
                                        );
                                    }}
                                >
                                    <MdEdit size={18} />
                                </Button>
                            </Group>
                        </Stack>
                    </Card>
                ))}
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
        </>
    );
}
