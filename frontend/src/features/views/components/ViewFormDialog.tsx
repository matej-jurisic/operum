import {
    ActionIcon,
    Badge,
    Button,
    Group,
    Modal,
    Paper,
    Select,
    Stack,
    Text,
    Textarea,
    TextInput,
} from "@mantine/core";
import { useForm, UseFormReturnType } from "@mantine/form";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit, MdKeyboardArrowDown, MdKeyboardArrowUp } from "react-icons/md";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import { useFields } from "../../fields/context/FieldsContext";
import { FieldDto } from "../../fields/types/FieldDto";
import QueryFormDialog from "../../queries/components/QueryFormDialog";
import { useQueries } from "../../queries/context/QueriesContext";
import { QueryDto } from "../../queries/types/QueryDto";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { CreateViewDto } from "../types/requests/CreateViewDto";
import { ViewQueryRefDto } from "../types/requests/ViewQueryRefDto";
import { UpdateViewDto } from "../types/requests/UpdateViewDto";
import { ViewDto } from "../types/ViewDto";
import EntryFilterListEditor from "./EntryFilterListEditor";
import SortListEditor from "./SortListEditor";

interface AdhocQueryRow {
    key: string;
    queryId?: undefined;
    name: string;
    description?: string;
    sorts: { fieldId: string; descending: boolean }[];
    filters: {
        fieldId: string;
        operator: string;
        value?: string | number | Date;
    }[];
}

interface ExistingQueryRow {
    key: string;
    queryId: string;
}

type QueryRow = AdhocQueryRow | ExistingQueryRow;

const isAdhoc = (row: QueryRow): row is AdhocQueryRow => !row.queryId;

interface ViewFormValues {
    name: string;
    description?: string;
    queries: QueryRow[];
}

const MAX_QUERIES = 5;
const MAX_SORTS = 3;
const MAX_FILTERS = 6;

const newAdhocRow = (): AdhocQueryRow => ({
    key: crypto.randomUUID(),
    name: "",
    sorts: [],
    filters: [],
});

interface Props {
    tracker: TrackerDto;
    viewId?: string;
    initialView?: ViewDto;
    onClose: () => void;
}

