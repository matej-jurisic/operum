import { Button, Modal, Stack } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import FieldValueInput from "../../fields/components/FieldValueInput";
import { useFields } from "../../fields/context/FieldsContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";

interface EntryFormDialogProps {
    tracker: TrackerDto;
    entryId?: string;
    title?: string;
    initialValues?: Record<string, unknown>;
    onClose: () => void;
}

export function GetStringValue(type: string | unknown, value: unknown) {
    if (value === null || value === undefined) {
        return "";
    }
    switch (type) {
        case "date":
            if (value) {
                const d =
                    value instanceof Date ? value : new Date(value as string);
                if (!isNaN(d.getTime())) {
                    const utcMidnight = new Date(
                        Date.UTC(d.getFullYear(), d.getMonth(), d.getDate())
                    );
                    return utcMidnight.toISOString();
                }
            }
            return "";

        case "timespan":
            return String(value);

        case "datetime":
            if (value) {
                const d =
                    value instanceof Date ? value : new Date(value as string);
                if (!isNaN(d.getTime())) {
                    return d.toISOString();
                }
            }
            return "";

        default:
            return String(value);
    }
}

export default function EntryFormDialog(props: EntryFormDialogProps) {
    const title = props.title ?? (props.entryId ? "Update Entry" : "Create Entry");
    const { fields } = useFields();
    const { updateEntry, createEntry } = useTrackerOperations();

    const form = useForm<{ [key: string]: unknown }>({
        initialValues: props.initialValues || {},
    });

    const handleSubmit = async (values: Record<string, unknown>) => {
        const fieldValues: Record<string, string> = {};

        fields.forEach((field) => {
            let value = values[field.name];
            fieldValues[field.name] = GetStringValue(field.type, value);
        });

        if (props.entryId) {
            await updateEntry(props.entryId, fieldValues);
        } else {
            await createEntry(fieldValues);
        }

        form.reset();
        props.onClose();
    };

    return (
        <>
            <Modal
                title={title}
                centered
                opened
                onClose={props.onClose}
            >
                <>
                    <form onSubmit={form.onSubmit(handleSubmit)}>
                        <Stack
                            align="stretch"
                            style={{ maxWidth: 400, margin: "0 auto" }}
                        >
                            {fields.map((field) => (
                                <FieldValueInput
                                    key={field.id}
                                    field={field}
                                    form={form}
                                />
                            ))}

                            <Button color={props.tracker.color} type="submit">
                                {props.entryId ? "Update" : "Create"}
                            </Button>

                        </Stack>
                    </form>
                </>
            </Modal>
        </>
    );
}
