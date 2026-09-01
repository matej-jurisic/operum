import { Paper, ScrollArea, Stack, Text } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useDebouncedValue } from "@mantine/hooks";
import { useEffect } from "react";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
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

/** What the clause filters, without a value — "Amount ≥", "Logged after". The value is the
    input below it, and a parameter clause usually has none stored, so describeAbstractClause
    (which would read "… empty") does not fit here. */
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
    /** Opens the widget's edit dialog: the filter set it drives and which widgets follow it. */
    onEdit?: (itemId: string) => void;
    /** Persists the new values and recomputes every widget linked to this one. */
    onSetValues: (itemId: string, values: Record<string, string | null>) => void;
}

/**
 * A board widget that is a live filter with the value typed on the dashboard: it points at
 * one DashboardView and shows an input per clause of it. Every Analytic/Entries widget
 * wired to it is recalculated against those clauses using the values entered here; a clause
 * left blank is simply not applied. Values are saved on the widget, so they are what every
 * viewer sees on the next load too.
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

    const form = useForm<{ values: Record<string, unknown> }>({
        initialValues: {
            values: Object.fromEntries(clauses.map((c) => [c.queryId, c.value ?? ""])),
        },
    });

    // A stable string of what the server currently has — the re-seed effect keys on it, so
    // it only runs when a value actually changed (the board hands back a fresh clause array
    // on every recompute, value or not).
    const serverJson = JSON.stringify(
        Object.fromEntries(clauses.map((c) => [c.queryId, c.value ?? null])),
    );

    useEffect(() => {
        const server: Record<string, string | null> = JSON.parse(serverJson);
        form.setValues({
            values: Object.fromEntries(
                Object.entries(server).map(([k, v]) => [k, v ?? ""]),
            ),
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [serverJson]);

    // What the inputs currently hold, normalised — a primitive so the debounce timer starts
    // only on a real edit, not on every re-render.
    const editedJson = JSON.stringify(
        Object.fromEntries(
            clauses.map((c) => [c.queryId, normalizeValue(form.values.values[c.queryId])]),
        ),
    );
    const [debouncedJson] = useDebouncedValue(editedJson, 500);

    useEffect(() => {
        if (debouncedJson === serverJson) return;
        onSetValues(widgetId, JSON.parse(debouncedJson));
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [debouncedJson]);

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
                <ScrollArea
                    style={{
                        flex: 1,
                        minHeight: 0,
                        pointerEvents: isConfiguring ? "none" : "auto",
                    }}
                >
                    {clauses.length === 0 ? (
                        <Text size="sm" c="dimmed" ta="center" p="md">
                            This parameter widget is misconfigured.
                        </Text>
                    ) : (
                        <Stack gap="sm" p="sm" pt={0}>
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
                    )}
                </ScrollArea>
            </Stack>
        </Paper>
    );
}
