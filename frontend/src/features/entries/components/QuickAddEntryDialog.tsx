import { Button, Modal, Stack } from "@mantine/core";
import { useForm } from "@mantine/form";
import { entriesController } from "../api/entriesController";
import FieldValueInput from "../../fields/components/FieldValueInput";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { GetStringValue } from "./EntryFormDialog";

interface Props {
    tracker: TrackerDto;
    onClose: () => void;
}

export default function QuickAddEntryDialog({ tracker, onClose }: Props) {
    const inputtableFields = tracker.fields.filter((f) => !f.isCalculated);

    const form = useForm<{ [key: string]: unknown }>({ initialValues: {} });

    const handleSubmit = async (values: Record<string, unknown>) => {
        const fieldValues: Record<string, string> = {};
        inputtableFields.forEach((field) => {
            fieldValues[field.name] = GetStringValue(field.type, values[field.name]);
        });
        await entriesController.createEntry(tracker.id, fieldValues);
        form.reset();
        onClose();
    };

    return (
        <Modal
            opened
            centered
            title={`Add entry — ${tracker.name}`}
            onClose={onClose}
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    {inputtableFields.map((field) => (
                        <FieldValueInput key={field.id} field={field} form={form} />
                    ))}
                    <Button color={tracker.color} type="submit">
                        Add Entry
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
