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
import { Dispatch, SetStateAction, useEffect, useState } from "react";
import api from "../api/api";
import { FieldDto } from "../model/FieldDto";
import { TrackerDto } from "../model/TrackerDto";

interface CreateEntryDialogProps {
    tracker: TrackerDto;
    onClose: () => void;
    onEntryCreated?: () => void;
}

const GetFields = async (
    trackerId: string,
    setFields: Dispatch<SetStateAction<FieldDto[]>>
) => {
    const response = await api.get(`/trackers/${trackerId}/fields`);
    setFields(response.data.data);
};

export default function CreateEntryDialog(props: CreateEntryDialogProps) {
    const [fields, setFields] = useState<FieldDto[]>([]);
    const form = useForm<{ [key: string]: unknown }>({ initialValues: {} });

    const handleSubmit = async (values: Record<string, unknown>) => {
        const fieldValues: Record<string, string> = {};

        fields.forEach((field) => {
            let value = values[field.name];

            if (field.type === "bool") {
                value = value ?? false;
                fieldValues[field.name] = value ? "true" : "false";
            } else if (value != null) {
                if (field.type === "date" && value instanceof Date) {
                    fieldValues[field.name] = value.toISOString();
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

    useEffect(() => {
        GetFields(props.tracker.id, setFields);
    }, [props.tracker.id]);

    if (fields.length === 0) {
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
                                                placeholder="Pick date"
                                                {...fieldProps}
                                            />
                                        );
                                    case "timespan":
                                        return (
                                            <TimeInput
                                                withSeconds
                                                {...fieldProps}
                                            />
                                        );
                                    case "datetime":
                                        return (
                                            <DateTimePicker
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
