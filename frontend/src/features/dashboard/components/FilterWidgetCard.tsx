import {
    Button,
    Group,
    Modal,
    Paper,
    ScrollArea,
    Select,
    Stack,
    Text,
    UnstyledButton,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { TbFilter } from "react-icons/tb";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";
import { FilterWidgetDto } from "../types/DashboardDto";
import {
    clauseLabel,
    DATE_TYPES,
    normalizeClauseValue,
    syntheticField,
} from "./filterClauseInput";

interface Props {
    widgetId: string;
    /** The clauses + current values, and the matching-shape presets, resolved by the board
        itself. */
    filter: FilterWidgetDto | undefined;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the widget's edit dialog: its clauses, its presets, and which widgets follow
        it. */
    onEdit?: (itemId: string) => void;
    /** Persists the new typed values and recomputes every widget linked to those clauses. */
    onSetValues: (itemId: string, values: Record<string, string | null>) => void;
}

/**
 * A compact board widget that reads as one filter: an icon and a one-line summary of the
 * values currently set. Clicking it opens a dialog to edit those values -- type them by
 * hand, or pick one of the widget's presets to fill them in -- which then re-filters every
 * widget wired to its clauses. A clause left blank is simply not applied. The card looks the
 * same however the values were set.
 */
export function FilterWidgetCard({
    widgetId,
    filter,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    onSetValues,
}: Props) {
    const layout = useCardLayout(true);
    const clauses = filter?.clauses ?? [];
    const presets = filter?.presets ?? [];
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
                    normalizeClauseValue(form.values.values[c.queryId]),
                ]),
            ),
        );

    const clearAll = () =>
        commit(Object.fromEntries(clauses.map((c) => [c.queryId, null])));

    // Picking a preset only fills the inputs -- the user still reviews and hits Apply.
    const applyPreset = (presetId: string) => {
        const preset = presets.find((p) => p.id === presetId);
        if (!preset) return;
        form.setValues({
            values: Object.fromEntries(
                clauses.map((c, i) => [c.queryId, preset.values[i] ?? ""]),
            ),
        });
    };

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
            withBorder
            p={0}
            radius="md"
            w="100%"
            {...cardShellProps(true)}
        >
            <Stack gap="xs" {...cardBodyProps(true)} h="100%">
                <AnalyticCardHeader
                    title="Filters"
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={widgetId}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    compact
                />
                {clauses.length > 0 ? (
                    <UnstyledButton
                        onClick={openEditor}
                        disabled={isConfiguring}
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
                        {summaryParts.length === 0 ? (
                            <Text size="sm" c="dimmed">
                                Set filters…
                            </Text>
                        ) : (
                            <Text size="sm" truncate>
                                {summaryParts.join("  ·  ")}
                            </Text>
                        )}
                    </UnstyledButton>
                ) : (
                    <Text size="sm" c="dimmed" px="xs">
                        This filter widget is misconfigured.
                    </Text>
                )}
            </Stack>

            <Modal
                opened={editing}
                onClose={() => setEditing(false)}
                title="Set filters"
                centered
                zIndex={400}
            >
                <Stack gap="md">
                    {presets.length > 0 && (
                        <Select
                            label="Apply a preset"
                            placeholder="Pick a preset…"
                            data={presets.map((p) => ({ value: p.id, label: p.name }))}
                            value={null}
                            onChange={(value) => value && applyPreset(value)}
                            comboboxProps={{ withinPortal: true, zIndex: 500 }}
                        />
                    )}
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
