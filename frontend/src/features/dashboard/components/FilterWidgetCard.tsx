import {
    ActionIcon,
    Button,
    Group,
    Modal,
    ScrollArea,
    Select,
    Stack,
    Text,
    UnstyledButton,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { FiChevronLeft, FiChevronRight } from "react-icons/fi";
import { TbFilter } from "react-icons/tb";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { renderResolvedDateValue } from "../../../shared/utils/formatters/ValueRenderer";
import { useCardLayout } from "../../analytics/components/cardSizing";
import { WidgetShell } from "../../analytics/components/WidgetShell";
import { FilterWidgetDto } from "../types/DashboardDto";
import {
    clauseLabel,
    DATE_TYPES,
    groupClauseRows,
    normalizeClauseValue,
    shiftDateRangeValue,
    shiftDateValue,
    syntheticField,
} from "./filterClauseInput";

interface Props {
    widgetId: string;
    /** The clauses + current values, and the matching-shape presets, resolved by the board
        itself. */
    filter: FilterWidgetDto | undefined;
    color: string | undefined;
    isConfiguring: boolean;
    flat?: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the widget's edit dialog: its clauses, its presets, and which widgets follow
        it. */
    onEdit?: (itemId: string) => void;
    /** Persists the new typed values and recomputes every widget linked to those clauses. */
    onSetValues: (
        itemId: string,
        values: Record<string, string | null>,
    ) => void;
}

/** A clause left blank is simply not applied. */
export function FilterWidgetCard({
    widgetId,
    filter,
    color,
    isConfiguring,
    flat,
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
            values: Object.fromEntries(
                clauses.map((c) => [c.slotId, c.value ?? ""]),
            ),
        },
    });

    const openEditor = () => {
        form.setValues({
            values: Object.fromEntries(
                clauses.map((c) => [c.slotId, c.value ?? ""]),
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
                    c.slotId,
                    normalizeClauseValue(form.values.values[c.slotId]),
                ]),
            ),
        );

    const clearAll = () =>
        commit(Object.fromEntries(clauses.map((c) => [c.slotId, null])));

    // Picking a preset only fills the inputs -- the user still reviews and hits Apply.
    const applyPreset = (presetId: string) => {
        const preset = presets.find((p) => p.id === presetId);
        if (!preset) return;
        form.setValues({
            values: Object.fromEntries(
                clauses.map((c, i) => [c.slotId, preset.values[i] ?? ""]),
            ),
        });
    };

    const hasValue = (c: { value?: string | null }) =>
        c.value !== undefined && c.value !== null && c.value !== "";

    // What each set clause reads as on the live tile -- "Date & time ≥ Jan 1", "Amount ≥ 10" --
    // plus prev/next handlers for the ones with a shiftable date value, so the tile itself can
    // step through without opening the editor.
    const liveRows = groupClauseRows(clauses).flatMap((row) => {
        if (row.type === "single") {
            const c = row.clause;
            if (!hasValue(c)) return [];
            const canShift =
                DATE_TYPES.includes(c.dataType) &&
                shiftDateValue(c.value, 1) !== null;
            return [
                {
                    key: c.slotId,
                    text: `${clauseLabel(c.dataType, c.operator)} ${renderResolvedDateValue(c.dataType, c.value)}`,
                    shift: canShift
                        ? (direction: 1 | -1) => {
                              const shifted = shiftDateValue(
                                  c.value,
                                  direction,
                              );
                              if (shifted === null) return;
                              onSetValues(widgetId, {
                                  [c.slotId]: normalizeClauseValue(shifted),
                              });
                          }
                        : undefined,
                },
            ];
        }

        const { start, end } = row;
        if (!hasValue(start) || !hasValue(end)) {
            return [start, end].filter(hasValue).map((c) => ({
                key: c.slotId,
                text: `${clauseLabel(c.dataType, c.operator)} ${renderResolvedDateValue(c.dataType, c.value)}`,
                shift: undefined,
            }));
        }

        const canShift =
            shiftDateRangeValue(start.value, end.value, 1) !== null;
        return [
            {
                key: `${start.slotId}-${end.slotId}`,
                text: `${clauseLabel(start.dataType)} ${renderResolvedDateValue(
                    start.dataType,
                    start.value,
                )} – ${renderResolvedDateValue(end.dataType, end.value)}`,
                shift: canShift
                    ? (direction: 1 | -1) => {
                          const shifted = shiftDateRangeValue(
                              start.value,
                              end.value,
                              direction,
                          );
                          if (!shifted) return;
                          onSetValues(widgetId, {
                              [start.slotId]: normalizeClauseValue(
                                  shifted.start,
                              ),
                              [end.slotId]: normalizeClauseValue(shifted.end),
                          });
                      }
                    : undefined,
            },
        ];
    });

    return (
        <WidgetShell
            layout={layout}
            fillHeight
            flat={flat}
            isConfiguring={isConfiguring}
            color={color}
            itemId={widgetId}
            onRemove={onRemove}
            onEdit={onEdit}
            title="Filters"
            compactHeader
            padding={0}
            bodyProps={{ h: "100%" }}
            after={
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
                                placeholder="Select a preset"
                                data={presets.map((p) => ({
                                    value: p.id,
                                    label: p.name,
                                }))}
                                value={null}
                                onChange={(value) =>
                                    value && applyPreset(value)
                                }
                            />
                        )}
                        <ScrollArea.Autosize mah="60vh">
                            <Stack gap="sm">
                                {groupClauseRows(clauses).map((row) => {
                                    if (row.type === "range") {
                                        const { start, end } = row;
                                        const startValue =
                                            form.values.values[start.slotId];
                                        const endValue =
                                            form.values.values[end.slotId];
                                        const shift = (
                                            direction: 1 | -1,
                                        ) => {
                                            const shifted = shiftDateRangeValue(
                                                startValue,
                                                endValue,
                                                direction,
                                            );
                                            if (!shifted) return;
                                            form.setFieldValue(
                                                `values.${start.slotId}`,
                                                shifted.start,
                                            );
                                            form.setFieldValue(
                                                `values.${end.slotId}`,
                                                shifted.end,
                                            );
                                        };
                                        const canShift =
                                            shiftDateRangeValue(
                                                startValue,
                                                endValue,
                                                1,
                                            ) !== null;

                                        return (
                                            <Stack
                                                key={`${start.slotId}-${end.slotId}`}
                                                gap={2}
                                            >
                                                <Group
                                                    justify="space-between"
                                                    align="center"
                                                >
                                                    <Text
                                                        size="xs"
                                                        fw={500}
                                                        c="dimmed"
                                                    >
                                                        {clauseLabel(
                                                            start.dataType,
                                                        )}{" "}
                                                        range
                                                    </Text>
                                                    {canShift && (
                                                        <Group gap={4}>
                                                            <ActionIcon
                                                                variant="subtle"
                                                                color="gray"
                                                                size="sm"
                                                                onClick={() =>
                                                                    shift(-1)
                                                                }
                                                                aria-label="Previous range"
                                                            >
                                                                <FiChevronLeft
                                                                    size={14}
                                                                />
                                                            </ActionIcon>
                                                            <ActionIcon
                                                                variant="subtle"
                                                                color="gray"
                                                                size="sm"
                                                                onClick={() =>
                                                                    shift(1)
                                                                }
                                                                aria-label="Next range"
                                                            >
                                                                <FiChevronRight
                                                                    size={14}
                                                                />
                                                            </ActionIcon>
                                                        </Group>
                                                    )}
                                                </Group>
                                                <Stack gap={2}>
                                                    <Text
                                                        size="xs"
                                                        fw={500}
                                                        c="dimmed"
                                                    >
                                                        {clauseLabel(
                                                            start.dataType,
                                                            start.operator,
                                                        )}
                                                    </Text>
                                                    <DynamicDateValueInput
                                                        isDateType
                                                        value={
                                                            startValue as
                                                                | string
                                                                | number
                                                                | Date
                                                                | undefined
                                                        }
                                                        onChange={(v) =>
                                                            form.setFieldValue(
                                                                `values.${start.slotId}`,
                                                                v,
                                                            )
                                                        }
                                                        field={syntheticField(
                                                            start.slotId,
                                                            start.dataType,
                                                        )}
                                                        form={form}
                                                        fieldPath={`values.${start.slotId}`}
                                                    />
                                                </Stack>
                                                <Stack gap={2}>
                                                    <Text
                                                        size="xs"
                                                        fw={500}
                                                        c="dimmed"
                                                    >
                                                        {clauseLabel(
                                                            end.dataType,
                                                            end.operator,
                                                        )}
                                                    </Text>
                                                    <DynamicDateValueInput
                                                        isDateType
                                                        value={
                                                            endValue as
                                                                | string
                                                                | number
                                                                | Date
                                                                | undefined
                                                        }
                                                        onChange={(v) =>
                                                            form.setFieldValue(
                                                                `values.${end.slotId}`,
                                                                v,
                                                            )
                                                        }
                                                        field={syntheticField(
                                                            end.slotId,
                                                            end.dataType,
                                                        )}
                                                        form={form}
                                                        fieldPath={`values.${end.slotId}`}
                                                    />
                                                </Stack>
                                            </Stack>
                                        );
                                    }

                                    const c = row.clause;
                                    const value =
                                        form.values.values[c.slotId];
                                    const canShiftDay =
                                        DATE_TYPES.includes(c.dataType) &&
                                        shiftDateValue(value, 1) !== null;

                                    return (
                                        <Stack key={c.slotId} gap={2}>
                                            <Group
                                                justify="space-between"
                                                align="center"
                                            >
                                                <Text
                                                    size="xs"
                                                    fw={500}
                                                    c="dimmed"
                                                >
                                                    {clauseLabel(
                                                        c.dataType,
                                                        c.operator,
                                                    )}
                                                </Text>
                                                {canShiftDay && (
                                                    <Group gap={4}>
                                                        <ActionIcon
                                                            variant="subtle"
                                                            color="gray"
                                                            size="sm"
                                                            onClick={() =>
                                                                form.setFieldValue(
                                                                    `values.${c.slotId}`,
                                                                    shiftDateValue(
                                                                        value,
                                                                        -1,
                                                                    ),
                                                                )
                                                            }
                                                            aria-label="Previous"
                                                        >
                                                            <FiChevronLeft
                                                                size={14}
                                                            />
                                                        </ActionIcon>
                                                        <ActionIcon
                                                            variant="subtle"
                                                            color="gray"
                                                            size="sm"
                                                            onClick={() =>
                                                                form.setFieldValue(
                                                                    `values.${c.slotId}`,
                                                                    shiftDateValue(
                                                                        value,
                                                                        1,
                                                                    ),
                                                                )
                                                            }
                                                            aria-label="Next"
                                                        >
                                                            <FiChevronRight
                                                                size={14}
                                                            />
                                                        </ActionIcon>
                                                    </Group>
                                                )}
                                            </Group>
                                            <DynamicDateValueInput
                                                isDateType={DATE_TYPES.includes(
                                                    c.dataType,
                                                )}
                                                value={
                                                    value as
                                                        | string
                                                        | number
                                                        | Date
                                                        | undefined
                                                }
                                                onChange={(v) =>
                                                    form.setFieldValue(
                                                        `values.${c.slotId}`,
                                                        v,
                                                    )
                                                }
                                                field={syntheticField(
                                                    c.slotId,
                                                    c.dataType,
                                                )}
                                                form={form}
                                                fieldPath={`values.${c.slotId}`}
                                            />
                                        </Stack>
                                    );
                                })}
                            </Stack>
                        </ScrollArea.Autosize>
                        <Group justify="space-between">
                            <Button
                                variant="subtle"
                                color="gray"
                                onClick={clearAll}
                            >
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
            }
        >
            {clauses.length > 0 ? (
                <Group
                    wrap="nowrap"
                    gap={8}
                    style={{
                        flex: 1,
                        minHeight: 0,
                        padding: "0 12px",
                        overflow: "hidden",
                        pointerEvents: isConfiguring ? "none" : "auto",
                    }}
                >
                    <TbFilter
                        size={16}
                        style={{ flexShrink: 0, opacity: 0.7 }}
                    />
                    {liveRows.length === 0 ? (
                        <UnstyledButton
                            onClick={openEditor}
                            disabled={isConfiguring}
                            style={{ flex: 1, textAlign: "left" }}
                        >
                            <Text size="sm" c="dimmed">
                                Set filters…
                            </Text>
                        </UnstyledButton>
                    ) : (
                        <Group
                            wrap="nowrap"
                            gap="md"
                            style={{
                                flex: 1,
                                minWidth: 0,
                                overflowX: "auto",
                                overflowY: "hidden",
                            }}
                        >
                            {liveRows.map((row) => (
                                <Group
                                    key={row.key}
                                    gap={4}
                                    wrap="nowrap"
                                    justify="space-between"
                                    style={{ flex: 1, minWidth: 0 }}
                                >
                                    {row.shift && (
                                        <ActionIcon
                                            variant="subtle"
                                            color="gray"
                                            size="sm"
                                            disabled={isConfiguring}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                row.shift?.(-1);
                                            }}
                                            aria-label="Previous"
                                            style={{ flexShrink: 0 }}
                                        >
                                            <FiChevronLeft size={14} />
                                        </ActionIcon>
                                    )}
                                    <UnstyledButton
                                        onClick={openEditor}
                                        disabled={isConfiguring}
                                        style={{ flex: 1, minWidth: 0 }}
                                    >
                                        <Text size="sm" truncate ta="center">
                                            {row.text}
                                        </Text>
                                    </UnstyledButton>
                                    {row.shift && (
                                        <ActionIcon
                                            variant="subtle"
                                            color="gray"
                                            size="sm"
                                            disabled={isConfiguring}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                row.shift?.(1);
                                            }}
                                            aria-label="Next"
                                            style={{ flexShrink: 0 }}
                                        >
                                            <FiChevronRight size={14} />
                                        </ActionIcon>
                                    )}
                                </Group>
                            ))}
                        </Group>
                    )}
                </Group>
            ) : (
                <Text size="sm" c="dimmed" px="xs">
                    This filter widget is misconfigured.
                </Text>
            )}
        </WidgetShell>
    );
}
