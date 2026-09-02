import {
    ActionIcon,
    Badge,
    Button,
    Checkbox,
    Group,
    Paper,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useCallback, useEffect, useMemo, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { QueryKinds } from "../../../shared/constants/QueryKinds";
import { describeAbstractClause } from "../../../shared/utils/formatters/QueryFormatter";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import { ClauseDto, DashboardViewDto } from "../types/DashboardDto";
import { AbstractClauseRow } from "./AbstractClauseListEditor";
import {
    clauseLabel,
    DATE_TYPES,
    normalizeClauseValue,
    syntheticField,
} from "./filterClauseInput";

interface Props {
    /** The widget's own complete filter clauses -- a preset may only be offered when its
        clause shape (data type + operator, in order) matches these exactly. */
    clauses: AbstractClauseRow[];
    presetIds: string[];
    onPresetIdsChange: (ids: string[]) => void;
    color?: string;
}

/** Does this board view fit the widget's clause shape? Returns the view's value per clause
    (in the widget's clause order) when it does, or null when the shapes differ. */
function matchValues(
    view: DashboardViewDto,
    clauses: AbstractClauseRow[],
): (string | null)[] | null {
    const filters = view.clauses.filter((c) => c.kind === QueryKinds.Filter);
    if (filters.length !== view.clauses.length) return null;
    if (filters.length !== clauses.length) return null;
    for (let i = 0; i < filters.length; i++) {
        if (
            filters[i].dataType !== clauses[i].dataType ||
            (filters[i].operator ?? "") !== clauses[i].operator
        )
            return null;
    }
    return filters.map((f) => f.value ?? null);
}

/**
 * The preset facet of the filter widget editor. A preset is a board DashboardView that is
 * nothing but a named set of values for this exact clause set; only views whose clause
 * shape matches the widget's are offered, and a "New preset" form creates one from the
 * current clauses. Shown only once the widget's clauses are complete.
 */
export function FilterPresetsSection({
    clauses,
    presetIds,
    onPresetIdsChange,
    color,
}: Props) {
    const { dashboardId } = useDashboard();
    const [views, setViews] = useState<DashboardViewDto[]>([]);
    const [adding, setAdding] = useState(false);
    const [busy, setBusy] = useState(false);

    const load = useCallback(async () => {
        const res = await dashboardController.getDashboardViews(dashboardId);
        setViews(res.data ?? []);
    }, [dashboardId]);

    useEffect(() => {
        load();
    }, [load]);

    // Every view paired with the values it would supply, or null when it no longer fits.
    const matched = useMemo(
        () =>
            views
                .map((v) => ({ view: v, values: matchValues(v, clauses) }))
                .filter((m): m is { view: DashboardViewDto; values: (string | null)[] } =>
                    m.values !== null,
                ),
        [views, clauses],
    );

    // Drop any offered preset whose shape has drifted away from the current clauses, so the
    // save never carries one the backend would reject.
    useEffect(() => {
        if (views.length === 0) return;
        const offerable = new Set(matched.map((m) => m.view.id));
        const pruned = presetIds.filter((id) => offerable.has(id));
        if (pruned.length !== presetIds.length) onPresetIdsChange(pruned);
    }, [matched, views.length, presetIds, onPresetIdsChange]);

    const form = useForm<{ name: string; values: Record<number, unknown> }>({
        initialValues: { name: "", values: {} },
    });

    const toggle = (id: string, on: boolean) =>
        onPresetIdsChange(on ? [...presetIds, id] : presetIds.filter((p) => p !== id));

    const canSave = form.values.name.trim().length > 0;

    const handleAdd = async () => {
        if (!canSave) return;
        setBusy(true);
        const clauseDtos: ClauseDto[] = clauses.map((c, i) => ({
            kind: QueryKinds.Filter,
            dataType: c.dataType,
            operator: c.operator,
            value: normalizeClauseValue(form.values.values[i]),
            descending: false,
        }));
        const res = await dashboardController.addDashboardView(dashboardId, {
            name: form.values.name.trim(),
            clauses: clauseDtos,
        });
        setBusy(false);
        const newId = res.data?.id;
        form.reset();
        setAdding(false);
        await load();
        if (newId) onPresetIdsChange([...presetIds, newId]);
    };

    const handleDelete = async (id: string) => {
        setBusy(true);
        await dashboardController.deleteDashboardView(dashboardId, id);
        setBusy(false);
        onPresetIdsChange(presetIds.filter((p) => p !== id));
        await load();
    };

    return (
        <Stack gap="md">
            <Group justify="space-between" wrap="nowrap">
                <Text fw={500} size="md">
                    Presets
                    {matched.length > 0 && (
                        <Text span c="dimmed" size="sm" ml="xs">
                            ({matched.length})
                        </Text>
                    )}
                </Text>
                {!adding && (
                    <Button
                        size="sm"
                        variant="outline"
                        color={color}
                        leftSection={<FiPlus size={14} />}
                        onClick={() => setAdding(true)}
                    >
                        New preset
                    </Button>
                )}
            </Group>

            <Text size="xs" c="dimmed">
                A preset fills these clauses with a saved set of values. Only presets that
                match this widget's clauses are shown.
            </Text>

            {matched.length === 0 && !adding && (
                <Paper p="md" withBorder radius="md">
                    <Text c="dimmed" ta="center" size="sm">
                        No matching presets yet. Create one from the clauses above.
                    </Text>
                </Paper>
            )}

            {matched.map(({ view, values }) => (
                <Paper key={view.id} p="md" withBorder radius="md">
                    <Group justify="space-between" wrap="nowrap" align="flex-start">
                        <Stack gap={6} miw={0}>
                            <Checkbox
                                label={view.name}
                                checked={presetIds.includes(view.id)}
                                onChange={(e) =>
                                    toggle(view.id, e.currentTarget.checked)
                                }
                            />
                            <Group gap={4} wrap="wrap" pl="lg">
                                {clauses.map((c, i) => (
                                    <Badge
                                        key={i}
                                        variant="light"
                                        radius="sm"
                                        styles={{ label: { textTransform: "none" } }}
                                    >
                                        {describeAbstractClause({
                                            kind: QueryKinds.Filter,
                                            dataType: c.dataType,
                                            operator: c.operator,
                                            value: values[i] ?? undefined,
                                            descending: false,
                                        })}
                                    </Badge>
                                ))}
                            </Group>
                        </Stack>
                        <ActionIcon
                            color="red"
                            variant="outline"
                            disabled={busy}
                            onClick={() => handleDelete(view.id)}
                            aria-label="Delete preset"
                        >
                            <MdDelete size={16} />
                        </ActionIcon>
                    </Group>
                </Paper>
            ))}

            {adding && (
                <Paper p="md" withBorder radius="md">
                    <Stack gap="md">
                        <TextInput
                            label="Name"
                            placeholder="e.g. Current Month"
                            maxLength={50}
                            {...form.getInputProps("name")}
                        />
                        {clauses.map((c, i) => (
                            <Stack key={i} gap={2}>
                                <Text size="xs" fw={500} c="dimmed">
                                    {clauseLabel(c.dataType, c.operator)}
                                </Text>
                                <DynamicDateValueInput
                                    isDateType={DATE_TYPES.includes(c.dataType)}
                                    value={
                                        form.values.values[i] as
                                            | string
                                            | number
                                            | Date
                                            | undefined
                                    }
                                    onChange={(v) =>
                                        form.setFieldValue(`values.${i}`, v)
                                    }
                                    field={syntheticField(`values.${i}`, c.dataType)}
                                    form={form}
                                    fieldPath={`values.${i}`}
                                />
                            </Stack>
                        ))}
                        <Group justify="flex-end">
                            <Button
                                variant="default"
                                onClick={() => {
                                    form.reset();
                                    setAdding(false);
                                }}
                            >
                                Cancel
                            </Button>
                            <Button
                                color={color}
                                loading={busy}
                                disabled={!canSave}
                                onClick={handleAdd}
                            >
                                Add preset
                            </Button>
                        </Group>
                    </Stack>
                </Paper>
            )}
        </Stack>
    );
}
