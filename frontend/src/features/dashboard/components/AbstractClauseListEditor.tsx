import {
    ActionIcon,
    Badge,
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
} from "@mantine/core";
import { UseFormReturnType } from "@mantine/form";
import { useState } from "react";
import { CiFilter } from "react-icons/ci";
import { FiPlus, FiPlusSquare } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { fieldTypes, operatorsForFieldType } from "../../../shared/constants/DataTypesForSelect";
import {
    QueryKind,
    QueryKindColor,
    QueryKindLabel,
    QueryKinds,
} from "../../../shared/constants/QueryKinds";
import { describeAbstractClause } from "../../../shared/utils/formatters/QueryFormatter";
import { FieldDto } from "../../fields/types/FieldDto";
import { filterTemplates } from "../../views/components/ViewFilterTemplates";
import { filterWidgetClauseTemplates } from "./filterWidgetClauseTemplates";

export interface AbstractClauseRow {
    kind: QueryKind;
    dataType: string;
    operator: string;
    value?: unknown;
    descending: boolean;
}

const DATE_TYPES = ["date", "datetime"];

// Lets the shared FieldDto-keyed value input render for a data-type-only clause.
const syntheticField = (path: string, index: number, type: string): FieldDto => ({
    id: `${path}.${index}`,
    name: "Value",
    type,
    required: false,
    isCalculated: false,
});

// Reference clauses need a tracker to pick from, which a data-type-only clause can't name.
const clauseFieldTypes = fieldTypes.filter((t) => t.value !== "reference");

const dataTypeLabel = (value: string) =>
    fieldTypes.find((t) => t.value === value)?.label ?? value;

/** Flattens a view filter template and a filter-widget clause template to one shared shape. */
interface ClauseTemplateOption {
    id: string;
    name: string;
    description?: string;
    icon: React.ReactNode;
    fieldTypes: string[];
    clauses: Array<{ operator: string; value?: unknown }>;
}

interface Props {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    form: UseFormReturnType<any>;
    path: string;
    color?: string;
    max?: number;
    /** Filter clauses only: hides the filter/sort toggle and forces every row to a filter. */
    filterOnly?: boolean;
}

/**
 * Edits field-agnostic clauses (kind + data type + operator/value or direction). The
 * concrete field each runs against is chosen per followed widget on the view selector, not here.
 */
