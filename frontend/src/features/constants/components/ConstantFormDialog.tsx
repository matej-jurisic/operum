import {
    ActionIcon,
    Button,
    Group,
    Modal,
    NumberInput,
    Paper,
    Select,
    Stack,
    Text,
    TextInput,
    Title,
} from "@mantine/core";
import { TimePicker } from "@mantine/dates";
import { useForm } from "@mantine/form";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import {
    calculatedFieldTypes,
    operatorTypes,
} from "../../../shared/constants/DataTypesForSelect";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import FieldValueInput from "../../fields/components/FieldValueInput";
import { useFields } from "../../fields/context/FieldsContext";
import { FieldDto } from "../../fields/types/FieldDto";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import {
    CreateTrackerConstantDto,
    CreateTrackerConstantValueDto,
    CreateTrackerConstantValueFilterDto,
    TrackerConstantDto,
} from "../types/TrackerConstantDto";

const MAX_CONDITIONAL_VALUES = 6;
const MAX_FILTERS_PER_VALUE = 6;

interface ConstantFormDialogProps {
    tracker: TrackerDto;
    constantId?: string;
    initialValues?: TrackerConstantDto;
    onClose: () => void;
    onCreate: (values: CreateTrackerConstantDto) => Promise<void>;
    onUpdate: (
        constantId: string,
        values: CreateTrackerConstantDto,
    ) => Promise<void>;
}

// Separate local form type so filter values can be typed (Date, number, etc.)
type FilterRow = {
    fieldId: string;
    operator: string;
    value?: string | number | Date;
};
type ConditionalValueRow = {
    priority: number;
    value: string;
    filters: FilterRow[];
};
type FormValues = {
    name: string;
    type: string;
    value: string;
    values: ConditionalValueRow[];
};

function validateValueForType(value: string, type: string): string | null {
    if (!value.trim()) return "Value is required";
    if (type === "number" && isNaN(Number(value)))
        return "Value must be a valid number";
    if (type === "bool" && value !== "true" && value !== "false")
        return "Value must be 'true' or 'false'";
    if (type === "timespan") {
        const parts = value.split(":");
        if (
            parts.length < 2 ||
            parts.length > 3 ||
            parts.some((p) => isNaN(Number(p)))
        )
            return "Value must be a valid timespan (e.g. 01:30:00)";
    }
    return null;
}

interface ConstantValueInputProps {
    type: string;
    label?: string;
    description?: string;
    value: string;
    error?: React.ReactNode;
    onChange: (val: string) => void;
    [key: string]: unknown;
}

function ConstantValueInput({
    type,
    label,
    description,
    value,
    error,
    onChange,
    ...rest
}: ConstantValueInputProps) {
    if (type === "number") {
        return (
            <NumberInput
                label={label}
                description={description}
                value={value === "" ? "" : Number(value)}
                error={error}
                onChange={(v) => onChange(String(v))}
                {...rest}
            />
        );
    }
    if (type === "bool") {
        return (
            <Select
                label={label}
                description={description}
                allowDeselect={false}
                data={[
                    { value: "true", label: "Yes" },
                    { value: "false", label: "No" },
                ]}
                value={value}
                error={error}
                onChange={(v) => onChange(v ?? "")}
                {...rest}
            />
        );
    }
    if (type === "timespan") {
        return (
            <TimePicker
                label={label ? `${label} (hh:mm:ss)` : undefined}
                description={description}
                withSeconds
                format="24h"
                value={value}
                error={error}
                onChange={onChange}
                {...rest}
            />
        );
    }
    return (
        <TextInput
            label={label}
            description={description}
            value={value}
            error={error}
            onChange={(e) => onChange(e.currentTarget.value)}
            {...rest}
        />
    );
}

