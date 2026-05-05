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
    Textarea,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import { useFields } from "../../fields/context/FieldsContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateViewDto } from "../types/requests/CreateViewDto";
import { UpdateViewDto } from "../types/requests/UpdateViewDto";
import { ViewDto } from "../types/ViewDto";
import EntryFilterListEditor from "./EntryFilterListEditor";

interface ViewFormValues {
    name: string;
    description?: string;
    sorts: { fieldId: string; descending: boolean }[];
    filters: {
        fieldId: string;
        operator: string;
        value?: string | number | Date;
    }[];
}

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

const MAX_SORTS = 3;
const MAX_FILTERS = 6;

export default function ViewFormDialog({
    tracker,
    viewId,
    initialView,
    onClose,
}: Props) {
    const { fields } = useFields();
    const { createView, updateView } = useTrackerOperations();


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

    const addSort = () => {
        if (!canAddSort) return;

        form.insertListItem("sorts", {
            fieldId: "",
            descending: false,
        });
    };

    const removeSort = (index: number) => {
        form.removeListItem("sorts", index);
    };

    const getFieldById = (fieldId: string) =>
        fields.find((f) => f.id === fieldId);

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
        <Modal
            opened
            centered
            onClose={onClose}
            title={viewId ? "Edit View" : "Create View"}
            size="lg"
        >
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
                                                    `sorts.${index}.fieldId`,
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
                                                        value === "desc",
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

                    <EntryFilterListEditor
                        fields={fields}
                        form={form}
                        color={tracker.color}
                        maxFilters={MAX_FILTERS}
                    />

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

        </Modal>
    );
}
