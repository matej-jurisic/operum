// Updated FieldValueInput.tsx
import { Autocomplete, NumberInput, Select, Textarea } from "@mantine/core";
import { DatePickerInput, DateTimePicker, TimePicker } from "@mantine/dates";
import { UseFormReturnType } from "@mantine/form";
import { CSSProperties } from "react";
import { FieldDto } from "../types/FieldDto";

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

    const { key, ...baseProps } = {
        label: field.name,
        description: field.description || undefined,
        required: field.required,
        key: field.id,
        ...form.getInputProps(path),
    };

    switch (field.type) {
        case "string":
            return field.selectOptions?.length ? (
                <Autocomplete key={key} {...baseProps} style={styles} data={field.selectOptions} />
            ) : (
                <Textarea key={key} {...baseProps} style={styles} autosize />
            );

        case "number":
            return field.selectOptions?.length ? (
                <Autocomplete key={key} {...baseProps} style={styles} data={field.selectOptions} />
            ) : (
                <NumberInput key={key} {...baseProps} style={styles} />
            );

        case "bool":
            const boolValue = baseProps.value;
            const stringValue =
                typeof boolValue === "boolean"
                    ? boolValue.toString()
                    : boolValue;
            return (
                <Select
                    key={key}
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
                    key={key}
                    dropdownType="modal"
                    style={styles}
                    valueFormat="DD/MM/YYYY"
                    placeholder="Pick date"
                    {...baseProps}
                    value={baseProps.value || null}
                    modalProps={{
                        centered: true,
                    }}
                />
            );

        case "timespan":
            return (
                <TimePicker
                    key={key}
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
                    key={key}
                    valueFormat="DD/MM/YYYY HH:mm:ss"
                    withSeconds
                    placeholder="Pick date/time"
                    {...baseProps}
                    dropdownType="modal"
                    styles={{
                        input: {
                            cursor: "pointer",
                            userSelect: "none",
                        },
                    }}
                    modalProps={{
                        centered: true,
                    }}
                />
            );

        default:
            return null;
    }
}