export default function AbstractClauseListEditor({
    form,
    path,
    color,
    max = 9,
    filterOnly = false,
}: Props) {
    const rows: AbstractClauseRow[] = form.values[path] ?? [];
    const canAdd = rows.length < max;

    const [templateModalOpen, setTemplateModalOpen] = useState(false);
    const [templateDataType, setTemplateDataType] = useState<string | null>(null);

    const addRow = (partial?: Partial<AbstractClauseRow>) =>
        form.insertListItem(path, {
            kind: QueryKinds.Filter,
            dataType: "",
            operator: "",
            value: undefined,
            descending: false,
            ...partial,
        });

    // filterOnly rows never hold a value, so they use operator-shape templates instead of views'.
    const templates: ClauseTemplateOption[] = filterOnly
        ? filterWidgetClauseTemplates
        : filterTemplates.map((t) => ({ ...t, clauses: t.filters }));

    const templateTypeOptions = fieldTypes.filter((t) =>
        templates.some((tpl) => tpl.fieldTypes.includes(t.value)),
    );

    const availableTemplates = templateDataType
        ? templates.filter(
              (t) =>
                  t.fieldTypes.includes(templateDataType) &&
                  rows.length + t.clauses.length <= max,
          )
        : [];

    const openTemplateModal = () => {
        setTemplateDataType(null);
        setTemplateModalOpen(true);
    };

    const closeTemplateModal = () => {
        setTemplateModalOpen(false);
        setTemplateDataType(null);
    };

    const applyTemplate = (template: ClauseTemplateOption) => {
        if (!templateDataType) return;
        template.clauses
            .slice(0, max - rows.length)
            .forEach((f) =>
                addRow({
                    kind: QueryKinds.Filter,
                    dataType: templateDataType,
                    operator: f.operator,
                    value: filterOnly ? undefined : f.value,
                }),
            );
        closeTemplateModal();
    };

    return (
        <Stack gap="sm">
            <Group justify="space-between" wrap="nowrap">
                <Text fw={500} size="sm">
                    {filterOnly ? "Live Clauses" : "Clauses"}
                    {rows.length > 0 && (
                        <Text span c="dimmed" size="sm" ml="xs">
                            ({rows.length}/{max})
                        </Text>
                    )}
                </Text>
                <Menu position="bottom-end">
                    <Menu.Target>
                        <Button
                            color={color}
                            variant="outline"
                            leftSection={<FiPlus size={14} />}
                            size="xs"
                            disabled={!canAdd}
                        >
                            Add
                        </Button>
                    </Menu.Target>
                    <Menu.Dropdown>
                        <Menu.Item
                            leftSection={<CiFilter size={16} />}
                            onClick={() => addRow()}
                        >
                            {filterOnly ? "Filter" : "Filter or sort"}
                        </Menu.Item>
                        <Menu.Item
                            leftSection={<FiPlusSquare size={14} />}
                            disabled={templateTypeOptions.length === 0}
                            onClick={openTemplateModal}
                        >
                            From a template
                        </Menu.Item>
                    </Menu.Dropdown>
                </Menu>
            </Group>

            {rows.length === 0 ? (
                <Paper p="md" withBorder radius="md">
                    <Text c="dimmed" ta="center" size="sm">
                        No clauses yet. Add one, or start from a template.
                    </Text>
                </Paper>
            ) : (
                rows.map((row, index) => {
                    const isDate = DATE_TYPES.includes(row.dataType);
                    if (filterOnly) {
                        return (
                            <Paper key={index} p="xs" withBorder radius="md">
                                <Group gap="sm" wrap="nowrap" align="flex-end">
                                    <Select
                                        flex={1}
                                        label="Type"
                                        allowDeselect={false}
                                        data={clauseFieldTypes}
                                        value={row.dataType || null}
                                        onChange={(value) =>
                                            form.setFieldValue(`${path}.${index}`, {
                                                ...row,
                                                dataType: value ?? "",
                                                operator: "",
                                                value: undefined,
                                            })
                                        }
                                    />
                                    <Select
                                        flex={1}
                                        label="Operator"
                                        allowDeselect={false}
                                        disabled={!row.dataType}
                                        data={operatorsForFieldType(
                                            row.dataType || undefined,
                                        )}
                                        {...form.getInputProps(
                                            `${path}.${index}.operator`,
                                        )}
                                        value={row.operator || null}
                                    />
                                    <ActionIcon
                                        color="red"
                                        variant="outline"
                                        aria-label="Remove clause"
                                        onClick={() =>
                                            form.removeListItem(path, index)
                                        }
                                    >
                                        <MdDelete size={16} />
                                    </ActionIcon>
                                </Group>
                            </Paper>
                        );
                    }
                    const described =
                        row.dataType &&
                        (row.kind === QueryKinds.Sort || row.operator);
                    return (
                        <Paper key={index} p="md" withBorder radius="md">
                            <Stack gap="sm">
                                <Group justify="space-between" wrap="nowrap">
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
                                        onChange={(kind) =>
                                            form.setFieldValue(
                                                `${path}.${index}`,
                                                {
                                                    ...row,
                                                    kind: kind as QueryKind,
                                                    operator: "",
                                                    value: undefined,
                                                },
                                            )
                                        }
                                    />
                                    <ActionIcon
                                        color="red"
                                        variant="outline"
                                        aria-label="Remove clause"
                                        onClick={() =>
                                            form.removeListItem(path, index)
                                        }
                                    >
                                        <MdDelete size={16} />
                                    </ActionIcon>
                                </Group>
                                <Group gap="sm" wrap="nowrap" align="flex-end">
                                    <Select
                                        flex={1}
                                        label="Type"
                                        allowDeselect={false}
                                        data={clauseFieldTypes}
                                        value={row.dataType || null}
                                        onChange={(value) =>
                                            form.setFieldValue(`${path}.${index}`, {
                                                ...row,
                                                dataType: value ?? "",
                                                operator: "",
                                                value: undefined,
                                            })
                                        }
                                    />
                                    {row.kind === QueryKinds.Filter ? (
                                        <Select
                                            flex={1}
                                            label="Operator"
                                            allowDeselect={false}
                                            disabled={!row.dataType}
                                            data={operatorsForFieldType(
                                                row.dataType || undefined,
                                            )}
                                            {...form.getInputProps(
                                                `${path}.${index}.operator`,
                                            )}
                                            value={row.operator || null}
                                        />
                                    ) : (
                                        <SegmentedControl
                                            data={[
                                                { value: "asc", label: "Asc" },
                                                { value: "desc", label: "Desc" },
                                            ]}
                                            value={row.descending ? "desc" : "asc"}
                                            onChange={(v) =>
                                                form.setFieldValue(
                                                    `${path}.${index}.descending`,
                                                    v === "desc",
                                                )
                                            }
                                        />
                                    )}
                                </Group>
                                {row.kind === QueryKinds.Filter && row.dataType && (
                                    <DynamicDateValueInput
                                        isDateType={isDate}
                                        value={
                                            row.value as
                                                | string
                                                | number
                                                | Date
                                                | undefined
                                        }
                                        onChange={(v) =>
                                            form.setFieldValue(
                                                `${path}.${index}.value`,
                                                v,
                                            )
                                        }
                                        field={syntheticField(
                                            path,
                                            index,
                                            row.dataType,
                                        )}
                                        form={form}
                                        fieldPath={`${path}.${index}.value`}
                                    />
                                )}
                                {described && (
                                    <Group gap="xs">
                                        <Badge
                                            variant="light"
                                            color={QueryKindColor[row.kind]}
                                            size="sm"
                                            styles={{
                                                label: { textTransform: "none" },
                                            }}
                                        >
                                            {describeAbstractClause(row)}
                                        </Badge>
                                    </Group>
                                )}
                            </Stack>
                        </Paper>
                    );
                })
            )}

            <Modal
                opened={templateModalOpen}
                centered
                onClose={closeTemplateModal}
                title="Add from a template"
                size="md"
                zIndex={400}
            >
                <Stack gap="md">
                    <Select
                        label="Data type"
                        placeholder="Pick the data type to filter on"
                        allowDeselect={false}
                        data={templateTypeOptions}
                        value={templateDataType}
                        onChange={setTemplateDataType}
                        // Must clear the modal's own zIndex (400) or the dropdown renders behind it.
                        comboboxProps={{ zIndex: 500 }}
                    />

                    {templateDataType && (
                        <Stack gap="xs">
                            <Text fw={500} size="sm">
                                Templates for {dataTypeLabel(templateDataType)}
                            </Text>
                            {availableTemplates.length === 0 ? (
                                <Paper p="md" withBorder>
                                    <Text c="dimmed" ta="center" size="sm">
                                        No templates fit this data type, or adding
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
                                        onClick={() => applyTemplate(t)}
                                    >
                                        <Stack gap={4}>
                                            <Group
                                                justify="space-between"
                                                wrap="nowrap"
                                            >
                                                <Group gap="sm" wrap="nowrap">
                                                    {t.icon}
                                                    <Text fw={500} size="sm">
                                                        {t.name}
                                                    </Text>
                                                </Group>
                                                <Text c="dimmed" size="xs">
                                                    +{t.clauses.length} clause
                                                    {t.clauses.length > 1
                                                        ? "s"
                                                        : ""}
                                                </Text>
                                            </Group>
                                            {t.description && (
                                                <Text c="dimmed" size="xs">
                                                    {t.description}
                                                </Text>
                                            )}
                                        </Stack>
                                    </Card>
                                ))
                            )}
                        </Stack>
                    )}
                </Stack>
            </Modal>
        </Stack>
    );
}
