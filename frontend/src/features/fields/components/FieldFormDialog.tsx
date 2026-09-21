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
import { calculatedFieldTypes, fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { trackersController } from "../../trackers/api/trackersController";
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
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);

    useEffect(() => {
        trackersController
            .getTrackerList("Accessible")
            .then((res) => setTrackers(res.data ?? []))
            .catch(() => setTrackers([]));
    }, []);

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
                  referencedTrackerId: props.initialValues.referencedTrackerId || "",
                  referencedDisplayFieldId:
                      props.initialValues.referencedDisplayFieldId || "",
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

    const handleModeChange = (value: string) => {
        const isCalc = value === "calculated";
        form.setFieldValue("isCalculated", isCalc);
        if (isCalc) {
            form.setFieldValue("required", false);
            const calcTypes = calculatedFieldTypes.map((t) => t.value);
            if (!calcTypes.includes(form.values.type)) {
                form.setFieldValue("type", "number");
            }
        }
    };

    const handleTypeChange = (value: string | null) => {
        const next = value ?? "string";
        form.setFieldValue("type", next);
        if (next === "reference") {
            form.setFieldValue("isCalculated", false);
            form.setFieldValue("selectOptions", []);
        } else {
            form.setFieldValue("referencedTrackerId", "");
            form.setFieldValue("referencedDisplayFieldId", "");
        }
    };

    const handleSubmit = async (values: CreateFieldDto & UpdateFieldDto) => {
        const payload = {
            ...values,
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
                                Reference fields and constants as {"{Name}"}, with + - * / %. Timespans also take {"{Name.hours}"}, {"{Name.minutes}"} and {"{Name.seconds}"}.
                            </Text>
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
