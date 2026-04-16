import {
    ActionIcon,
    Button,
    Card,
    Group,
    Menu,
    Modal,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Text,
    Textarea,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { FiPlus, FiPlusSquare } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import { operatorTypes } from "../../../shared/constants/DataTypesForSelect";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import FieldValueInput from "../../fields/components/FieldValueInput";
import { useFields } from "../../fields/context/FieldsContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateViewDto } from "../types/requests/CreateViewDto";
import { UpdateViewDto } from "../types/requests/UpdateViewDto";

interface ViewFormValues {
    name: string;
    description?: string;
    sorts: { fieldId: string; descending: boolean }[];
    filters: { fieldId: string; operator: string; value?: string | number | Date }[];
}
import { ViewDto } from "../types/ViewDto";
import {
    DynamicDateTokens,
    dynamicDateTokenOptions,
    isDynamicDateToken,
} from "../../../shared/constants/dynamicDateTokens";
import { FilterTemplate, filterTemplates } from "./ViewFilterTemplates";

interface Props {
    tracker: TrackerDto;
    viewId?: string;
    initialView?: ViewDto;
    onClose: () => void;
}

function getFormValue(type: string, storedValue: string | undefined) {
    if (!storedValue) return undefined;
    switch (type) {
        case "date":
        case "datetime":
            if (isDynamicDateToken(storedValue)) return storedValue;
            return new Date(storedValue);
        case "number":
            return parseFloat(storedValue);
        case "bool":
            return storedValue.toLowerCase();
        default:
            return storedValue;
    }
}

enum OpenDialogTypes {
    AddFilterFromTemplate,
}

const MAX_SORTS = 3;
const MAX_FILTERS = 6;

export default function ViewFormDialog({ tracker, viewId, initialView, onClose }: Props) {
    const { fields } = useFields();
    const { createView, updateView } = useTrackerOperations();

    const [openDialogType, setOpenDialogType] = useState<OpenDialogTypes>();
    const [selectedFieldForTemplate, setSelectedFieldForTemplate] =
        useState<string>("");

    const form = useForm<ViewFormValues>({
        initialValues: initialView
            ? {
                  name: initialView.name,
                  description: initialView.description,
                  sorts: initialView.sorts.map((s) => ({
                      fieldId: s.field.id,
                      descending: s.descending,
                  })),
                  filters: initialView.filters.map((f) => ({
                      fieldId: f.field.id,
                      operator: f.operator,
                      value: getFormValue(f.field.type, f.value),
                  })),
              }
            : {
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

    const applyTemplate = (template: FilterTemplate) => {
        if (!selectedFieldForTemplate) return;

        const remainingSlots = MAX_FILTERS - form.values.filters.length;
        const filtersToAdd = template.filters.slice(0, remainingSlots);

        filtersToAdd.forEach((templateFilter) => {
            // Generate the value
            let value = templateFilter.value;
            if (templateFilter.valueGenerator) {
                value = templateFilter.valueGenerator();
            }

            form.insertListItem("filters", {
                fieldId: selectedFieldForTemplate,
                operator: templateFilter.operator,
                value: value,
            });
        });

        setOpenDialogType(undefined);
        setSelectedFieldForTemplate("");
    };

    const getAvailableTemplatesForField = (fieldId: string) => {
        const field = getFieldById(fieldId);
        if (!field) return [];

        return filterTemplates.filter((template) => {
            const wouldExceedLimit =
                form.values.filters.length + template.filters.length >
                MAX_FILTERS;
            if (wouldExceedLimit) return false;

            return (
                field.type !== undefined &&
                template.fieldTypes.includes(field.type)
            );
        });
    };

    const handleSubmit = async (values: ViewFormValues) => {
        const valuesToSend = {
            ...values,
            filters: values.filters.map((filter) => {
                const field = getFieldById(filter.fieldId);
                if (field) {
                    return {
                        ...filter,
                        value:
                            filter.value !== undefined
                                ? isDynamicDateToken(filter.value)
                                    ? filter.value
                                    : GetStringValue(field.type, filter.value)
                                : undefined,
                    };
                }
                return filter;
            }),
        };
        if (viewId) {
            await updateView(viewId, valuesToSend as UpdateViewDto);
        } else {
            await createView(valuesToSend as CreateViewDto);
        }
        onClose();
        form.reset();
    };

    return (
        <Modal opened centered onClose={onClose} title={viewId ? "Edit View" : "Create View"} size="lg">
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
                        <Textarea
                            label="Description"
                            placeholder="Enter view description"
                            maxLength={500}
                            autosize
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
                                variant="outline"
                                leftSection={<FiPlus size={14} />}
                                onClick={addSort}
                                size="sm"
                                disabled={!canAddSort}
                            >
                                Add
                            </Button>
                        </Group>

                        {form.values.sorts.length === 0 ? (
                            <Paper p="md" withBorder>
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
                            <Group>
                                <Menu position="bottom-end">
                                    <Menu.Target>
                                        <Button
                                            color={tracker.color}
                                            variant="outline"
                                            leftSection={<FiPlus size={14} />}
                                            size="sm"
                                            disabled={!canAddFilter}
                                        >
                                            Add
                                        </Button>
                                    </Menu.Target>
                                    <Menu.Dropdown>
                                        <Menu.Item
                                            leftSection={<FiPlus size={16} />}
                                            onClick={addFilter}
                                        >
                                            Add Manually
                                        </Menu.Item>
                                        <Menu.Item
                                            leftSection={
                                                <FiPlusSquare size={14} />
                                            }
                                            onClick={() => {
                                                setSelectedFieldForTemplate("");
                                                setOpenDialogType(
                                                    OpenDialogTypes.AddFilterFromTemplate
                                                );
                                            }}
                                        >
                                            Add From Template
                                        </Menu.Item>
                                    </Menu.Dropdown>
                                </Menu>
                            </Group>
                        </Group>

                        {form.values.filters.length === 0 ? (
                            <Paper p="md" withBorder>
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
                                                    {selectedField && (() => {
                                                        const isDateType = selectedField.type === "date" || selectedField.type === "datetime";
                                                        const isDynamic = isDynamicDateToken(filter.value);
                                                        return (
                                                            <Group flex={1} align="flex-end" gap="xs">
                                                                {isDateType && (
                                                                    <SegmentedControl
                                                                        size="xs"
                                                                        data={[
                                                                            { value: "fixed", label: "Fixed" },
                                                                            { value: "dynamic", label: "Dynamic" },
                                                                        ]}
                                                                        value={isDynamic ? "dynamic" : "fixed"}
                                                                        onChange={(v) =>
                                                                            form.setFieldValue(
                                                                                `filters.${index}.value`,
                                                                                v === "dynamic" ? DynamicDateTokens.Today : undefined
                                                                            )
                                                                        }
                                                                    />
                                                                )}
                                                                {isDateType && isDynamic ? (
                                                                    <Select
                                                                        flex={1}
                                                                        label={selectedField.name}
                                                                        placeholder="Select dynamic value"
                                                                        data={dynamicDateTokenOptions}
                                                                        value={typeof filter.value === "string" ? filter.value : null}
                                                                        onChange={(v) =>
                                                                            form.setFieldValue(`filters.${index}.value`, v ?? undefined)
                                                                        }
                                                                        allowDeselect={false}
                                                                    />
                                                                ) : (
                                                                    <FieldValueInput
                                                                        field={selectedField}
                                                                        form={form}
                                                                        fieldPath={`filters.${index}.value`}
                                                                        styles={{ flex: 1 }}
                                                                    />
                                                                )}
                                                            </Group>
                                                        );
                                                    })()}

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
                            {viewId ? "Update View" : "Create View"}
                        </Button>
                    </Stack>
                </Stack>
            </form>

            {/* Template Selection Modal */}
            {openDialogType === OpenDialogTypes.AddFilterFromTemplate && (
                <Modal
                    opened
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedFieldForTemplate("");
                    }}
                    centered
                    title="Choose Filter Template"
                    size="md"
                >
                    <Stack gap="md">
                        <Text size="sm" c="dimmed">
                            First select a field, then choose from available
                            templates for that field type
                        </Text>

                        {/* Field Selection */}
                        <Select
                            label="Select Field"
                            placeholder="Choose a field to filter on"
                            allowDeselect={false}
                            data={fieldOptions}
                            value={selectedFieldForTemplate}
                            onChange={(value) =>
                                setSelectedFieldForTemplate(value || "")
                            }
                            clearable
                        />

                        {/* Templates for Selected Field */}
                        {selectedFieldForTemplate && (
                            <>
                                <Text size="sm" fw={500}>
                                    Available Templates for "
                                    {
                                        getFieldById(selectedFieldForTemplate)
                                            ?.name
                                    }
                                    "
                                </Text>

                                {getAvailableTemplatesForField(
                                    selectedFieldForTemplate
                                ).length === 0 ? (
                                    <Paper p="md" withBorder>
                                        <Text c="dimmed" ta="center" size="sm">
                                            No templates available for this
                                            field type or you've reached the
                                            filter limit.
                                        </Text>
                                    </Paper>
                                ) : (
                                    <Stack gap="sm">
                                        {getAvailableTemplatesForField(
                                            selectedFieldForTemplate
                                        ).map((template) => (
                                            <Card
                                                key={template.id}
                                                withBorder
                                                p="md"
                                                style={{ cursor: "pointer" }}
                                                onClick={() =>
                                                    applyTemplate(template)
                                                }
                                            >
                                                <Group
                                                    justify="space-between"
                                                    align="flex-start"
                                                >
                                                    <Group gap="sm">
                                                        {template.icon}
                                                        <div>
                                                            <Text
                                                                fw={500}
                                                                size="sm"
                                                            >
                                                                {template.name}
                                                            </Text>
                                                            <Text
                                                                c="dimmed"
                                                                size="xs"
                                                            >
                                                                {
                                                                    template.description
                                                                }
                                                            </Text>
                                                        </div>
                                                    </Group>
                                                    <Text c="dimmed" size="xs">
                                                        +
                                                        {
                                                            template.filters
                                                                .length
                                                        }{" "}
                                                        filter
                                                        {template.filters
                                                            .length > 1
                                                            ? "s"
                                                            : ""}
                                                    </Text>
                                                </Group>
                                            </Card>
                                        ))}
                                    </Stack>
                                )}
                            </>
                        )}
                    </Stack>
                </Modal>
            )}
        </Modal>
    );
}
