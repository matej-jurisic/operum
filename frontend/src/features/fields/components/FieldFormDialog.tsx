import {
    Button,
    Checkbox,
    Group,
    Modal,
    SegmentedControl,
    Select,
    Stack,
    TagsInput,
    Text,
    Textarea,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useMemo, useState } from "react";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import {
    calculatedFieldTypes,
    fieldTypes,
    operatorsForFieldType,
} from "../../../shared/constants/DataTypesForSelect";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useConstants } from "../../constants/context/ConstantsContext";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useFields } from "../context/FieldsContext";
import { CreateFieldDto } from "../types/CreateFieldDto";
import { FieldDto } from "../types/FieldDto";
import { UpdateFieldDto } from "../types/UpdateFieldDto";
import FieldValueInput from "./FieldValueInput";

/** Mirrors the server's DataTypes.AreCompatible: date/datetime are interchangeable, everything else needs an exact match. */
function isCompatibleConstantType(constantType: string, fieldType: string): boolean {
    if (constantType === fieldType) return true;
    const dateTypes = ["date", "datetime"];
    return dateTypes.includes(constantType) && dateTypes.includes(fieldType);
}

type DefaultValueMode = "none" | "static" | "constant";

type FormValues = Omit<
    CreateFieldDto & UpdateFieldDto,
    "defaultValue" | "visibilityValue"
> & {
    defaultValue?: string | number | Date;
    visibilityValue?: string | number | Date;
};

interface FieldFormDialogProps {
    tracker: TrackerDto;
    fieldId?: string;
    initialValues?: FieldDto;
    onClose: () => void;
}

