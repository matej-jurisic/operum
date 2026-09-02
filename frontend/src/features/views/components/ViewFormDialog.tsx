import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Group,
    Menu,
    Modal,
    MultiSelect,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Text,
    Textarea,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useMediaQuery } from "@mantine/hooks";
import { useState } from "react";
import { CiFilter } from "react-icons/ci";
import { FiPlus, FiPlusSquare } from "react-icons/fi";
import {
    MdDelete,
    MdKeyboardArrowDown,
    MdKeyboardArrowUp,
} from "react-icons/md";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { FieldTypes } from "../../../shared/constants/DataTypes";
import { operatorsForFieldType } from "../../../shared/constants/DataTypesForSelect";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import {
    QueryKind,
    QueryKindColor,
    QueryKindLabel,
    QueryKinds,
} from "../../../shared/constants/QueryKinds";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { describeClause } from "../../../shared/utils/formatters/QueryFormatter";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import { useFields } from "../../fields/context/FieldsContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { ViewClauseDto } from "../types/ViewClauseDto";
import { ViewDto } from "../types/ViewDto";
import { CreateViewDto } from "../types/requests/CreateViewDto";
import { UpdateViewDto } from "../types/requests/UpdateViewDto";
import { filterTemplates } from "./ViewFilterTemplates";

interface ClauseRow {
    key: string;
    kind: QueryKind;
    fieldId: string;
    operator: string;
    value?: string | number | Date;
    descending: boolean;
}

interface ViewFormValues {
    name: string;
    description?: string;
    clauses: ClauseRow[];
    columnFieldIds: string[];
}