export default function ViewFormDialog({
    tracker,
    viewId,
    initialView,
    onClose,
}: Props) {
    const { fields } = useFields();
    const { createView, updateView } = useTrackerOperations();
    const { queries, refreshQueriesIfDirty } = useQueries();
    const [editingQuery, setEditingQuery] = useState<QueryDto>();

    useEffect(() => {
        refreshQueriesIfDirty();
    }, []);

    const form = useForm<ViewFormValues>({
        initialValues: initialView
            ? {
                  name: initialView.name,
                  description: initialView.description,
                  queries: initialView.queries.map((q) => ({
                      key: crypto.randomUUID(),
                      queryId: q.id,
                  })),
              }
            : {
                  name: "",
                  queries: [],
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
            queries: (rows) => {
                if (rows.length > MAX_QUERIES)
                    return `A maximum of ${MAX_QUERIES} queries are allowed`;
                for (const row of rows) {
                    if (!isAdhoc(row)) continue;
                    if (!row.name.trim()) return "Every new query needs a name";
                    if (row.sorts.find((s) => !s.fieldId))
                        return "Every sort needs a field selected";
                    if (row.sorts.length > MAX_SORTS)
                        return `A query can have at most ${MAX_SORTS} sorts`;
                    if (
                        row.sorts.map((s) => s.fieldId).length !==
                        new Set(row.sorts.map((s) => s.fieldId)).size
                    )
                        return "Each sort field must be unique within a query";
                    if (row.filters.find((f) => !f.fieldId || !f.operator))
                        return "Every filter needs a field and operator";
                    if (row.filters.length > MAX_FILTERS)
                        return `A query can have at most ${MAX_FILTERS} filters`;
                }
                return null;
            },
        },
    });

    const rows = form.values.queries;
    const attachedQueryIds = new Set(
        rows.map((r) => r.queryId).filter((id): id is string => !!id),
    );
    const availableQueries = queries.filter((q) => !attachedQueryIds.has(q.id));
    const canAddMore = rows.length < MAX_QUERIES;

    const addExisting = (queryId: string | null) => {
        if (!queryId || !canAddMore) return;
        form.insertListItem("queries", { key: crypto.randomUUID(), queryId });
    };

    const addNew = () => {
        if (!canAddMore) return;
        form.insertListItem("queries", newAdhocRow());
    };

    const removeRow = (index: number) => form.removeListItem("queries", index);

    const moveRow = (index: number, direction: -1 | 1) => {
        const target = index + direction;
        if (target < 0 || target >= rows.length) return;
        form.reorderListItem("queries", { from: index, to: target });
    };

    const getFieldById = (fieldId: string) =>
        fields.find((f) => f.id === fieldId);

    const handleSubmit = async (values: ViewFormValues) => {
        const queryRefs: ViewQueryRefDto[] = values.queries.map((row) => {
            if (!isAdhoc(row)) return { queryId: row.queryId };
            return {
                newQuery: {
                    name: row.name,
                    description: row.description,
                    sorts: row.sorts,
                    filters: row.filters.map((filter) => {
                        const field = getFieldById(filter.fieldId);
                        if (field) {
                            return {
                                ...filter,
                                value:
                                    filter.value !== undefined
                                        ? isDynamicDateToken(filter.value)
                                            ? filter.value
                                            : GetStringValue(
                                                  field.type,
                                                  filter.value,
                                              )
                                        : undefined,
                            };
                        }
                        return filter;
                    }),
                },
            };
        });

        const dto = {
            name: values.name,
            description: values.description,
            queries: queryRefs,
        };

        if (viewId) {
            await updateView(viewId, dto as UpdateViewDto);
        } else {
            await createView(dto as CreateViewDto);
        }
        onClose();
        form.reset();
    };

    return (
        <>
            <Modal
                opened
                centered
                onClose={onClose}
                title={viewId ? "Edit View" : "Create View"}
                size="lg"
            >
                <form onSubmit={form.onSubmit(handleSubmit)}>
                    <Stack gap="lg">
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

                        <Stack gap="md">
                            <Group justify="space-between" align="center">
                                <Text fw={500} size="md">
                                    Queries
                                    {rows.length > 0 && (
                                        <Text span c="dimmed" size="sm" ml="xs">
                                            ({rows.length}/{MAX_QUERIES})
                                        </Text>
                                    )}
                                </Text>
                                <Group gap="xs">
                                    <Select
                                        placeholder="Add existing query"
                                        data={availableQueries.map((q) => ({
                                            value: q.id,
                                            label: q.name,
                                        }))}
                                        value={null}
                                        onChange={addExisting}
                                        disabled={
                                            !canAddMore ||
                                            availableQueries.length === 0
                                        }
                                        searchable
                                        w={220}
                                    />
                                    <Button
                                        color={tracker.color}
                                        variant="outline"
                                        leftSection={<FiPlus size={14} />}
                                        onClick={addNew}
                                        size="sm"
                                        disabled={!canAddMore}
                                    >
                                        New Query
                                    </Button>
                                </Group>
                            </Group>

                            {rows.length === 0 ? (
                                <Paper p="md" withBorder>
                                    <Text c="dimmed" ta="center" size="sm">
                                        This view has no queries yet — it will
                                        match every entry.
                                    </Text>
                                </Paper>
                            ) : (
                                <Stack gap="sm">
                                    {rows.map((row, index) =>
                                        row.queryId ? (
                                            <ExistingQueryCard
                                                key={row.key}
                                                query={queries.find(
                                                    (q) => q.id === row.queryId,
                                                )}
                                                index={index}
                                                total={rows.length}
                                                onMove={moveRow}
                                                onRemove={() =>
                                                    removeRow(index)
                                                }
                                                onEdit={() =>
                                                    setEditingQuery(
                                                        queries.find(
                                                            (q) =>
                                                                q.id ===
                                                                row.queryId,
                                                        ),
                                                    )
                                                }
                                            />
                                        ) : (
                                            <AdhocQueryCard
                                                key={row.key}
                                                index={index}
                                                total={rows.length}
                                                color={tracker.color}
                                                fields={fields}
                                                form={form}
                                                onMove={moveRow}
                                                onRemove={() =>
                                                    removeRow(index)
                                                }
                                            />
                                        ),
                                    )}
                                </Stack>
                            )}
                            {form.errors.queries && (
                                <Text c="red" size="xs">
                                    {form.errors.queries}
                                </Text>
                            )}
                        </Stack>

                        <Button color={tracker.color} type="submit" size="md">
                            {viewId ? "Update View" : "Create View"}
                        </Button>
                    </Stack>
                </form>
            </Modal>

            {editingQuery && (
                <QueryFormDialog
                    tracker={tracker}
                    queryId={editingQuery.id}
                    initialQuery={editingQuery}
                    onClose={() => setEditingQuery(undefined)}
                />
            )}
        </>
    );
}

