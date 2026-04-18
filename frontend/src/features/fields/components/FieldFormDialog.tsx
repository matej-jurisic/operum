import {
    Button,
    Checkbox,
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
import { calculatedFieldTypes, fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateFieldDto } from "../types/CreateFieldDto";
import { FieldDto } from "../types/FieldDto";
import { UpdateFieldDto } from "../types/UpdateFieldDto";

interface FieldFormDialogProps {
    tracker: TrackerDto;
    fieldId?: string;
    initialValues?: FieldDto;
    onClose: () => void;
}

export function FieldFormDialog(props: FieldFormDialogProps) {
    const { createField, updateField } = useTrackerOperations();

    const form = useForm<CreateFieldDto & UpdateFieldDto>({
        initialValues: props.initialValues
            ? {
                  name: props.initialValues.name,
                  type: props.initialValues.type,
                  required: props.initialValues.required,
                  description: props.initialValues.description || "",
                  selectOptions: props.initialValues.selectOptions || [],
                  isCalculated: props.initialValues.isCalculated,
                  formula: props.initialValues.formula || "",
              }
            : {
                  name: "",
                  type: "string",
                  required: false,
                  description: "",
                  selectOptions: [],
                  isCalculated: false,
                  formula: "",
              },

        validate: {
            name: (value) =>
                value.trim().length === 0
                    ? "Field name is required"
                    : value.length > 30
                    ? "Name must be shorter than 30 characters"
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
                    ? "Formula cannot exceed 500 characters"
                    : null,
            selectOptions: (values, form) =>
                form.type === "number" && values?.some((v) => isNaN(Number(v)))
                    ? "All suggested options for number fields must be valid numbers"
                    : null,
        },
    });

    const handleModeChange = (value: string) => {
        const isCalc = value === "calculated";
        form.setFieldValue("isCalculated", isCalc);
        if (isCalc) {
            form.setFieldValue("required", false);
            // Switch to a calculated-compatible type if current type isn't compatible
            const calcTypes = calculatedFieldTypes.map((t) => t.value);
            if (!calcTypes.includes(form.values.type)) {
                form.setFieldValue("type", "number");
            }
        }
    };

    const handleSubmit = async (values: CreateFieldDto & UpdateFieldDto) => {
        const payload = {
            ...values,
            formula: values.isCalculated ? values.formula : undefined,
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
            title={props.fieldId ? "Edit Field" : "Create Field"}
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
                        label="Field Name"
                        placeholder="Enter field name"
                        maxLength={30}
                        {...form.getInputProps("name")}
                    />

                    <Select
                        allowDeselect={false}
                        label="Type"
                        placeholder="Choose field type"
                        data={isCalculated ? calculatedFieldTypes : fieldTypes}
                        required
                        {...form.getInputProps("type")}
                    />

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
                                Reference fields/constants using {"{FieldName}"}. For timespan fields use {"{Field.hours}"}, {"{Field.minutes}"}, or {"{Field.seconds}"}. Supported input types: number, bool, timespan. Supported operators: +, -, *, /, % (booleans resolve to 1 or 0; division by 0 yields no result).
                            </Text>
                        </Stack>
                    )}

                    <Textarea
                        label="Description"
                        placeholder="Enter field description"
                        autosize
                        maxLength={500}
                        {...form.getInputProps("description")}
                    />

                    {!isCalculated && (form.values.type === "string" || form.values.type === "number") && (
                        <TagsInput
                            label="Suggested Options"
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

                    <Button color={props.tracker.color} type="submit">
                        {props.fieldId ? "Update" : "Create"}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
