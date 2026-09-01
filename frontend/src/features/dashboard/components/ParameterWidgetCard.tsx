import {
    Button,
    Group,
    Modal,
    Paper,
    ScrollArea,
    Stack,
    Text,
    UnstyledButton,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { TbFilter } from "react-icons/tb";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";
import { FieldDto } from "../../fields/types/FieldDto";
import { ParameterWidgetDto } from "../types/DashboardDto";

const DATE_TYPES = ["date", "datetime"];

/** A synthetic field so the shared value input (which keys off a FieldDto) can render for a
    clause that names only a data type. Mirrors AbstractClauseListEditor. */
const syntheticField = (queryId: string, type: string): FieldDto => ({
    id: queryId,
    name: "Value",
    type,
    required: false,
    isCalculated: false,
});

/** "Amount ≥", "Logged after" — the clause without a value, for the modal's input labels. */
const clauseLabel = (dataType: string, operator?: string | null) => {
    const type = fieldTypes.find((t) => t.value === dataType)?.label ?? dataType;
    return `${type} ${operator ? formatOperator(operator) : ""}`.trim();
};

/** The string form the backend stores a clause value in — mirrors DashboardViewsPanel. */
function normalizeValue(value: unknown): string | null {
    if (value === undefined || value === null || value === "") return null;
    if (value instanceof Date) return value.toISOString();
    return String(value);
}

interface Props {
    widgetId: string;
    /** The clauses + current values, resolved by the board itself. */
    parameter: ParameterWidgetDto | undefined;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the widget's edit dialog: the clauses it drives and which widgets follow it. */
    onEdit?: (itemId: string) => void;
    /** Persists the new values and recomputes every widget linked to this one. */
    onSetValues: (itemId: string, values: Record<string, string | null>) => void;
}

/**
 * A compact board widget that reads as a filter chip: an icon and a one-line summary of the
 * values currently set. Clicking it opens a dialog to edit those values; every
 * Analytic/Entries widget wired to it is then recomputed against the widget's clauses using
 * what was entered. A clause left blank is simply not applied. Values are saved on the
 * widget, so they are what every viewer sees on the next load too.
 */
export function ParameterWidgetCard({
    widgetId,
    parameter,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    onSetValues,
}: Props) {
    const layout = useCardLayout(true);
    const clauses = parameter?.clauses ?? [];
    const [editing, setEditing] = useState(false);

    const form = useForm<{ values: Record<string, unknown> }>({
        initialValues: {
            values: Object.fromEntries(clauses.map((c) => [c.queryId, c.value ?? ""])),
        },
    });

    const openEditor = () => {
        form.setValues({
            values: Object.fromEntries(
                clauses.map((c) => [c.queryId, c.value ?? ""]),
            ),
        });
        setEditing(true);
    };

    const commit = (values: Record<string, string | null>) => {
        onSetValues(widgetId, values);
        setEditing(false);
    };

    const apply = () =>
        commit(
            Object.fromEntries(
                clauses.map((c) => [
                    c.queryId,
                    normalizeValue(form.values.values[c.queryId]),
                ]),
            ),
        );

    const clearAll = () =>
        commit(Object.fromEntries(clauses.map((c) => [c.queryId, null])));

    // What each set clause reads as — "Date & time ≥ Jan 1", "Amount ≥ 10".
    const summaryParts = clauses
        .filter((c) => c.value !== undefined && c.value !== null && c.value !== "")
        .map(
            (c) =>
                `${clauseLabel(c.dataType, c.operator)} ${renderValue(
                    c.dataType,
                    c.value,
                )}`,
        );

    return (
        <Paper
            ref={layout.ref}
            withBorder={isConfiguring}
            p={0}
            radius="md"
            w="100%"
            {...cardShellProps(true)}
        >
            <Stack gap="xs" {...cardBodyProps(true)} h="100%">
                <AnalyticCardHeader
                    title="Parameters"
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={widgetId}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    compact
                />
                <UnstyledButton
                    onClick={openEditor}
                    disabled={isConfiguring || clauses.length === 0}
                    style={{
                        flex: 1,
                        minHeight: 0,
                        display: "flex",
                        alignItems: "center",
                        gap: 8,
                        padding: "0 12px",
                        pointerEvents: isConfiguring ? "none" : "auto",
                    }}
                >
                    <TbFilter size={16} style={{ flexShrink: 0, opacity: 0.7 }} />
                    {clauses.length === 0 ? (
                        <Text size="sm" c="dimmed">
                            This parameter widget is misconfigured.
                        </Text>
                    ) : summaryParts.length === 0 ? (
                        <Text size="sm" c="dimmed">
                            Set filters…
                        </Text>
                    ) : (
                        <Text size="sm" truncate>
                            {summaryParts.join("  ·  ")}
                        </Text>
                    )}
                </UnstyledButton>
            </Stack>

            <Modal
                opened={editing}
                onClose={() => setEditing(false)}
                title="Set parameters"
                centered
                zIndex={400}
            >
                <Stack gap="md">
                    <ScrollArea.Autosize mah="60vh">
                        <Stack gap="sm">
                            {clauses.map((c) => (
                                <Stack key={c.queryId} gap={2}>
                                    <Text size="xs" fw={500} c="dimmed">
                                        {clauseLabel(c.dataType, c.operator)}
                                    </Text>
                                    <DynamicDateValueInput
                                        isDateType={DATE_TYPES.includes(c.dataType)}
                                        value={
                                            form.values.values[c.queryId] as
                                                | string
                                                | number
                                                | Date
                                                | undefined
                                        }
                                        onChange={(v) =>
                                            form.setFieldValue(
                                                `values.${c.queryId}`,
                                                v,
                                            )
                                        }
                                        field={syntheticField(c.queryId, c.dataType)}
                                        form={form}
                                        fieldPath={`values.${c.queryId}`}
                                    />
                                </Stack>
                            ))}
                        </Stack>
                    </ScrollArea.Autosize>
                    <Group justify="space-between">
                        <Button variant="subtle" color="gray" onClick={clearAll}>
                            Clear all
                        </Button>
                        <Group gap="sm">
                            <Button
                                variant="default"
                                onClick={() => setEditing(false)}
                            >
                                Cancel
                            </Button>
                            <Button color={color} onClick={apply}>
                                Apply
                            </Button>
                        </Group>
                    </Group>
                </Stack>
            </Modal>
        </Paper>
    );
}
