import {
    Button,
    Checkbox,
    Modal,
    NumberInput,
    Stack,
    TextInput,
} from "@mantine/core";
import { DatePickerInput, DateTimePicker, TimeInput } from "@mantine/dates";
import { useForm } from "@mantine/form";
import api from "../api/api";
import { TrackerDto } from "../model/TrackerDto";

interface CreateEntryDialogProps {
    tracker: TrackerDto;
    onClose: () => void;
    onEntryCreated?: () => void;
}

export default function CreateEntryDialog(props: CreateEntryDialogProps) {
    const form = useForm<{ [key: string]: unknown }>({ initialValues: {} });

    const handleSubmit = async (values: Record<string, unknown>) => {
        const fieldValues: Record<string, string> = {};

        props.tracker.fields.forEach((field) => {
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

        await api.post(`/trackers/${props.tracker.id}/entries`, {
            fieldValues,
        });

        form.reset();
        props.onClose();
        props.onEntryCreated?.();
    };

    if (props.tracker.fields.length === 0) {
        return <></>;
    }

    return (
        <>
            <Modal title="Create Entry" centered opened onClose={props.onClose}>
                <>
                    <form onSubmit={form.onSubmit(handleSubmit)}>
                        <Stack
                            align="stretch"
                            style={{ maxWidth: 400, margin: "0 auto" }}
                        >
                            {props.tracker.fields.map((field) => {
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
                                            <TimeInput
                                                withSeconds
                                                {...fieldProps}
                                                label={
                                                    fieldProps.label +
                                                    " (hh:mm:ss)"
                                                }
                                            />
                                        );
                                    case "datetime":
                                        return (
                                            <DateTimePicker
                                                valueFormat="DD/MM/YYYY hh:mm:ss"
                                                withSeconds
                                                placeholder="Pick date/time"
                                                {...fieldProps}
                                            />
                                        );
                                    default:
                                        return null;
                                }
                            })}

                            <Button color={props.tracker.color} type="submit">
                                Create
                            </Button>
                        </Stack>
                    </form>
                </>
            </Modal>
        </>
    );
}
