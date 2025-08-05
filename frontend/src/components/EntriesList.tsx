import {
    Button,
    Group,
    Input,
    Paper,
    SimpleGrid,
    Stack,
    Text,
} from "@mantine/core";
import { useState } from "react";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { EntryDto } from "../model/EntryDto";
import { FieldValueDto } from "../model/FieldValueDto";
import { TrackerDto } from "../model/TrackerDto";
import ConfirmationDialog from "./ConfirmationDialog";

interface EntriesListProps {
    tracker: TrackerDto;
    entries: EntryDto[];
    refreshEntries: () => void;
}

const DeleteEntry = async (trackerId: string, entryId: string) => {
    await api.delete(`/trackers/${trackerId}/entries/${entryId}`);
};

const renderValue = (v: FieldValueDto) => {
    if (v.value === null) return "Value not set.";
    if (typeof v.value === "string") {
        if (v.fieldType === "date")
            return new Date(v.value).toLocaleDateString();
        if (v.fieldType === "datetime")
            return new Date(v.value).toLocaleString();
        if (v.fieldType === "time")
            return new Date(`1970-01-01T${v.value}`).toLocaleTimeString();
        return v.value;
    }
    if (typeof v.value === "number") return v.value;
    if (typeof v.value === "boolean") return v.value.toString();
    return "Unexpected data type";
};

export default function EntriesList(props: EntriesListProps) {
    const [entryToDelete, setEntryToDelete] = useState<EntryDto>();

    return (
        <>
            <Stack gap="md">
                {props.entries.map((entry) => (
                    <Paper
                        key={entry.id}
                        shadow="xs"
                        p="md"
                        radius="md"
                        withBorder
                    >
                        <Stack gap={"md"} align="stretch">
                            <SimpleGrid
                                cols={{ base: 1, sm: 2, md: 3, lg: 4 }}
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
                                    variant="light"
                                    color="red"
                                    onClick={() => setEntryToDelete(entry)}
                                >
                                    <MdDelete size={18} />
                                </Button>
                            </Group>
                        </Stack>
                    </Paper>
                ))}
            </Stack>
            {entryToDelete && (
                <ConfirmationDialog
                    isOpen={!!entryToDelete}
                    onClose={() => setEntryToDelete(undefined)}
                    onConfirm={async () => {
                        await DeleteEntry(props.tracker.id, entryToDelete.id);
                        props.refreshEntries();
                        setEntryToDelete(undefined);
                    }}
                    severity="warning"
                    message="Are you sure you want to delete this entry?"
                />
            )}
        </>
    );
}