export function ConstantFormDialog(props: ConstantFormDialogProps) {
    const { fields } = useFields();

    const toFilterRow = (
        f: CreateTrackerConstantValueFilterDto,
    ): FilterRow => ({
        fieldId: f.fieldId,
        operator: f.operator,
        value: f.value ?? undefined,
    });

    const form = useForm<FormValues>({
        initialValues: props.initialValues
            ? {
                  name: props.initialValues.name,
                  type: props.initialValues.type,
                  value: props.initialValues.value,
                  values: props.initialValues.values
                      .slice()
                      .sort((a, b) => a.priority - b.priority)
                      .map((v) => ({
                          priority: v.priority,
                          value: v.value,
                          filters: v.filters.map(toFilterRow),
                      })),
              }
            : { name: "", type: "number", value: "", values: [] },

        validate: {
            name: (value) =>
                !value.trim()
                    ? "Name is required"
                    : value.length > 30
                      ? "Name cannot exceed 30 characters"
                      : null,
            type: (value) => (!value ? "Type is required" : null),
            value: (value, values) => validateValueForType(value, values.type),
        },
    });

    const fieldOptions = fields.map((f) => ({ value: f.id, label: f.name }));
    const getFieldById = (id: string): FieldDto | undefined =>
        fields.find((f) => f.id === id);

    const addConditionalValue = () => {
        if (form.values.values.length >= MAX_CONDITIONAL_VALUES) return;
        form.insertListItem("values", {
            priority: form.values.values.length + 1,
            value: "",
            filters: [{ fieldId: "", operator: "", value: undefined }],
        } as ConditionalValueRow);
    };

    const removeConditionalValue = (index: number) => {
        form.removeListItem("values", index);
    };

    const addFilter = (vi: number) => {
        if (form.values.values[vi].filters.length >= MAX_FILTERS_PER_VALUE)
            return;
        form.insertListItem(`values.${vi}.filters`, {
            fieldId: "",
            operator: "",
            value: undefined,
        });
    };

    const removeFilter = (vi: number, fi: number) => {
        form.removeListItem(`values.${vi}.filters`, fi);
    };

    const validateConditionalValues = (): boolean => {
        let valid = true;
        form.values.values.forEach((cv, vi) => {
            const valueError = validateValueForType(cv.value, form.values.type);
            if (valueError) {
                form.setFieldError(`values.${vi}.value`, valueError);
                valid = false;
            }
            if (cv.filters.length === 0) {
                form.setFieldError(
                    `values.${vi}`,
                    "At least one filter is required",
                );
                valid = false;
            }
            cv.filters.forEach((f, fi) => {
                if (!f.fieldId) {
                    form.setFieldError(
                        `values.${vi}.filters.${fi}.fieldId`,
                        "Field is required",
                    );
                    valid = false;
                }
                if (!f.operator) {
                    form.setFieldError(
                        `values.${vi}.filters.${fi}.operator`,
                        "Operator is required",
                    );
                    valid = false;
                }
            });
        });
        return valid;
    };

    const handleSubmit = async (values: FormValues) => {
        if (!validateConditionalValues()) return;

        const dto: CreateTrackerConstantDto = {
            name: values.name,
            type: values.type,
            value: values.value,
            values: values.values.map(
                (cv): CreateTrackerConstantValueDto => ({
                    priority: cv.priority,
                    value: cv.value,
                    filters: cv.filters.map(
                        (f): CreateTrackerConstantValueFilterDto => {
                            const field = getFieldById(f.fieldId);
                            const strValue =
                                f.value !== undefined && field
                                    ? GetStringValue(field.type, f.value) ||
                                      null
                                    : null;
                            return {
                                fieldId: f.fieldId,
                                operator: f.operator,
                                value: strValue,
                            };
                        },
                    ),
                }),
            ),
        };

        if (props.constantId) {
            await props.onUpdate(props.constantId, dto);
        } else {
            await props.onCreate(dto);
        }
        props.onClose();
        form.reset();
    };

    return (
        <Modal
            opened
            onClose={props.onClose}
            title={props.constantId ? "Edit Constant" : "Create Constant"}
            centered
            size="lg"
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack align="stretch" gap="md">
                    <TextInput
                        label="Name"
                        placeholder="e.g. TAX_RATE"
                        maxLength={30}
                        {...form.getInputProps("name")}
                    />

                    <Select
                        allowDeselect={false}
                        label="Type"
                        placeholder="Choose type"
                        data={calculatedFieldTypes}
                        required
                        {...form.getInputProps("type")}
                        onChange={(val) => {
                            form.setFieldValue("type", val ?? "");
                            // Clear values/value when type changes — they'd be invalid for the new type
                            form.setFieldValue("value", "");
                            form.setFieldValue(
                                "values",
                                form.values.values.map((cv) => ({
                                    ...cv,
                                    value: "",
                                })),
                            );
                        }}
                    />

                    <ConstantValueInput
                        type={form.values.type}
                        label="Base Value"
                        description="Used when no conditional value matches"
                        value={form.values.value}
                        error={form.errors.value}
                        onChange={(v) => form.setFieldValue("value", v)}
                    />

                    {/* Conditional Values */}
                    <Stack gap="sm">
                        <Group justify="space-between" align="center">
                            <Title order={5}>
                                Conditional Values
                                {form.values.values.length > 0 && (
                                    <Text
                                        span
                                        c="dimmed"
                                        size="sm"
                                        ml="xs"
                                        fw={400}
                                    >
                                        ({form.values.values.length}/
                                        {MAX_CONDITIONAL_VALUES})
                                    </Text>
                                )}
                            </Title>
                            <Button
                                variant="outline"
                                color={props.tracker.color}
                                size="sm"
                                leftSection={<FiPlus size={14} />}
                                onClick={addConditionalValue}
                                disabled={
                                    form.values.values.length >=
                                    MAX_CONDITIONAL_VALUES
                                }
                            >
                                Add
                            </Button>
                        </Group>

                        {form.values.values.length === 0 ? (
                            <Paper p="md" withBorder>
                                <Text c="dimmed" ta="center" size="sm">
                                    No conditional values. The base value is
                                    always used.
                                </Text>
                            </Paper>
                        ) : (
                            <Stack gap="sm">
                                {form.values.values.map((cv, vi) => (
                                    <Paper key={vi} p="md" withBorder>
                                        <Stack gap="md">
                                            {/* Priority + Value + Delete — all aligned to bottom */}
                                            <Group gap="md" align="flex-end">
                                                <NumberInput
                                                    label="Priority"
                                                    description="Lower = higher priority"
                                                    min={1}
                                                    w={100}
                                                    {...form.getInputProps(
                                                        `values.${vi}.priority`,
                                                    )}
                                                />
                                                <ConstantValueInput
                                                    type={form.values.type}
                                                    label="Value"
                                                    value={cv.value}
                                                    error={
                                                        form.errors[
                                                            `values.${vi}.value`
                                                        ]
                                                    }
                                                    onChange={(v) =>
                                                        form.setFieldValue(
                                                            `values.${vi}.value`,
                                                            v,
                                                        )
                                                    }
                                                    style={{ flex: 1 }}
                                                />
                                                <ActionIcon
                                                    color="red"
                                                    variant="outline"
                                                    size="lg"
                                                    onClick={() =>
                                                        removeConditionalValue(
                                                            vi,
                                                        )
                                                    }
                                                    aria-label="Remove conditional value"
                                                >
                                                    <MdDelete size={16} />
                                                </ActionIcon>
                                            </Group>

                                            {/* Filters */}
                                            <Stack gap="xs">
                                                <Group
                                                    justify="space-between"
                                                    align="center"
                                                >
                                                    <Text size="sm" fw={500}>
                                                        Filters
                                                        <Text
                                                            span
                                                            c="dimmed"
                                                            size="xs"
                                                            ml={4}
                                                        >
                                                            ({cv.filters.length}
                                                            /
                                                            {
                                                                MAX_FILTERS_PER_VALUE
                                                            }
                                                            )
                                                        </Text>
                                                    </Text>
                                                    <Button
                                                        variant="subtle"
                                                        size="xs"
                                                        leftSection={
                                                            <FiPlus size={12} />
                                                        }
                                                        onClick={() =>
                                                            addFilter(vi)
                                                        }
                                                        disabled={
                                                            cv.filters.length >=
                                                            MAX_FILTERS_PER_VALUE
                                                        }
                                                    >
                                                        Add Filter
                                                    </Button>
                                                </Group>

                                                {cv.filters.map((f, fi) => {
                                                    const selectedField =
                                                        getFieldById(f.fieldId);
                                                    return (
                                                        <Paper
                                                            key={fi}
                                                            p="sm"
                                                            withBorder
                                                        >
                                                            <Stack gap="sm">
                                                                <Group gap="md">
                                                                    <Select
                                                                        flex={1}
                                                                        allowDeselect={
                                                                            false
                                                                        }
                                                                        label="Field"
                                                                        placeholder="Select field"
                                                                        data={
                                                                            fieldOptions
                                                                        }
                                                                        {...form.getInputProps(
                                                                            `values.${vi}.filters.${fi}.fieldId`,
                                                                        )}
                                                                        onChange={(
                                                                            val,
                                                                        ) => {
                                                                            form.setFieldValue(
                                                                                `values.${vi}.filters.${fi}`,
                                                                                {
                                                                                    fieldId:
                                                                                        val ??
                                                                                        "",
                                                                                    operator:
                                                                                        "",
                                                                                    value: undefined,
                                                                                },
                                                                            );
                                                                        }}
                                                                    />
                                                                    <Select
                                                                        allowDeselect={
                                                                            false
                                                                        }
                                                                        flex={1}
                                                                        label="Operator"
                                                                        placeholder="Select operator"
                                                                        data={
                                                                            operatorTypes
                                                                        }
                                                                        {...form.getInputProps(
                                                                            `values.${vi}.filters.${fi}.operator`,
                                                                        )}
                                                                    />
                                                                </Group>
                                                                <Group
                                                                    gap="md"
                                                                    align="flex-end"
                                                                    justify="flex-end"
                                                                >
                                                                    {selectedField && (
                                                                        <FieldValueInput
                                                                            field={
                                                                                selectedField
                                                                            }
                                                                            form={
                                                                                form as any
                                                                            }
                                                                            fieldPath={`values.${vi}.filters.${fi}.value`}
                                                                            styles={{
                                                                                flex: 1,
                                                                            }}
                                                                        />
                                                                    )}
                                                                    <ActionIcon
                                                                        color="red"
                                                                        variant="outline"
                                                                        size="lg"
                                                                        onClick={() =>
                                                                            removeFilter(
                                                                                vi,
                                                                                fi,
                                                                            )
                                                                        }
                                                                        aria-label="Remove filter"
                                                                    >
                                                                        <MdDelete
                                                                            size={
                                                                                18
                                                                            }
                                                                        />
                                                                    </ActionIcon>
                                                                </Group>
                                                            </Stack>
                                                        </Paper>
                                                    );
                                                })}
                                            </Stack>
                                        </Stack>
                                    </Paper>
                                ))}
                            </Stack>
                        )}
                    </Stack>

                    <Button color={props.tracker.color} type="submit">
                        {props.constantId ? "Update" : "Create"}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
