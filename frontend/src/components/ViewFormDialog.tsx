import {
    ActionIcon,
    Button,
    Group,
    Modal,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import { operatorTypes } from "../model/constants/DataTypesForSelect";
import { CreateViewDto } from "../model/requests/CreateViewDto";
import { TrackerDto } from "../model/TrackerDto";
import { GetStringValue } from "./EntryFormDialog";
import FieldValueInput from "./FieldValueInput";

interface Props {
    tracker: TrackerDto;
    onClose: () => void;
}

const MAX_SORTS = 3;
const MAX_FILTERS = 6;

export default function ViewFormDialog({ tracker, onClose }: Props) {
    const { fields, CreateView } = useTracker();

    const form = useForm<CreateViewDto>({
        initialValues: {
            name: "",
            sorts: [],
            filters: [],
        },
        validate: {
            name: (value) =>
                !value.trim()
                    ? "View name is required"
                    : value.length > 50
                    ? "View name must be at most 50 characters"
                    : null,
            description: (value) =>
                value && value.length > 500
                    ? "Description must be at most 500 characters"
                    : null,
            sorts: (sorts) => {
                if (sorts.find((s) => !s.fieldId)) {
                    return "All sorts must have a field selected";
                }
                if (sorts.length > MAX_SORTS) {
                    return `A maximum of ${MAX_SORTS} sorts are allowed`;
                }
                if (
                    sorts.map((s) => s.fieldId).length !==
                    new Set(sorts.map((s) => s.fieldId)).size
                ) {
                    return "Each sort field must be unique";
                }
                return null;
            },
            filters: (filters) => {
                if (filters.find((f) => !f.fieldId)) {
                    return "All filters must have a field selected";
                }
                if (filters.length > MAX_FILTERS) {
                    return `A maximum of ${MAX_FILTERS} filters are allowed`;
                }
                if (filters.length !== new Set(filters).size) {
                    return "Each filter must be unique";
                }
                if (filters.find((f) => !f.operator)) {
                    return "Each filter must have a operator";
                }
                return null;
            },
        },
    });

    const fieldOptions = fields.map((field) => ({
        value: field.id,
        label: field.name,
    }));

    const canAddSort = form.values.sorts.length < MAX_SORTS;
    const canAddFilter = form.values.filters.length < MAX_FILTERS;

    const addSort = () => {
        if (!canAddSort) return;

        form.insertListItem("sorts", {
            fieldId: "",
            descending: false,
        });
    };

    const addFilter = () => {
        if (!canAddFilter) return;

        form.insertListItem("filters", {
            fieldId: "",
            operator: "",
        });
    };

    const removeSort = (index: number) => {
        form.removeListItem("sorts", index);
    };

    const removeFilter = (index: number) => {
        form.removeListItem("filters", index);
    };

    const getFieldById = (fieldId: string) =>
        fields.find((f) => f.id === fieldId);

    const handleSubmit = async (values: CreateViewDto) => {
        const valuesToSend = {
            ...values,
            filters: values.filters.map((filter) => {
                const field = getFieldById(filter.fieldId);
                if (field) {
                    return {
                        ...filter,
                        value:
                            filter.value !== undefined
                                ? GetStringValue(field.type, filter.value)
                                : undefined,
                    };
                }
                return filter;
            }),
        };
        await CreateView(tracker.id, valuesToSend);
        onClose();
        form.reset();
    };

    return (
        <Modal opened centered onClose={onClose} title="Create View" size="lg">
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="lg">
                    {/* Basic Info Section */}
                    <Stack gap="md">
                        <TextInput
                            label="View Name"
                            placeholder="Enter view name"
                            required
                            maxLength={50}
                            {...form.getInputProps("name")}
                        />
                        <TextInput
                            label="Description"
                            placeholder="Enter view description"
                            maxLength={500}
                            {...form.getInputProps("description")}
                        />
                    </Stack>

                    {/* Sorts Section */}
                    <Stack gap="md">
                        <Group justify="space-between" align="center">
                            <Text fw={500} size="md">
                                Sorting Rules
                                {form.values.sorts.length > 0 && (
                                    <Text span c="dimmed" size="sm" ml="xs">
                                        ({form.values.sorts.length}/{MAX_SORTS})
                                    </Text>
                                )}
                            </Text>
                            <Button
                                color={tracker.color}
                                variant="light"
                                leftSection={<FiPlus size={14} />}
                                onClick={addSort}
                                size="sm"
                                disabled={!canAddSort}
                            >
                                Add Sort
                            </Button>
                        </Group>

                        {form.values.sorts.length === 0 ? (
                            <Paper p="md" withBorder bg="gray.0">
                                <Text c="dimmed" ta="center" size="sm">
                                    No sorting rules added yet
                                </Text>
                            </Paper>
                        ) : (
                            <Stack gap="sm">
                                {form.values.sorts.map((sort, index) => (
                                    <Paper key={index} p="md" withBorder>
                                        <Group gap="md" wrap="nowrap">
                                            <Select
                                                placeholder="Select field"
                                                allowDeselect={false}
                                                data={fieldOptions}
                                                {...form.getInputProps(
                                                    `sorts.${index}.fieldId`
                                                )}
                                                flex={1}
                                            />

                                            <SegmentedControl
                                                data={[
                                                    {
                                                        value: "asc",
                                                        label: "Asc",
                                                    },
                                                    {
                                                        value: "desc",
                                                        label: "Desc",
                                                    },
                                                ]}
                                                value={
                                                    sort.descending
                                                        ? "desc"
                                                        : "asc"
                                                }
                                                onChange={(value) =>
                                                    form.setFieldValue(
                                                        `sorts.${index}.descending`,
                                                        value === "desc"
                                                    )
                                                }
                                            />

                                            <ActionIcon
                                                color="red"
                                                variant="outline"
                                                onClick={() =>
                                                    removeSort(index)
                                                }
                                                aria-label="Remove sort"
                                                size="lg"
                                            >
                                                <MdDelete size={18} />
                                            </ActionIcon>
                                        </Group>
                                    </Paper>
                                ))}
                            </Stack>
                        )}
                    </Stack>

                    {/* Filters Section */}
                    <Stack gap="md">
                        <Group justify="space-between" align="center">
                            <Text fw={500} size="md">
                                Filtering Rules
                                {form.values.filters.length > 0 && (
                                    <Text span c="dimmed" size="sm" ml="xs">
                                        ({form.values.filters.length}/
                                        {MAX_FILTERS})
                                    </Text>
                                )}
                            </Text>
                            <Button
                                color={tracker.color}
                                variant="light"
                                leftSection={<FiPlus size={14} />}
                                onClick={addFilter}
                                size="sm"
                                disabled={!canAddFilter}
                            >
                                Add Filter
                            </Button>
                        </Group>

                        {form.values.filters.length === 0 ? (
                            <Paper p="md" withBorder bg="gray.0">
                                <Text c="dimmed" ta="center" size="sm">
                                    No filtering rules added yet
                                </Text>
                            </Paper>
                        ) : (
                            <Stack gap="sm">
                                {form.values.filters.map((filter, index) => {
                                    const selectedField = getFieldById(
                                        filter.fieldId
                                    );

                                    return (
                                        <Paper key={index} p="md" withBorder>
                                            <Stack gap="md">
                                                {/* First row: Field and Operator */}
                                                <Group gap="md">
                                                    <Select
                                                        flex={1}
                                                        allowDeselect={false}
                                                        label="Field"
                                                        placeholder="Select field"
                                                        data={fieldOptions}
                                                        {...form.getInputProps(
                                                            `filters.${index}.fieldId`
                                                        )}
                                                        onChange={(
                                                            newFieldId
                                                        ) => {
                                                            form.setFieldValue(
                                                                `filters.${index}`,
                                                                {
                                                                    fieldId:
                                                                        newFieldId ||
                                                                        "",
                                                                    operator:
                                                                        "",
                                                                    value: undefined,
                                                                }
                                                            );
                                                        }}
                                                    />

                                                    <Select
                                                        allowDeselect={false}
                                                        flex={1}
                                                        label="Operator"
                                                        placeholder="Select operator"
                                                        data={operatorTypes}
                                                        {...form.getInputProps(
                                                            `filters.${index}.operator`
                                                        )}
                                                    />
                                                </Group>

                                                {/* Second row: Value and Delete button */}
                                                <Group
                                                    gap="md"
                                                    align="flex-end"
                                                    justify="flex-end"
                                                >
                                                    {selectedField && (
                                                        <Group flex={1}>
                                                            <FieldValueInput
                                                                field={
                                                                    selectedField
                                                                }
                                                                form={form}
                                                                fieldPath={`filters.${index}.value`}
                                                                styles={{
                                                                    flex: 1,
                                                                }}
                                                            />
                                                        </Group>
                                                    )}

                                                    <ActionIcon
                                                        color="red"
                                                        variant="outline"
                                                        onClick={() =>
                                                            removeFilter(index)
                                                        }
                                                        aria-label="Remove filter"
                                                        size="lg"
                                                    >
                                                        <MdDelete size={18} />
                                                    </ActionIcon>
                                                </Group>
                                            </Stack>
                                        </Paper>
                                    );
                                })}
                            </Stack>
                        )}
                    </Stack>

                    {/* Submit Section */}
                    <Stack>
                        {form.errors.sorts && (
                            <Text c="red" size="xs">
                                {form.errors.sorts}
                            </Text>
                        )}
                        {form.errors.filters && (
                            <Text c="red" size="xs">
                                {form.errors.filters}
                            </Text>
                        )}
                        <Button color={tracker.color} type="submit" size="md">
                            Create View
                        </Button>
                    </Stack>
                </Stack>
            </form>
        </Modal>
    );
}
