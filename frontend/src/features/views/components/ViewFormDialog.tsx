import {
    ActionIcon,
    Badge,
    Button,
    Group,
    Menu,
    Modal,
    MultiSelect,
    Paper,
    Select,
    Stack,
    Text,
    Textarea,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import { CiFilter } from "react-icons/ci";
import { FiPlus, FiPlusSquare } from "react-icons/fi";
import {
    MdDelete,
    MdEdit,
    MdKeyboardArrowDown,
    MdKeyboardArrowUp,
} from "react-icons/md";
import {
    QueryKindColor,
    QueryKindLabel,
    QueryKinds,
} from "../../../shared/constants/QueryKinds";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { describeClause } from "../../../shared/utils/formatters/QueryFormatter";
import { useFields } from "../../fields/context/FieldsContext";
import QueryFormDialog from "../../queries/components/QueryFormDialog";
import QueryTemplateDialog from "../../queries/components/QueryTemplateDialog";
import { useQueries } from "../../queries/context/QueriesContext";
import { QueryDto } from "../../queries/types/QueryDto";
import { CreateQueryDto } from "../../queries/types/requests/CreateQueryDto";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateViewDto } from "../types/requests/CreateViewDto";
import { UpdateViewDto } from "../types/requests/UpdateViewDto";
import { ViewQueryRefDto } from "../types/requests/ViewQueryRefDto";
import { ViewDto } from "../types/ViewDto";

/** A row is either a saved query the view points at, or one authored here and saved with it. */
interface QueryRow {
    key: string;
    queryId?: string;
    clause?: CreateQueryDto;
}

interface ViewFormValues {
    name: string;
    description?: string;
    queries: QueryRow[];
    /**
     * Fields this view shows, in the order it shows them. Not clauses, so they are picked
     * as fields rather than listed as rows: a view naming every field would otherwise bury
     * its filters and sorts under 25 one-line entries.
     */
    columnFieldIds: string[];
}