function ExistingQueryCard({
    query,
    index,
    total,
    onMove,
    onRemove,
    onEdit,
}: {
    query?: QueryDto;
    index: number;
    total: number;
    onMove: (index: number, direction: -1 | 1) => void;
    onRemove: () => void;
    onEdit: () => void;
}) {
    return (
        <Paper p="md" withBorder>
            <Group justify="space-between" wrap="nowrap" align="flex-start">
                <Stack gap={4} flex={1}>
                    <Text fw={500} size="sm">
                        {query?.name ?? "Unknown query"}
                    </Text>
                    <Group gap="xs">
                        {(query?.filters.length ?? 0) > 0 && (
                            <Badge variant="light" color="blue" size="sm">
                                {query!.filters.length} filter
                                {query!.filters.length === 1 ? "" : "s"}
                            </Badge>
                        )}
                        {(query?.sorts.length ?? 0) > 0 && (
                            <Badge variant="light" color="teal" size="sm">
                                {query!.sorts.length} sort
                                {query!.sorts.length === 1 ? "" : "s"}
                            </Badge>
                        )}
                        <Badge variant="outline" color="gray" size="sm">
                            reusable
                        </Badge>
                    </Group>
                </Stack>
                <RowActions
                    index={index}
                    total={total}
                    onMove={onMove}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            </Group>
        </Paper>
    );
}

function AdhocQueryCard({
    index,
    total,
    color,
    fields,
    form,
    onMove,
    onRemove,
}: {
    index: number;
    total: number;
    color?: string;
    fields: FieldDto[];
    form: UseFormReturnType<ViewFormValues>;
    onMove: (index: number, direction: -1 | 1) => void;
    onRemove: () => void;
}) {
    return (
        <Paper p="md" withBorder>
            <Stack gap="md">
                <Group justify="space-between" align="flex-end" wrap="nowrap">
                    <TextInput
                        label="New query name"
                        placeholder="Enter query name"
                        flex={1}
                        maxLength={50}
                        {...form.getInputProps(`queries.${index}.name`)}
                    />
                    <RowActions
                        index={index}
                        total={total}
                        onMove={onMove}
                        onRemove={onRemove}
                    />
                </Group>
                <SortListEditor
                    fields={fields}
                    form={form}
                    sortsPath={`queries.${index}.sorts`}
                    color={color}
                    maxSorts={MAX_SORTS}
                />
                <EntryFilterListEditor
                    fields={fields}
                    form={form}
                    filtersPath={`queries.${index}.filters`}
                    color={color}
                    maxFilters={MAX_FILTERS}
                />
            </Stack>
        </Paper>
    );
}

function RowActions({
    index,
    total,
    onMove,
    onRemove,
    onEdit,
}: {
    index: number;
    total: number;
    onMove: (index: number, direction: -1 | 1) => void;
    onRemove: () => void;
    onEdit?: () => void;
}) {
    return (
        <Group gap={4} wrap="nowrap">
            <ActionIcon
                variant="subtle"
                disabled={index === 0}
                onClick={() => onMove(index, -1)}
                aria-label="Move query up"
            >
                <MdKeyboardArrowUp size={18} />
            </ActionIcon>
            <ActionIcon
                variant="subtle"
                disabled={index === total - 1}
                onClick={() => onMove(index, 1)}
                aria-label="Move query down"
            >
                <MdKeyboardArrowDown size={18} />
            </ActionIcon>
            {onEdit && (
                <ActionIcon
                    variant="subtle"
                    color="green"
                    onClick={onEdit}
                    aria-label="Edit query"
                >
                    <MdEdit size={16} />
                </ActionIcon>
            )}
            <ActionIcon
                variant="subtle"
                color="red"
                onClick={onRemove}
                aria-label="Remove query from view"
            >
                <MdDelete size={16} />
            </ActionIcon>
        </Group>
    );
}
