import { Button, Group, Modal, Stack } from "@mantine/core";
import { useForm } from "@mantine/form";
import { entriesController } from "../api/entriesController";
import FieldValueInput from "../../fields/components/FieldValueInput";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { GetStringValue } from "./EntryFormDialog";
import { useDefaultValueResolution } from "../hooks/useDefaultValueResolution";

interface Props {
    tracker: TrackerDto;
    onClose: () => void;
    /** Fired only on a successful create, separately from onClose (dismiss). */
    onCreated?: () => void;
}

export default function QuickAddEntryDialog({ tracker, onClose, onCreated }: Props) {
    const inputtableFields = tracker.fields.filter((f) => !f.isCalculated);

    const form = useForm<{ [key: string]: unknown }>({ initialValues: {} });

    const { hiddenFieldNames } = useDefaultValueResolution(
        tracker.id,
        tracker.fields,
        form,
        true,
    );
    const visibleFields = inputtableFields.filter(
        (field) => !hiddenFieldNames.has(field.name),
    );

    const handleSubmit = async (values: Record<string, unknown>) => {
        const fieldValues: Record<string, string> = {};
        inputtableFields.forEach((field) => {
            fieldValues[field.name] = GetStringValue(field.type, values[field.name]);
        });
        await entriesController.createEntry(tracker.id, fieldValues);
        form.reset();
        onCreated?.();
        onClose();
    };

    return (
        <Modal
            opened
            centered
            title={`Add entry: ${tracker.name}`}
            onClose={onClose}
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    {visibleFields.map((field) => (
                        <FieldValueInput key={field.id} field={field} form={form} />
                    ))}
                    <Group justify="flex-end">
                        <Button variant="default" onClick={onClose}>
                            Cancel
                        </Button>
                        <Button color={tracker.color} type="submit">
                            Add entry
                        </Button>
                    </Group>
                </Stack>
            </form>
        </Modal>
    );
}
