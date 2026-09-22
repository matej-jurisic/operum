import { Autocomplete, NumberInput, Select, Textarea } from "@mantine/core";
import { DatePickerInput, DateTimePicker, TimePicker } from "@mantine/dates";
import { UseFormReturnType } from "@mantine/form";
import { CSSProperties } from "react";
import { FieldDto } from "../types/FieldDto";
import ReferenceValueInput from "./ReferenceValueInput";

// Loose typing lets differently-shaped forms share this component without a cast at each call site.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyForm = UseFormReturnType<any>;

interface FieldValueInputProps {
    field: FieldDto;
    form: AnyForm;
    fieldPath?: string;
    styles?: CSSProperties;
    /** Reference fields only: "id" for entry forms, "label" for filter editors. */
    referenceValueMode?: "id" | "label";
    /** Reference fields only: label for the already-selected value. */
    referenceLabel?: string;
}

export default function FieldValueInput({
    field,
    form,
    fieldPath,
    styles,
    referenceValueMode,
    referenceLabel,
}: FieldValueInputProps) {
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
                <Autocomplete
                    key={key}
                    {...baseProps}
                    style={styles}
                    data={field.selectOptions}
                    comboboxProps={{ withinPortal: false }}
                />
            ) : (
                <Textarea key={key} {...baseProps} style={styles} autosize />
            );

        case "number":
            return field.selectOptions?.length ? (
                <Autocomplete
                    key={key}
                    {...baseProps}
                    style={styles}
                    data={field.selectOptions}
                    comboboxProps={{ withinPortal: false }}
                />
            ) : (
                <NumberInput key={key} {...baseProps} style={styles} />
            );

        case "bool": {
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
                    comboboxProps={{ withinPortal: false }}
                    {...form.getInputProps(path)}
                    value={stringValue}
                />
            );
        }

        case "date":
            return (
                <DatePickerInput
                    key={key}
                    dropdownType="modal"
                    style={styles}
                    valueFormat="DD/MM/YYYY"
                    placeholder="Select date"
                    highlightToday
                    {...baseProps}
                    value={baseProps.value || null}
                    modalProps={{
                        centered: true,
                        // Clears nested edit dialogs (e.g. "Set filters"), which sit at zIndex 400.
                        zIndex: 500,
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

        case "reference":
            return (
                <ReferenceValueInput
                    field={field}
                    form={form}
                    fieldPath={path}
                    styles={styles}
                    valueMode={referenceValueMode}
                    referenceLabel={referenceLabel}
                />
            );

        case "datetime":
            return (
                <DateTimePicker
                    key={key}
                    valueFormat="DD/MM/YYYY HH:mm:ss"
                    withSeconds
                    placeholder="Select date and time"
                    highlightToday
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
                        // Clears nested edit dialogs (e.g. "Set filters"), which sit at zIndex 400.
                        zIndex: 500,
                    }}
                />
            );

        default:
            return null;
    }
}
