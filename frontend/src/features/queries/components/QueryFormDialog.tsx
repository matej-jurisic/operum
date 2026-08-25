import { Button, Modal, Stack, Text, Textarea, TextInput } from "@mantine/core";
import { useForm } from "@mantine/form";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import EntryFilterListEditor from "../../views/components/EntryFilterListEditor";
import SortListEditor from "../../views/components/SortListEditor";
import { useFields } from "../../fields/context/FieldsContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useQueries } from "../context/QueriesContext";
import { CreateQueryDto } from "../types/requests/CreateQueryDto";
import { QueryDto } from "../types/QueryDto";

interface QueryFormValues {
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
    queryId?: string;
    initialQuery?: QueryDto;
    onClose: () => void;
    onSaved?: (query: QueryDto) => void;
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

/** Standalone create/edit dialog for a reusable Query (filters + sorts). */
export default function QueryFormDialog({
    tracker,
    queryId,
    initialQuery,
    onClose,
    onSaved,
}: Props) {
    const { fields } = useFields();
    const { _createQuery, _updateQuery } = useQueries();

    const form = useForm<QueryFormValues>({
        initialValues: initialQuery
            ? {
                  name: initialQuery.name,
                  description: initialQuery.description,
                  sorts: initialQuery.sorts.map((s) => ({
                      fieldId: s.field.id,
                      descending: s.descending,
                  })),
                  filters: initialQuery.filters.map((f) => ({
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
                    ? "Query name is required"
                    : value.length > 50
                      ? "Query name must be at most 50 characters"
                      : null,
            description: (value) =>
                value && value.length > 500
                    ? "Description must be at most 500 characters"
                    : null,
            sorts: (sorts) => {
                if (sorts.find((s) => !s.fieldId))
                    return "All sorts must have a field selected";
                if (sorts.length > MAX_SORTS)
                    return `A maximum of ${MAX_SORTS} sorts are allowed`;
                if (
                    sorts.map((s) => s.fieldId).length !==
                    new Set(sorts.map((s) => s.fieldId)).size
                )
                    return "Each sort field must be unique";
                return null;
            },
            filters: (filters) => {
                if (filters.find((f) => !f.fieldId))
                    return "All filters must have a field selected";
                if (filters.length > MAX_FILTERS)
                    return `A maximum of ${MAX_FILTERS} filters are allowed`;
                if (filters.length !== new Set(filters).size)
                    return "Each filter must be unique";
                if (filters.find((f) => !f.operator))
                    return "Each filter must have a operator";
                return null;
            },
        },
    });

    const getFieldById = (fieldId: string) =>
        fields.find((f) => f.id === fieldId);

    const handleSubmit = async (values: QueryFormValues) => {
        const dto: CreateQueryDto = {
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

        const saved = queryId
            ? await _updateQuery(queryId, dto)
            : await _createQuery(dto);
        onSaved?.(saved);
        onClose();
        form.reset();
    };

    return (
        <Modal
            opened
            centered
            onClose={onClose}
            title={queryId ? "Edit Query" : "Create Query"}
            size="lg"
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="lg">
                    <Stack gap="md">
                        <TextInput
                            label="Query Name"
                            placeholder="Enter query name"
                            required
                            maxLength={50}
                            {...form.getInputProps("name")}
                        />
                        <Textarea
                            label="Description"
                            placeholder="Enter query description"
                            maxLength={500}
                            autosize
                            {...form.getInputProps("description")}
                        />
                    </Stack>

                    <SortListEditor
                        fields={fields}
                        form={form}
                        color={tracker.color}
                        maxSorts={MAX_SORTS}
                    />

                    <EntryFilterListEditor
                        fields={fields}
                        form={form}
                        color={tracker.color}
                        maxFilters={MAX_FILTERS}
                    />

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
                            {queryId ? "Update Query" : "Create Query"}
                        </Button>
                    </Stack>
                </Stack>
            </form>
        </Modal>
    );
}
