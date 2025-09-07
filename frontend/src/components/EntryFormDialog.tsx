import {
    Button,
    Checkbox,
    Modal,
    NumberInput,
    Stack,
    TextInput,
} from "@mantine/core";
import { DatePickerInput, DateTimePicker, TimePicker } from "@mantine/dates";
import { useForm } from "@mantine/form";
import { useTracker } from "../context/TrackerContext";
import { TrackerDto } from "../model/TrackerDto";

interface EntryFormDialogProps {
    tracker: TrackerDto;
    entryId?: string;
    initialValues?: Record<string, unknown>;
    onClose: () => void;
}

export default function EntryFormDialog(props: EntryFormDialogProps) {
    const { fields, CreateEntry, UpdateEntry } = useTracker();
    const form = useForm<{ [key: string]: unknown }>({
        initialValues: props.initialValues || {},
    });

    const handleSubmit = async (values: Record<string, unknown>) => {
        const fieldValues: Record<string, string> = {};

        fields.forEach((field) => {
            let value = values[field.name];

            if (field.type === "bool") {
                value = value ?? false;
                fieldValues[field.name] = value ? "true" : "false";
            } else if (value != null) {
                if (field.type === "date") {
                    const parsed = new Date(value as string);
                    const utcMidnight = new Date(
                        Date.UTC(
                            parsed.getFullYear(),
                            parsed.getMonth(),
                            parsed.getDate()
                        )
                    );
                    fieldValues[field.name] = utcMidnight.toISOString();
                } else if (field.type === "timespan") {
                    fieldValues[field.name] = String(value);
                } else {
                    fieldValues[field.name] = String(value);
                }
            }
        });

        if (props.entryId) {
            await UpdateEntry(props.tracker.id, props.entryId, fieldValues);
        } else {
            await CreateEntry(props.tracker.id, fieldValues);
        }

        form.reset();
        props.onClose();
    };

    return (
        <>
            <Modal
                title={props.entryId ? "Update Entry" : "Create Entry"}
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
                            {fields.map((field) => {
                                const fieldProps = {
                                    label: field.name,
                                    required: field.required,
                                    key: field.id,
                                    ...form.getInputProps(field.name),
                                };

                                switch (field.type) {
                                    case "string":
                                        return <TextInput {...fieldProps} />;
                                    case "number":
                                        return <NumberInput {...fieldProps} />;
                                    case "bool":
                                        return (
                                            <Checkbox
                                                label={`${field.name}${
                                                    field.description
                                                        ? " - " +
                                                          field.description
                                                        : ""
                                                }`}
                                                key={field.id}
                                                {...form.getInputProps(
                                                    field.name,
                                                    { type: "checkbox" }
                                                )}
                                            />
                                        );
                                    case "date":
                                        return (
                                            <DatePickerInput
                                                valueFormat="DD/MM/YYYY"
                                                placeholder="Pick date"
                                                {...fieldProps}
                                            />
                                        );
                                    case "timespan":
                                        return (
                                            <TimePicker
                                                withSeconds
                                                {...fieldProps}
                                                format="24h"
                                                label={
                                                    fieldProps.label +
                                                    " (hh:mm:ss)"
                                                }
                                            />
                                        );
                                    case "datetime":
                                        return (
                                            <DateTimePicker
                                                valueFormat="DD/MM/YYYY HH:mm:ss"
                                                withSeconds
                                                placeholder="Pick date/time"
                                                {...fieldProps}
                                                dropdownType="popover"
                                                styles={{
                                                    input: {
                                                        cursor: "pointer",
                                                        userSelect: "none",
                                                    },
                                                }}
                                                modalProps={{
                                                    centered: true,
                                                    size: "sm",
                                                }}
                                            />
                                        );
                                    default:
                                        return null;
                                }
                            })}

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
