import { Card, Group, Modal, Paper, Select, Stack, Text } from "@mantine/core";
import { useState } from "react";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import { QueryKinds } from "../../../shared/constants/QueryKinds";
import { describeClause } from "../../../shared/utils/formatters/QueryFormatter";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import { useFields } from "../../fields/context/FieldsContext";
import {
    FilterTemplate,
    filterTemplates,
} from "../../views/components/ViewFilterTemplates";
import { CreateQueryDto } from "../types/requests/CreateQueryDto";

interface Props {
    /** How many more queries the caller can take. A template needing more is not offered. */
    remainingSlots?: number;
    onSubmitClauses: (clauses: CreateQueryDto[]) => void;
    onClose: () => void;
}

/**
 * Ready-made filters over one field. A period template ("Current Month") is two bounds
 * ANDed together, so it produces two queries rather than one.
 */
export default function QueryTemplateDialog({
    remainingSlots = Number.MAX_SAFE_INTEGER,
    onSubmitClauses,
    onClose,
}: Props) {
    const { fields } = useFields();
    const [fieldId, setFieldId] = useState("");

    const field = fields.find((f) => f.id === fieldId);

    const available = field
        ? filterTemplates.filter(
              (t) =>
                  field.type !== undefined &&
                  t.fieldTypes.includes(field.type) &&
                  t.filters.length <= remainingSlots,
          )
        : [];

    const toClauses = (template: FilterTemplate): CreateQueryDto[] =>
        template.filters.map((f) => ({
            kind: QueryKinds.Filter,
            fieldId,
            operator: f.operator,
            value:
                f.value === undefined
                    ? undefined
                    : isDynamicDateToken(f.value)
                      ? (f.value as string)
                      : GetStringValue(field?.type, f.value),
        }));

    const apply = (template: FilterTemplate) => {
        onSubmitClauses(toClauses(template));
        onClose();
    };

    return (
        <Modal opened centered onClose={onClose} title="Query Templates" size="md">
            <Stack gap="md">
                <Select
                    label="Field"
                    placeholder="Choose a field to filter on"
                    allowDeselect={false}
                    searchable
                    data={fields.map((f) => ({ value: f.id, label: f.name }))}
                    value={fieldId}
                    onChange={(value) => setFieldId(value || "")}
                />

                {field &&
                    (available.length === 0 ? (
                        <Paper p="md" withBorder>
                            <Text c="dimmed" ta="center" size="sm">
                                No templates fit this field type, or the view has
                                no room left for them.
                            </Text>
                        </Paper>
                    ) : (
                        <Stack gap="sm">
                            {available.map((template) => (
                                <Card
                                    key={template.id}
                                    withBorder
                                    p="md"
                                    style={{ cursor: "pointer" }}
                                    onClick={() => apply(template)}
                                >
                                    <Group
                                        justify="space-between"
                                        align="flex-start"
                                        wrap="nowrap"
                                    >
                                        <Group gap="sm" wrap="nowrap">
                                            {template.icon}
                                            <div>
                                                <Text fw={500} size="sm">
                                                    {template.name}
                                                </Text>
                                                <Text c="dimmed" size="xs">
                                                    {toClauses(template)
                                                        .map((c) =>
                                                            describeClause({
                                                                ...c,
                                                                field,
                                                            }),
                                                        )
                                                        .join(" and ")}
                                                </Text>
                                            </div>
                                        </Group>
                                        <Text c="dimmed" size="xs">
                                            {template.filters.length}{" "}
                                            {template.filters.length === 1
                                                ? "query"
                                                : "queries"}
                                        </Text>
                                    </Group>
                                </Card>
                            ))}
                        </Stack>
                    ))}
            </Stack>
        </Modal>
    );
}
