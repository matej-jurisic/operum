import {
    ActionIcon,
    Button,
    Card,
    Group,
    Menu,
    Modal,
    Paper,
    Select,
    Stack,
    Text,
} from "@mantine/core";
import { UseFormReturnType } from "@mantine/form";
import { useState } from "react";
import { FiPlus, FiPlusSquare } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { operatorTypes } from "../../../shared/constants/DataTypesForSelect";
import { FieldDto } from "../../fields/types/FieldDto";
import { FilterTemplate, filterTemplates } from "./ViewFilterTemplates";

interface Props {
    fields: FieldDto[];
    form: UseFormReturnType<any>;
    filtersPath?: string;
    color?: string;
    maxFilters?: number;
}

export default function EntryFilterListEditor({
    fields,
    form,
    filtersPath = "filters",
    color,
    maxFilters = 6,
}: Props) {
    const [showTemplateModal, setShowTemplateModal] = useState(false);
    const [selectedFieldForTemplate, setSelectedFieldForTemplate] = useState("");

    const filters: Array<{ fieldId: string; operator: string; value?: any }> =
        (form.values as any)[filtersPath] ?? [];
    const canAdd = filters.length < maxFilters;

    const fieldOptions = fields.map((f) => ({ value: f.id, label: f.name }));

    const getFieldById = (id: string) => fields.find((f) => f.id === id);

    const getAvailableTemplates = (fieldId: string) => {
        const field = getFieldById(fieldId);
        if (!field) return [];
        return filterTemplates.filter(
            (t) =>
                field.type !== undefined &&
                t.fieldTypes.includes(field.type) &&
                filters.length + t.filters.length <= maxFilters,
        );
    };

    const applyTemplate = (template: FilterTemplate) => {
        if (!selectedFieldForTemplate) return;
        const remainingSlots = maxFilters - filters.length;
        template.filters.slice(0, remainingSlots).forEach((tf) => {
            const value = tf.valueGenerator ? tf.valueGenerator() : tf.value;
            form.insertListItem(filtersPath, {
                fieldId: selectedFieldForTemplate,
                operator: tf.operator,
                value,
            });
        });
        setShowTemplateModal(false);
        setSelectedFieldForTemplate("");
    };

    return (
        <Stack gap="md">
            <Group justify="space-between" align="center">
                <Text fw={500} size="md">
                    Filtering Rules
                    {filters.length > 0 && (
                        <Text span c="dimmed" size="sm" ml="xs">
                            ({filters.length}/{maxFilters})
                        </Text>
                    )}
                </Text>
                <Menu position="bottom-end">
                    <Menu.Target>
                        <Button
                            color={color}
                            variant="outline"
                            leftSection={<FiPlus size={14} />}
                            size="sm"
                            disabled={!canAdd}
                        >
                            Add
                        </Button>
                    </Menu.Target>
                    <Menu.Dropdown>
                        <Menu.Item
                            leftSection={<FiPlus size={16} />}
                            onClick={() =>
                                form.insertListItem(filtersPath, {
                                    fieldId: "",
                                    operator: "",
                                    value: undefined,
                                })
                            }
                        >
                            Add Manually
                        </Menu.Item>
                        <Menu.Item
                            leftSection={<FiPlusSquare size={14} />}
                            onClick={() => {
                                setSelectedFieldForTemplate("");
                                setShowTemplateModal(true);
                            }}
                        >
                            Add From Template
                        </Menu.Item>
                    </Menu.Dropdown>
                </Menu>
            </Group>

            {filters.length === 0 ? (
                <Paper p="md" withBorder>
                    <Text c="dimmed" ta="center" size="sm">
                        No filtering rules added yet
                    </Text>
                </Paper>
            ) : (
                <Stack gap="sm">
                    {filters.map((filter, index) => {
                        const selectedField = getFieldById(filter.fieldId);
                        return (
                            <Paper key={index} p="md" withBorder>
                                <Stack gap="md">
                                    <Group gap="md">
                                        <Select
                                            flex={1}
                                            allowDeselect={false}
                                            label="Field"
                                            placeholder="Select field"
                                            data={fieldOptions}
                                            {...form.getInputProps(
                                                `${filtersPath}.${index}.fieldId`,
                                            )}
                                            onChange={(newFieldId) => {
                                                form.setFieldValue(
                                                    `${filtersPath}.${index}`,
                                                    {
                                                        fieldId:
                                                            newFieldId || "",
                                                        operator: "",
                                                        value: undefined,
                                                    },
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
                                                `${filtersPath}.${index}.operator`,
                                            )}
                                        />
                                    </Group>
                                    <Group
                                        gap="md"
                                        align="flex-end"
                                        justify="flex-end"
                                    >
                                        {selectedField && (
                                            <DynamicDateValueInput
                                                isDateType={
                                                    selectedField.type ===
                                                        "date" ||
                                                    selectedField.type ===
                                                        "datetime"
                                                }
                                                value={filter.value}
                                                onChange={(v) =>
                                                    form.setFieldValue(
                                                        `${filtersPath}.${index}.value`,
                                                        v,
                                                    )
                                                }
                                                field={selectedField}
                                                form={form}
                                                fieldPath={`${filtersPath}.${index}.value`}
                                                label={selectedField.name}
                                            />
                                        )}
                                        <ActionIcon
                                            color="red"
                                            variant="outline"
                                            onClick={() =>
                                                form.removeListItem(
                                                    filtersPath,
                                                    index,
                                                )
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

            {showTemplateModal && (
                <Modal
                    opened
                    onClose={() => {
                        setShowTemplateModal(false);
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
                                {getAvailableTemplates(
                                    selectedFieldForTemplate,
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
                                        {getAvailableTemplates(
                                            selectedFieldForTemplate,
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
                                                        +{template.filters.length}{" "}
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
        </Stack>
    );
}