const MAX_FILTERS = 6;
const MAX_SORTS = 3;
const MAX_QUERIES = MAX_FILTERS + MAX_SORTS;
const DATE_TYPES: string[] = [FieldTypes.Date, FieldTypes.DateTime];

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
    const isMobile = useMediaQuery("(max-width: 48em)");
    const [templateModalOpen, setTemplateModalOpen] = useState(false);
    const [templateFieldId, setTemplateFieldId] = useState<string | null>(null);

    const getFieldById = (fieldId: string) => fields.find((f) => f.id === fieldId);

    const form = useForm<ViewFormValues>({
        initialValues: initialView
            ? {
                  name: initialView.name,
                  description: initialView.description,
                  clauses: initialView.queries.map((q) => ({
                      key: crypto.randomUUID(),
                      kind: q.kind,
                      fieldId: q.field.id,
                      operator: q.operator ?? "",
                      value:
                          q.value != null
                              ? isDynamicDateToken(q.value)
                                  ? q.value
                                  : DATE_TYPES.includes(q.field.type)
                                    ? new Date(q.value)
                                    : q.field.type === FieldTypes.Number
                                      ? Number(q.value)
                                      : q.value
                              : undefined,
                      descending: q.descending,
                  })),
                  columnFieldIds: initialView.columnFieldIds,
              }
            : { name: "", clauses: [], columnFieldIds: [] },
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
            clauses: (rows) => {
                if (rows.length > MAX_QUERIES)
                    return `A view can hold at most ${MAX_QUERIES} clauses`;
                if (rows.filter((r) => r.kind === QueryKinds.Filter).length > MAX_FILTERS)
                    return `A view can hold at most ${MAX_FILTERS} filters`;
                if (rows.filter((r) => r.kind === QueryKinds.Sort).length > MAX_SORTS)
                    return `A view can hold at most ${MAX_SORTS} sorts`;
                if (rows.some((r) => !r.fieldId))
                    return "Every clause needs a field";
                if (rows.some((r) => r.kind === QueryKinds.Filter && !r.operator))
                    return "Every filter needs an operator";
                return null;
            },
        },
    });

    const rows = form.values.clauses;
    const canAddMore = rows.length < MAX_QUERIES;

    const addRow = (partial?: Partial<ClauseRow>) =>
        form.insertListItem("clauses", {
            key: crypto.randomUUID(),
            kind: QueryKinds.Filter,
            fieldId: "",
            operator: "",
            value: undefined,
            descending: false,
            ...partial,
        });

    const removeRow = (index: number) => form.removeListItem("clauses", index);

    const moveRow = (index: number, direction: -1 | 1) => {
        const target = index + direction;
        if (target < 0 || target >= rows.length) return;
        form.reorderListItem("clauses", { from: index, to: target });
    };

    const addTemplateFor = (fieldId: string, templateId: string) => {
        const template = filterTemplates.find((t) => t.id === templateId);
        if (!template) return;
        template.filters
            .slice(0, MAX_QUERIES - rows.length)
            .forEach((f) =>
                addRow({
                    kind: QueryKinds.Filter,
                    fieldId,
                    operator: f.operator,
                    value: f.value,
                }),
            );
    };

    const openTemplateModal = () => {
        setTemplateFieldId(null);
        setTemplateModalOpen(true);
    };

    const closeTemplateModal = () => {
        setTemplateModalOpen(false);
        setTemplateFieldId(null);
    };

    const templateField = templateFieldId
        ? getFieldById(templateFieldId)
        : undefined;

    const availableTemplates = templateField
        ? filterTemplates.filter(
              (t) =>
                  t.fieldTypes.includes(templateField.type) &&
                  rows.length + t.filters.length <= MAX_QUERIES,
          )
        : [];

    const applyTemplate = (templateId: string) => {
        if (!templateFieldId) return;
        addTemplateFor(templateFieldId, templateId);
        closeTemplateModal();
    };

    const handleSubmit = async (values: ViewFormValues) => {
        const clauses: ViewClauseDto[] = values.clauses.map((row) => {
            if (row.kind === QueryKinds.Sort)
                return {
                    kind: QueryKinds.Sort,
                    fieldId: row.fieldId,
                    descending: row.descending,
                };
            const field = getFieldById(row.fieldId);
            return {
                kind: QueryKinds.Filter,
                fieldId: row.fieldId,
                operator: row.operator,
                value:
                    row.value !== undefined && field
                        ? isDynamicDateToken(row.value)
                            ? (row.value as string)
                            : GetStringValue(field.type, row.value)
                        : undefined,
            };
        });

        const dto = {
            name: values.name,
            description: values.description,
            columnFieldIds: values.columnFieldIds,
            queries: clauses,
        };

        if (viewId) await updateView(viewId, dto as UpdateViewDto);
        else await createView(dto as CreateViewDto);
        onClose();
        form.reset();
    };

    const templateFields = fields.filter((f) =>
        filterTemplates.some((t) => t.fieldTypes.includes(f.type)),
    );

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
                        <Group justify="space-between" wrap="nowrap">
                            <Text fw={500} size="md">
                                Clauses
                                {rows.length > 0 && (
                                    <Text span c="dimmed" size="sm" ml="xs">
                                        ({rows.length}/{MAX_QUERIES})
                                    </Text>
                                )}
                            </Text>
                            <Menu position="bottom-end">
                                <Menu.Target>
                                    <Button
                                        color={tracker.color}
                                        variant="outline"
                                        leftSection={<FiPlus size={14} />}
                                        size="sm"
                                        disabled={!canAddMore}
                                    >
                                        Add
                                    </Button>
                                </Menu.Target>
                                <Menu.Dropdown>
                                    <Menu.Item
                                        leftSection={<CiFilter size={16} />}
                                        onClick={() => addRow()}
                                    >
                                        Filter or sort
                                    </Menu.Item>
                                    <Menu.Item
                                        leftSection={<FiPlusSquare size={14} />}
                                        disabled={templateFields.length === 0}
                                        onClick={openTemplateModal}
                                    >
                                        From a template
                                    </Menu.Item>
                                </Menu.Dropdown>
                            </Menu>
                        </Group>

                        {rows.length === 0 ? (
                            <Paper p="md" withBorder>
                                <Text c="dimmed" ta="center" size="sm">
                                    This view has no clauses yet, so it matches
                                    every entry.
                                </Text>
                            </Paper>
                        ) : (
                            <Stack gap="sm">
                                {rows.map((row, index) => {
                                    const field = getFieldById(row.fieldId);
                                    const isDate =
                                        field != null &&
                                        DATE_TYPES.includes(field.type);
                                    return (
                                        <Paper key={row.key} p="md" withBorder>
                                            <Stack gap="sm">
                                                <Group
                                                    justify="space-between"
                                                    wrap="nowrap"
                                                >
                                                    <SegmentedControl
                                                        size="xs"
                                                        data={[
                                                            {
                                                                value: QueryKinds.Filter,
                                                                label: QueryKindLabel.filter,
                                                            },
                                                            {
                                                                value: QueryKinds.Sort,
                                                                label: QueryKindLabel.sort,
                                                            },
                                                        ]}
                                                        value={row.kind}
                                                        onChange={(kind) => {
                                                            form.setFieldValue(
                                                                `clauses.${index}.kind`,
                                                                kind as QueryKind,
                                                            );
                                                            form.setFieldValue(
                                                                `clauses.${index}.operator`,
                                                                "",
                                                            );
                                                            form.setFieldValue(
                                                                `clauses.${index}.value`,
                                                                undefined,
                                                            );
                                                        }}
                                                    />
                                                    <Group gap={4} wrap="nowrap">
                                                        <ActionIcon
                                                            variant="outline"
                                                            disabled={index === 0}
                                                            onClick={() =>
                                                                moveRow(index, -1)
                                                            }
                                                            aria-label="Move up"
                                                        >
                                                            <MdKeyboardArrowUp
                                                                size={18}
                                                            />
                                                        </ActionIcon>
                                                        <ActionIcon
                                                            variant="outline"
                                                            disabled={
                                                                index ===
                                                                rows.length - 1
                                                            }
                                                            onClick={() =>
                                                                moveRow(index, 1)
                                                            }
                                                            aria-label="Move down"
                                                        >
                                                            <MdKeyboardArrowDown
                                                                size={18}
                                                            />
                                                        </ActionIcon>
                                                        <ActionIcon
                                                            variant="outline"
                                                            color="red"
                                                            onClick={() =>
                                                                removeRow(index)
                                                            }
                                                            aria-label="Remove clause"
                                                        >
                                                            <MdDelete size={16} />
                                                        </ActionIcon>
                                                    </Group>
                                                </Group>

                                                <Group gap="sm" wrap="nowrap">
                                                    <Select
                                                        flex={1}
                                                        label="Field"
                                                        placeholder="Select field"
                                                        allowDeselect={false}
                                                        searchable
                                                        data={fields.map((f) => ({
                                                            value: f.id,
                                                            label: f.name,
                                                        }))}
                                                        value={row.fieldId || null}
                                                        onChange={(fieldId) => {
                                                            form.setFieldValue(
                                                                `clauses.${index}.fieldId`,
                                                                fieldId || "",
                                                            );
                                                            form.setFieldValue(
                                                                `clauses.${index}.operator`,
                                                                "",
                                                            );
                                                            form.setFieldValue(
                                                                `clauses.${index}.value`,
                                                                undefined,
                                                            );
                                                        }}
                                                    />
                                                    {row.kind ===
                                                    QueryKinds.Filter ? (
                                                        <Select
                                                            flex={1}
                                                            label="Operator"
                                                            placeholder="Operator"
                                                            allowDeselect={false}
                                                            disabled={!field}
                                                            data={operatorsForFieldType(
                                                                field?.type,
                                                            )}
                                                            {...form.getInputProps(
                                                                `clauses.${index}.operator`,
                                                            )}
                                                            value={
                                                                row.operator ||
                                                                null
                                                            }
                                                        />
                                                    ) : (
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
                                                                row.descending
                                                                    ? "desc"
                                                                    : "asc"
                                                            }
                                                            onChange={(v) =>
                                                                form.setFieldValue(
                                                                    `clauses.${index}.descending`,
                                                                    v === "desc",
                                                                )
                                                            }
                                                        />
                                                    )}
                                                </Group>

                                                {row.kind === QueryKinds.Filter &&
                                                    field && (
                                                        <DynamicDateValueInput
                                                            isDateType={isDate}
                                                            value={row.value}
                                                            onChange={(v) =>
                                                                form.setFieldValue(
                                                                    `clauses.${index}.value`,
                                                                    v,
                                                                )
                                                            }
                                                            field={field}
                                                            form={form}
                                                            fieldPath={`clauses.${index}.value`}
                                                            label={field.name}
                                                        />
                                                    )}

                                                <Group gap="xs">
                                                    <Badge
                                                        variant="light"
                                                        color={
                                                            QueryKindColor[row.kind]
                                                        }
                                                        size="sm"
                                                    >
                                                        {describeClause({
                                                            kind: row.kind,
                                                            field,
                                                            operator: row.operator,
                                                            value:
                                                                row.value !==
                                                                    undefined &&
                                                                field
                                                                    ? isDynamicDateToken(
                                                                          row.value,
                                                                      )
                                                                        ? (row.value as string)
                                                                        : GetStringValue(
                                                                              field.type,
                                                                              row.value,
                                                                          )
                                                                    : undefined,
                                                            descending:
                                                                row.descending,
                                                        })}
                                                    </Badge>
                                                </Group>
                                            </Stack>
                                        </Paper>
                                    );
                                })}
                            </Stack>
                        )}
                        {form.errors.clauses && (
                            <Text c="red" size="xs">
                                {form.errors.clauses}
                            </Text>
                        )}
                        <Text c="dimmed" size="xs">
                            When two sorts cover the same field, the first one
                            wins.
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
                                form.setFieldValue("columnFieldIds", fieldIds)
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

        <Modal
            opened={templateModalOpen}
            centered
            onClose={closeTemplateModal}
            title="Add from a template"
            size="md"
            fullScreen={isMobile}
            zIndex={300}
        >
            <Stack gap="md">
                <Select
                    label="Field"
                    placeholder="Select a field to filter on"
                    allowDeselect={false}
                    searchable
                    data={templateFields.map((f) => ({
                        value: f.id,
                        label: f.name,
                    }))}
                    value={templateFieldId}
                    onChange={setTemplateFieldId}
                />

                {templateField && (
                    <Stack gap="xs">
                        <Text fw={500} size="sm">
                            Templates for "{templateField.name}"
                        </Text>
                        {availableTemplates.length === 0 ? (
                            <Paper p="md" withBorder>
                                <Text c="dimmed" ta="center" size="sm">
                                    No templates fit this field type, or adding
                                    one would exceed the clause limit.
                                </Text>
                            </Paper>
                        ) : (
                            availableTemplates.map((t) => (
                                <Card
                                    key={t.id}
                                    withBorder
                                    p="sm"
                                    style={{ cursor: "pointer" }}
                                    onClick={() => applyTemplate(t.id)}
                                >
                                    <Group justify="space-between" wrap="nowrap">
                                        <Group gap="sm" wrap="nowrap">
                                            {t.icon}
                                            <Text fw={500} size="sm">
                                                {t.name}
                                            </Text>
                                        </Group>
                                        <Text c="dimmed" size="xs">
                                            +{t.filters.length} filter
                                            {t.filters.length > 1 ? "s" : ""}
                                        </Text>
                                    </Group>
                                </Card>
                            ))
                        )}
                    </Stack>
                )}
            </Stack>
        </Modal>
        </>
    );
}
