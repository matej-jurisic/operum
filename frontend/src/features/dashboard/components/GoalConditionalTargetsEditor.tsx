import { ActionIcon, Button, Group, Paper, Stack, Text, TextInput } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect } from "react";
import { MdAdd, MdDelete } from "react-icons/md";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { GoalConditionalTargetDto } from "../types/DashboardDto";
import {
    DATE_TYPES,
    clauseLabel,
    normalizeClauseValue,
    syntheticField,
} from "./filterClauseInput";
import { ConnectedClause } from "./filterLinkUtils";

interface Props {
    clauses: ConnectedClause[];
    value: GoalConditionalTargetDto[];
    onChange: (rows: GoalConditionalTargetDto[]) => void;
}

interface FormRow {
    values: Record<string, unknown>;
    target: string;
}

const toFormRow = (row: GoalConditionalTargetDto): FormRow => ({
    values: { ...row.conditions },
    target: row.target,
});

const emptyRow = (): FormRow => ({ values: {}, target: "" });

/** Each row = a target that applies when the followed filters currently hold these values.
    Rows are tried top to bottom; the first whose every filled-in value matches the board
    wins. A value left blank on a row is a wildcard. */
export function GoalConditionalTargetsEditor({ clauses, value, onChange }: Props) {
    const form = useForm<{ rows: FormRow[] }>({
        initialValues: {
            rows: value.length > 0 ? value.map(toFormRow) : [],
        },
    });

    const rows = form.values.rows;

    // A row with no filled-in conditions, or no target, is dropped.
    useEffect(() => {
        const out: GoalConditionalTargetDto[] = rows
            .map((row) => {
                const conditions: Record<string, string> = {};
                for (const clause of clauses) {
                    const normalized = normalizeClauseValue(row.values[clause.slotId]);
                    if (normalized !== null) conditions[clause.slotId] = normalized;
                }
                return { conditions, target: row.target.trim() };
            })
            .filter((row) => Object.keys(row.conditions).length > 0 && row.target !== "");
        onChange(out);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [JSON.stringify(rows), clauses]);

    if (clauses.length === 0) return null;

    return (
        <Stack gap="sm">
            <Text fw={500} size="sm">
                Conditional targets
            </Text>

            {rows.map((row, index) => (
                <Paper key={index} withBorder p="sm" radius="md">
                    <Stack gap="sm">
                        <Group justify="space-between" align="center">
                            <Text size="xs" fw={600} c="dimmed">
                                When
                            </Text>
                            <ActionIcon
                                size="sm"
                                variant="outline"
                                color="red"
                                aria-label="Remove conditional target"
                                onClick={() =>
                                    form.setFieldValue(
                                        "rows",
                                        rows.filter((_, i) => i !== index),
                                    )
                                }
                            >
                                <MdDelete size={14} />
                            </ActionIcon>
                        </Group>

                        {clauses.map((clause) => (
                            <Stack key={clause.slotId} gap={2}>
                                <Text size="xs" fw={500} c="dimmed">
                                    {clauseLabel(
                                        clause.dataType,
                                        clause.operator,
                                        clause.fieldName,
                                    )}
                                </Text>
                                <DynamicDateValueInput
                                    isDateType={DATE_TYPES.includes(clause.dataType)}
                                    value={
                                        row.values[clause.slotId] as
                                            | string
                                            | number
                                            | Date
                                            | undefined
                                    }
                                    onChange={(v) =>
                                        form.setFieldValue(
                                            `rows.${index}.values.${clause.slotId}`,
                                            v,
                                        )
                                    }
                                    field={syntheticField(clause.slotId, clause.dataType)}
                                    form={form}
                                    fieldPath={`rows.${index}.values.${clause.slotId}`}
                                />
                            </Stack>
                        ))}

                        <TextInput
                            label="Target"
                            placeholder="Number or hh:mm:ss"
                            value={row.target}
                            onChange={(event) =>
                                form.setFieldValue(
                                    `rows.${index}.target`,
                                    event.currentTarget.value,
                                )
                            }
                        />
                    </Stack>
                </Paper>
            ))}

            <Button
                variant="light"
                size="xs"
                leftSection={<MdAdd size={14} />}
                onClick={() => form.setFieldValue("rows", [...rows, emptyRow()])}
            >
                Add conditional target
            </Button>
        </Stack>
    );
}