const MAX_FILTERS = 6;
const MAX_SORTS = 3;
const MAX_QUERIES = MAX_FILTERS + MAX_SORTS;

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
    const isMobile = useMediaQuery("(max-width: 48em)");

    const [editingQuery, setEditingQuery] = useState<QueryDto>();
    const [newQueryOpen, setNewQueryOpen] = useState(false);
    const [templatesOpen, setTemplatesOpen] = useState(false);
    const [editingRowIndex, setEditingRowIndex] = useState<number>();

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
                  columnFieldIds: initialView.columnFieldIds,
              }
            : {
                  name: "",
                  queries: [],
                  columnFieldIds: [],
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
                    return `A view can hold at most ${MAX_QUERIES} queries`;
                const kinds = rows.map(kindOfRow);
                if (kinds.filter((k) => k === QueryKinds.Filter).length > MAX_FILTERS)
                    return `A view can hold at most ${MAX_FILTERS} filters`;
                if (kinds.filter((k) => k === QueryKinds.Sort).length > MAX_SORTS)
                    return `A view can hold at most ${MAX_SORTS} sorts`;
                return null;
            },
        },
    });

    const getQueryById = (queryId: string) =>
        queries.find((q) => q.id === queryId);

    const getFieldById = (fieldId: string) =>
        fields.find((f) => f.id === fieldId);

    function kindOfRow(row: QueryRow) {
        if (row.clause) return row.clause.kind;
        return row.queryId
            ? getQueryById(row.queryId)?.kind
            : undefined;
    }

    const describeRow = (row: QueryRow) => {
        if (row.clause)
            return describeClause({
                ...row.clause,
                field: getFieldById(row.clause.fieldId),
            });
        const query = row.queryId ? getQueryById(row.queryId) : undefined;
        return query ? describeClause(query) : "Unknown query";
    };

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

    const addClauses = (clauses: CreateQueryDto[]) => {
        clauses
            .slice(0, MAX_QUERIES - rows.length)
            .forEach((clause) =>
                form.insertListItem("queries", {
                    key: crypto.randomUUID(),
                    clause,
                }),
            );
    };

    const replaceClause = (index: number, clause: CreateQueryDto) =>
        form.setFieldValue(`queries.${index}`, {
            key: rows[index].key,
            clause,
        });

    const removeRow = (index: number) => form.removeListItem("queries", index);

    const moveRow = (index: number, direction: -1 | 1) => {
        const target = index + direction;
        if (target < 0 || target >= rows.length) return;
        form.reorderListItem("queries", { from: index, to: target });
    };

    const handleSubmit = async (values: ViewFormValues) => {
        const queryRefs: ViewQueryRefDto[] = values.queries.map((row) =>
            row.clause ? { newQuery: row.clause } : { queryId: row.queryId },
        );

        const dto = {
            name: values.name,
            description: values.description,
            columnFieldIds: values.columnFieldIds,
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
                fullScreen={isMobile}
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
                            <Group
                                justify="space-between"
                                align={isMobile ? "stretch" : "center"}
                                wrap={isMobile ? "wrap" : "nowrap"}
                            >
                                <Text fw={500} size="md">
                                    Queries
                                    {rows.length > 0 && (
                                        <Text span c="dimmed" size="sm" ml="xs">
                                            ({rows.length}/{MAX_QUERIES})
                                        </Text>
                                    )}
                                </Text>
                                <Group
                                    gap="xs"
                                    wrap={isMobile ? "wrap" : "nowrap"}
                                    w={isMobile ? "100%" : undefined}
                                >
                                    <Select
                                        placeholder="Add existing query"
                                        data={availableQueries.map((q) => ({
                                            value: q.id,
                                            label: describeClause(q),
                                        }))}
                                        value={null}
                                        onChange={addExisting}
                                        disabled={
                                            !canAddMore ||
                                            availableQueries.length === 0
                                        }
                                        searchable
                                        w={isMobile ? "100%" : 240}
                                    />
                                    <Menu position="bottom-end">
                                        <Menu.Target>
                                            <Button
                                                color={tracker.color}
                                                variant="outline"
                                                leftSection={
                                                    <FiPlus size={14} />
                                                }
                                                size="sm"
                                                disabled={!canAddMore}
                                                w={isMobile ? "100%" : undefined}
                                            >
                                                New Query
                                            </Button>
                                        </Menu.Target>
                                        <Menu.Dropdown>
                                            <Menu.Item
                                                leftSection={
                                                    <CiFilter size={16} />
                                                }
                                                onClick={() => {
                                                    setEditingRowIndex(
                                                        undefined,
                                                    );
                                                    setNewQueryOpen(true);
                                                }}
                                            >
                                                Filter or sort
                                            </Menu.Item>
                                            <Menu.Item
                                                leftSection={
                                                    <FiPlusSquare size={14} />
                                                }
                                                onClick={() =>
                                                    setTemplatesOpen(true)
                                                }
                                            >
                                                From a template
                                            </Menu.Item>
                                        </Menu.Dropdown>
                                    </Menu>
                                </Group>
                            </Group>

                            {rows.length === 0 ? (
                                <Paper p="md" withBorder>
                                    <Text c="dimmed" ta="center" size="sm">
                                        This view has no queries yet, so it will
                                        match every entry.
                                    </Text>
                                </Paper>
                            ) : (
                                <Stack gap="sm">
                                    {rows.map((row, index) => {
                                        const kind = kindOfRow(row);
                                        return (
                                            <Paper
                                                key={row.key}
                                                p="md"
                                                withBorder
                                            >
                                                <Group
                                                    justify="space-between"
                                                    wrap="nowrap"
                                                    align="center"
                                                >
                                                    <Stack
                                                        gap={4}
                                                        flex={1}
                                                        miw={0}
                                                    >
                                                        <Group
                                                            gap="xs"
                                                            wrap="nowrap"
                                                        >
                                                            {kind && (
                                                                <Badge
                                                                    variant="light"
                                                                    color={
                                                                        QueryKindColor[
                                                                            kind
                                                                        ]
                                                                    }
                                                                    size="sm"
                                                                >
                                                                    {
                                                                        QueryKindLabel[
                                                                            kind
                                                                        ]
                                                                    }
                                                                </Badge>
                                                            )}
                                                            <Text
                                                                size="sm"
                                                                fw={500}
                                                                className="wrapped-text"
                                                            >
                                                                {describeRow(
                                                                    row,
                                                                )}
                                                            </Text>
                                                        </Group>
                                                        <Text
                                                            size="xs"
                                                            c="dimmed"
                                                        >
                                                            {row.clause
                                                                ? "New, saved with this view"
                                                                : "Reusable query"}
                                                        </Text>
                                                    </Stack>
                                                    <Group gap={4} wrap="nowrap">
                                                        <ActionIcon
                                                            variant="subtle"
                                                            disabled={
                                                                index === 0
                                                            }
                                                            onClick={() =>
                                                                moveRow(
                                                                    index,
                                                                    -1,
                                                                )
                                                            }
                                                            aria-label="Move query up"
                                                        >
                                                            <MdKeyboardArrowUp
                                                                size={18}
                                                            />
                                                        </ActionIcon>
                                                        <ActionIcon
                                                            variant="subtle"
                                                            disabled={
                                                                index ===
                                                                rows.length - 1
                                                            }
                                                            onClick={() =>
                                                                moveRow(
                                                                    index,
                                                                    1,
                                                                )
                                                            }
                                                            aria-label="Move query down"
                                                        >
                                                            <MdKeyboardArrowDown
                                                                size={18}
                                                            />
                                                        </ActionIcon>
                                                        <ActionIcon
                                                            variant="subtle"
                                                            color="green"
                                                            onClick={() => {
                                                                if (row.clause) {
                                                                    setEditingRowIndex(
                                                                        index,
                                                                    );
                                                                    setNewQueryOpen(
                                                                        true,
                                                                    );
                                                                } else if (
                                                                    row.queryId
                                                                ) {
                                                                    setEditingQuery(
                                                                        getQueryById(
                                                                            row.queryId,
                                                                        ),
                                                                    );
                                                                }
                                                            }}
                                                            aria-label="Edit query"
                                                        >
                                                            <MdEdit size={16} />
                                                        </ActionIcon>
                                                        <ActionIcon
                                                            variant="subtle"
                                                            color="red"
                                                            onClick={() =>
                                                                removeRow(index)
                                                            }
                                                            aria-label="Remove query from view"
                                                        >
                                                            <MdDelete
                                                                size={16}
                                                            />
                                                        </ActionIcon>
                                                    </Group>
                                                </Group>
                                            </Paper>
                                        );
                                    })}
                                </Stack>
                            )}
                            {form.errors.queries && (
                                <Text c="red" size="xs">
                                    {form.errors.queries}
                                </Text>
                            )}
                            <Text c="dimmed" size="xs">
                                When two sorts cover the same field, the first
                                one wins.
                            </Text>
                        </Stack>

                        <Stack gap="md">
                            <Text fw={500} size="md">
                                Columns
                                {form.values.columnFieldIds.length > 0 && (
                                    <Text span c="dimmed" size="sm" ml="xs">
                                        ({form.values.columnFieldIds.length}/
                                        {fields.length})
                                    </Text>
                                )}
                            </Text>
                            <MultiSelect
                                placeholder={
                                    form.values.columnFieldIds.length > 0
                                        ? undefined
                                        : "Every field"
                                }
                                data={fields.map((f) => ({
                                    value: f.id,
                                    label: f.name,
                                }))}
                                value={form.values.columnFieldIds}
                                onChange={(fieldIds) =>
                                    form.setFieldValue(
                                        "columnFieldIds",
                                        fieldIds,
                                    )
                                }
                                searchable
                                clearable
                            />
                        </Stack>

                        <Button color={tracker.color} type="submit" size="md">
                            {viewId ? "Update View" : "Create View"}
                        </Button>
                    </Stack>
                </form>
            </Modal>

            {newQueryOpen && (
                <QueryFormDialog
                    tracker={tracker}
                    initialClause={
                        editingRowIndex !== undefined
                            ? rows[editingRowIndex]?.clause
                            : undefined
                    }
                    onSubmitClause={(clause) => {
                        if (editingRowIndex !== undefined)
                            replaceClause(editingRowIndex, clause);
                        else addClauses([clause]);
                    }}
                    onClose={() => {
                        setNewQueryOpen(false);
                        setEditingRowIndex(undefined);
                    }}
                />
            )}

            {templatesOpen && (
                <QueryTemplateDialog
                    remainingSlots={MAX_QUERIES - rows.length}
                    onSubmitClauses={addClauses}
                    onClose={() => setTemplatesOpen(false)}
                />
            )}

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
