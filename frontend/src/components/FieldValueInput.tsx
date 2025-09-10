// Updated FieldValueInput.tsx
import { Checkbox, NumberInput, TextInput } from "@mantine/core";
import { DatePickerInput, DateTimePicker, TimePicker } from "@mantine/dates";
import { UseFormReturnType } from "@mantine/form";
import { CSSProperties } from "react";
import { FieldDto } from "../model/FieldDto";

interface FieldValueInputProps<T = any> {
    field: FieldDto;
    form: UseFormReturnType<T>;
    fieldPath?: string; // Optional custom path for nested fields
    styles?: CSSProperties;
}

export default function FieldValueInput<T = any>({
    field,
    form,
    fieldPath,
    styles,
}: FieldValueInputProps<T>) {
    const path = fieldPath || field.name;

    const baseProps = {
        label: field.name,
        required: field.required,
        key: field.id,
        ...form.getInputProps(path),
    };

    switch (field.type) {
        case "string":
            return <TextInput {...baseProps} style={styles} />;

        case "number":
            return <NumberInput {...baseProps} style={styles} />;

        case "bool":
            return (
                <Checkbox
                    label={`${field.name}${
                        field.description ? " - " + field.description : ""
                    }`}
                    key={field.id}
                    style={styles}
                    {...form.getInputProps(path, { type: "checkbox" })}
                />
            );

        case "date":
            return (
                <DatePickerInput
                    style={styles}
                    valueFormat="DD/MM/YYYY"
                    placeholder="Pick date"
                    {...baseProps}
                    value={baseProps.value || null}
                />
            );

        case "timespan":
            return (
                <TimePicker
                    style={styles}
                    withSeconds
                    {...baseProps}
                    format="24h"
                    label={baseProps.label + " (hh:mm:ss)"}
                />
            );

        case "datetime":
            return (
                <DateTimePicker
                    valueFormat="DD/MM/YYYY HH:mm:ss"
                    withSeconds
                    placeholder="Pick date/time"
                    {...baseProps}
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
}