export function FieldFormDialog(props: FieldFormDialogProps) {
    const { createField, updateField } = useTrackerOperations();
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const { constants, refreshConstantsIfDirty } = useConstants();
    const { fields } = useFields();

    useEffect(() => {
        trackersController
            .getTrackerList("Accessible")
            .then((res) => setTrackers(res.data ?? []))
            .catch(() => setTrackers([]));
    }, []);

    useEffect(() => {
        refreshConstantsIfDirty();
    }, [refreshConstantsIfDirty]);

    const [defaultValueMode, setDefaultValueMode] = useState<DefaultValueMode>(
        props.initialValues?.defaultValueConstantId
            ? "constant"
            : props.initialValues?.defaultValue !== undefined &&
                props.initialValues.defaultValue !== ""
              ? "static"
              : "none",
    );

    const form = useForm<FormValues>({
        initialValues: props.initialValues
            ? {
                  name: props.initialValues.name,
                  type: props.initialValues.type,
                  required: props.initialValues.required,
                  description: props.initialValues.description || "",
                  selectOptions: props.initialValues.selectOptions || [],
                  isCalculated: props.initialValues.isCalculated,
                  formula: props.initialValues.formula || "",
                  referencedTrackerId: props.initialValues.referencedTrackerId || "",
                  referencedDisplayFieldId:
                      props.initialValues.referencedDisplayFieldId || "",
                  defaultValue: props.initialValues.defaultValue || "",
                  defaultValueConstantId:
                      props.initialValues.defaultValueConstantId || "",
                  visibilityFieldId:
                      props.initialValues.visibilityFieldId || "",
                  visibilityOperator:
                      props.initialValues.visibilityOperator || "",
                  visibilityValue:
                      props.initialValues.visibilityValue || "",
              }
            : {
                  name: "",
                  type: "string",
                  required: false,
                  description: "",
                  selectOptions: [],
                  isCalculated: false,
                  formula: "",
                  referencedTrackerId: "",
                  referencedDisplayFieldId: "",
                  defaultValue: "",
                  defaultValueConstantId: "",
                  visibilityFieldId: "",
                  visibilityOperator: "",
                  visibilityValue: "",
              },

        validate: {
            name: (value) =>
                value.trim().length === 0
                    ? "Field name is required"
                    : value.length > (props.fieldId ? 100 : 30)
                    ? `Field name must be at most ${props.fieldId ? 100 : 30} characters`
                    : null,
            description: (value) =>
                value && value.length > 500
                    ? "Description must be at most 500 characters"
                    : null,
            type: (value) => (value ? null : "Type is required"),
            formula: (value, values) =>
                values.isCalculated && !value?.trim()
                    ? "Formula is required for calculated fields"
                    : values.isCalculated && value && value.length > 500
                    ? "Formula must be at most 500 characters"
                    : null,
            selectOptions: (values, form) =>
                form.type === "number" && values?.some((v) => isNaN(Number(v)))
                    ? "All suggested options for number fields must be valid numbers"
                    : null,
            referencedTrackerId: (value, values) =>
                values.type === "reference" && !value
                    ? "Pick a tracker to link to"
                    : null,
        },
    });

    const isReference = form.values.type === "reference";
    const isDateType = form.values.type === "date" || form.values.type === "datetime";
    const canHaveDefault = !isReference && !form.values.isCalculated;

    const compatibleConstants = useMemo(
        () => constants.filter((c) => isCompatibleConstantType(c.type, form.values.type)),
        [constants, form.values.type],
    );

    const visibilityFieldOptions = useMemo(
        () =>
            fields
                .filter((f) => !f.isCalculated && f.id !== props.fieldId)
                .map((f) => ({ value: f.id, label: f.name })),
        [fields, props.fieldId],
    );

    const visibilityTargetField = useMemo(
        () => fields.find((f) => f.id === form.values.visibilityFieldId),
        [fields, form.values.visibilityFieldId],
    );

    const referencedTracker = useMemo(
        () => trackers.find((t) => t.id === form.values.referencedTrackerId),
        [trackers, form.values.referencedTrackerId],
    );

    const displayFieldOptions = useMemo(
        () =>
            (referencedTracker?.fields ?? [])
                .filter((f) => f.type !== "reference")
                .map((f) => ({ value: f.id, label: f.name })),
        [referencedTracker],
    );

    const clearDefault = () => {
        setDefaultValueMode("none");
        form.setFieldValue("defaultValue", "");
        form.setFieldValue("defaultValueConstantId", "");
    };

    const handleModeChange = (value: string) => {
        const isCalc = value === "calculated";
        form.setFieldValue("isCalculated", isCalc);
        if (isCalc) {
            form.setFieldValue("required", false);
            clearDefault();
            form.setFieldValue("visibilityFieldId", "");
            form.setFieldValue("visibilityOperator", "");
            form.setFieldValue("visibilityValue", "");
            const calcTypes = calculatedFieldTypes.map((t) => t.value);
            if (!calcTypes.includes(form.values.type)) {
                form.setFieldValue("type", "number");
            }
        }
    };

    const handleTypeChange = (value: string | null) => {
        const next = value ?? "string";
        form.setFieldValue("type", next);
        // Existing default is invalid for the new type.
        clearDefault();
        if (next === "reference") {
            form.setFieldValue("isCalculated", false);
            form.setFieldValue("selectOptions", []);
        } else {
            form.setFieldValue("referencedTrackerId", "");
            form.setFieldValue("referencedDisplayFieldId", "");
        }
    };

    /** Converts a typed value field's live form state (string, number, Date, or a relative token) to the raw string the API expects. */
    const toSubmitTypedValue = (
        type: string,
        value: string | number | Date | undefined,
    ): string | undefined => {
        if (value === undefined || value === "") return undefined;
        if (type === "date" || type === "datetime") {
            if (typeof value === "string" && isDynamicDateToken(value))
                return value;
            return GetStringValue(type, value) || undefined;
        }
        return String(value);
    };

    const handleSubmit = async (values: FormValues) => {
        const defaultValue =
            defaultValueMode === "static" && canHaveDefault
                ? toSubmitTypedValue(values.type, values.defaultValue)
                : undefined;
        const defaultValueConstantId =
            defaultValueMode === "constant" && canHaveDefault
                ? values.defaultValueConstantId || undefined
                : undefined;
        const visibilityFieldId = values.isCalculated
            ? undefined
            : values.visibilityFieldId || undefined;
        const visibilityOperator = visibilityFieldId
            ? values.visibilityOperator || undefined
            : undefined;
        const visibilityValue = visibilityFieldId
            ? toSubmitTypedValue(
                  visibilityTargetField?.type ?? "string",
                  values.visibilityValue,
              )
            : undefined;

        const payload = {
            ...values,
            defaultValue,
            defaultValueConstantId,
            visibilityFieldId,
            visibilityOperator,
            visibilityValue,
            formula: values.isCalculated ? values.formula : undefined,
            referencedTrackerId:
                values.type === "reference"
                    ? values.referencedTrackerId
                    : undefined,
            referencedDisplayFieldId:
                values.type === "reference"
                    ? values.referencedDisplayFieldId || undefined
                    : undefined,
        };
        if (props.fieldId) {
            updateField(props.fieldId, payload);
        } else {
            createField(payload);
        }
        props.onClose();
        form.reset();
    };

    const isCalculated = form.values.isCalculated;

    return (
        <Modal
            opened
            onClose={props.onClose}
            title={props.fieldId ? "Edit field" : "Create field"}
            centered
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack align="stretch">
                    <SegmentedControl
                        data={[
                            { label: "Manual", value: "manual" },
                            { label: "Calculated", value: "calculated" },
                        ]}
                        value={isCalculated ? "calculated" : "manual"}
                        onChange={handleModeChange}
                        fullWidth
                    />

                    <TextInput
                        label="Field name"
                        maxLength={30}
                        {...form.getInputProps("name")}
                    />

                    <Select
                        allowDeselect={false}
                        label="Type"
                        placeholder="Select field type"
                        data={isCalculated ? calculatedFieldTypes : fieldTypes}
                        required
                        {...form.getInputProps("type")}
                        onChange={handleTypeChange}
                    />

                    {isReference && (
                        <>
                            <Select
                                label="Referenced tracker"
                                placeholder="Select a tracker"
                                searchable
                                data={trackers.map((t) => ({
                                    value: t.id,
                                    label: t.name,
                                }))}
                                {...form.getInputProps("referencedTrackerId")}
                                onChange={(value) => {
                                    form.setFieldValue(
                                        "referencedTrackerId",
                                        value ?? "",
                                    );
                                    form.setFieldValue(
                                        "referencedDisplayFieldId",
                                        "",
                                    );
                                }}
                            />
                            <Select
                                label="Display field"
                                description="Which field of that tracker to show as the link label."
                                placeholder="Entry date"
                                clearable
                                disabled={!referencedTracker}
                                data={displayFieldOptions}
                                {...form.getInputProps("referencedDisplayFieldId")}
                            />
                        </>
                    )}

                    {isCalculated && (
                        <Stack gap="xs">
                            <Textarea
                                label="Formula"
                                placeholder="{FieldName} * {AnotherField} + 100"
                                autosize
                                minRows={2}
                                maxLength={500}
                                {...form.getInputProps("formula")}
                            />
                            <Text size="xs" c="dimmed">
                                Reference fields and constants as {"{Name}"}, with + - * / %. Timespans also take {"{Name.hours}"}, {"{Name.minutes}"} and {"{Name.seconds}"}. Subtracting two dates gives seconds.
                            </Text>
                        </Stack>
                    )}

                    {canHaveDefault && (
                        <Stack gap="xs">
                            <Text size="sm" fw={500}>
                                Default value
                            </Text>
                            <SegmentedControl
                                size="xs"
                                fullWidth
                                data={[
                                    { label: "None", value: "none" },
                                    { label: "Static value", value: "static" },
                                    { label: "From constant", value: "constant" },
                                ]}
                                value={defaultValueMode}
                                onChange={(v) => {
                                    setDefaultValueMode(v as DefaultValueMode);
                                    form.setFieldValue("defaultValue", "");
                                    form.setFieldValue(
                                        "defaultValueConstantId",
                                        "",
                                    );
                                }}
                            />
                            {defaultValueMode === "static" &&
                                (isDateType ? (
                                    <DynamicDateValueInput
                                        isDateType
                                        value={form.values.defaultValue}
                                        onChange={(v) =>
                                            form.setFieldValue(
                                                "defaultValue",
                                                v ?? "",
                                            )
                                        }
                                        field={{
                                            id: "defaultValue",
                                            name: "Default value",
                                            type: form.values.type,
                                            required: false,
                                            isCalculated: false,
                                        }}
                                        form={form}
                                        fieldPath="defaultValue"
                                        label="Default value"
                                    />
                                ) : (
                                    <FieldValueInput
                                        field={{
                                            id: "defaultValue",
                                            name: "Default value",
                                            type: form.values.type,
                                            required: false,
                                            isCalculated: false,
                                            selectOptions:
                                                form.values.selectOptions,
                                        }}
                                        form={form}
                                        fieldPath="defaultValue"
                                    />
                                ))}
                            {defaultValueMode === "constant" && (
                                <Select
                                    placeholder="Select constant"
                                    data={compatibleConstants.map((c) => ({
                                        value: c.id,
                                        label: c.name,
                                    }))}
                                    {...form.getInputProps(
                                        "defaultValueConstantId",
                                    )}
                                />
                            )}
                        </Stack>
                    )}

                    {!isCalculated && (
                        <Stack gap="xs">
                            <Select
                                label="Hide unless"
                                description="Only applies to the create-entry form, not editing."
                                placeholder="Always show"
                                clearable
                                data={visibilityFieldOptions}
                                {...form.getInputProps("visibilityFieldId")}
                                onChange={(value) => {
                                    form.setFieldValue(
                                        "visibilityFieldId",
                                        value ?? "",
                                    );
                                    form.setFieldValue("visibilityOperator", "");
                                    form.setFieldValue("visibilityValue", "");
                                }}
                            />
                            {visibilityTargetField && (
                                <>
                                    <Select
                                        allowDeselect={false}
                                        label="Operator"
                                        placeholder="Select operator"
                                        data={operatorsForFieldType(
                                            visibilityTargetField.type,
                                        )}
                                        value={form.values.visibilityOperator || null}
                                        onChange={(value) =>
                                            form.setFieldValue(
                                                "visibilityOperator",
                                                value ?? "",
                                            )
                                        }
                                    />
                                    <DynamicDateValueInput
                                        isDateType={
                                            visibilityTargetField.type === "date" ||
                                            visibilityTargetField.type === "datetime"
                                        }
                                        value={form.values.visibilityValue}
                                        onChange={(v) =>
                                            form.setFieldValue(
                                                "visibilityValue",
                                                v ?? "",
                                            )
                                        }
                                        field={visibilityTargetField}
                                        form={form}
                                        fieldPath="visibilityValue"
                                        label="Value"
                                    />
                                </>
                            )}
                        </Stack>
                    )}

                    <Textarea
                        label="Description"
                        autosize
                        maxLength={500}
                        {...form.getInputProps("description")}
                    />

                    {!isCalculated && (form.values.type === "string" || form.values.type === "number") && (
                        <TagsInput
                            label="Suggested options"
                            placeholder="Type and press Enter to add options"
                            {...form.getInputProps("selectOptions")}
                        />
                    )}

                    {!isCalculated && (
                        <Checkbox
                            label="Required"
                            checked={form.values.required}
                            {...form.getInputProps("required")}
                        />
                    )}

                    <Group justify="flex-end">
                        <Button variant="default" onClick={props.onClose}>
                            Cancel
                        </Button>
                        <Button color={props.tracker.color} type="submit">
                            {props.fieldId ? "Save" : "Create"}
                        </Button>
                    </Group>
                </Stack>
            </form>
        </Modal>
    );
}
