// Updated FieldValueInput.tsx
import { NumberInput, Select, Textarea } from "@mantine/core";
import { DatePickerInput, DateTimePicker, TimePicker } from "@mantine/dates";
import { UseFormReturnType } from "@mantine/form";
import { CSSProperties } from "react";
import { FieldDto } from "../model/FieldDto";

interface FieldValueInputProps<T extends Record<string, any> = any> {
    field: FieldDto;
    form: UseFormReturnType<T>;
    fieldPath?: string;
    styles?: CSSProperties;
}

export default function FieldValueInput<T extends Record<string, any> = any>({
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
            return <Textarea {...baseProps} style={styles} autosize />;

        case "number":
            return <NumberInput {...baseProps} style={styles} />;

        case "bool":
            const boolValue = baseProps.value;
            const stringValue =
                typeof boolValue === "boolean"
                    ? boolValue.toString()
                    : boolValue;
            return (
                <Select
                    label={field.name}
                    placeholder="Select"
                    data={[
                        { value: "true", label: "Yes" },
                        { value: "false", label: "No" },
                    ]}
                    clearable
                    style={styles}
                    {...form.getInputProps(path)}
                    value={stringValue}
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
