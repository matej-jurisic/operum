import {
    ActionIcon,
    Badge,
    Button,
    Group,
    MultiSelect,
    Paper,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import { QueryKinds } from "../../../shared/constants/QueryKinds";
import { describeAbstractClause } from "../../../shared/utils/formatters/QueryFormatter";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import { ClauseDto, DashboardViewDto } from "../types/DashboardDto";
import AbstractClauseListEditor, {
    AbstractClauseRow,
} from "./AbstractClauseListEditor";

/** The string form the backend stores a clause value in. */
function normalizeClauseValue(value: unknown): string | null {
    if (value === undefined || value === null || value === "") return null;
    if (value instanceof Date) return value.toISOString();
    return String(value);
}

interface Props {
    /** Called whenever the set of views changes, so a parent can refresh its options. */
    onChange: (views: DashboardViewDto[]) => void;
    /** Board colour, so the panel's controls match the rest of the dashboard chrome. */
    color?: string;
    /** Which of the board's preset filters this widget offers, and which one it starts
        on — this widget's own selection, not the board-wide set managed above it. */
    presetIds: string[];
    onPresetIdsChange: (ids: string[]) => void;
    selectedPresetId: string | null;
    onSelectedPresetIdChange: (id: string | null) => void;
}

/** Manage the board's DashboardViews — the named clause sets a filter widget can offer
    as presets — and, right alongside them, which of those this particular widget offers
    and starts on. Both facets are only ever meaningful together, so they share one section. */
export function DashboardViewsPanel({
    onChange,
    color,
    presetIds,
    onPresetIdsChange,
    selectedPresetId,
    onSelectedPresetIdChange,
}: Props) {
    const { dashboardId } = useDashboard();
    const [views, setViews] = useState<DashboardViewDto[]>([]);
    const [adding, setAdding] = useState(false);
    const [busy, setBusy] = useState(false);

    const form = useForm<{ name: string; clauses: AbstractClauseRow[] }>({
        initialValues: { name: "", clauses: [] },
    });

    const load = async () => {
        const res = await dashboardController.getDashboardViews(dashboardId);
        const list = res.data ?? [];
        setViews(list);
        onChange(list);
    };

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [dashboardId]);

    const canSave =
        form.values.name.trim().length > 0 &&
        form.values.clauses.length > 0 &&
        form.values.clauses.every(
            (c) =>
                c.dataType &&
                (c.kind === QueryKinds.Sort || c.operator),
        );

    const handleAdd = async () => {
        if (!canSave) return;
        setBusy(true);
        const clauses: ClauseDto[] = form.values.clauses.map((c) => ({
            kind: c.kind,
            dataType: c.dataType,
            operator: c.kind === QueryKinds.Filter ? c.operator : null,
            value:
                c.kind === QueryKinds.Filter
                    ? normalizeClauseValue(c.value)
                    : null,
            descending: c.kind === QueryKinds.Sort && c.descending,
        }));
        await dashboardController.addDashboardView(dashboardId, {
            name: form.values.name.trim(),
            clauses,
        });
        form.reset();
        setAdding(false);
        setBusy(false);
        await load();
    };

    const handleDelete = async (id: string) => {
        setBusy(true);
        await dashboardController.deleteDashboardView(dashboardId, id);
        setBusy(false);
        await load();
    };

    const presetData = views.map((v) => ({ value: v.id, label: v.name }));

    return (
        <Stack gap="md">
            <Group justify="space-between" wrap="nowrap">
                <Text fw={500} size="md">
                    Preset filters
                    {views.length > 0 && (
                        <Text span c="dimmed" size="sm" ml="xs">
                            ({views.length})
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
                        New
                    </Button>
                )}
            </Group>

            {views.length === 0 && !adding && (
                <Paper p="md" withBorder radius="md">
                    <Text c="dimmed" ta="center" size="sm">
                        No preset filters yet. Add one (e.g. "Current Month") for the
                        widget to offer.
                    </Text>
                </Paper>
            )}

            {views.map((v) => (
                <Paper key={v.id} p="md" withBorder radius="md">
                    <Group justify="space-between" wrap="nowrap">
                        <Stack gap={4} miw={0}>
                            <Text size="sm" fw={500}>
                                {v.name}
                            </Text>
                            <Group gap={4} wrap="wrap">
                                {v.clauses.map((c, i) => (
                                    <Badge
                                        key={i}
                                        variant="light"
                                        radius="sm"
                                        styles={{
                                            label: { textTransform: "none" },
                                        }}
                                    >
                                        {describeAbstractClause(c)}
                                    </Badge>
                                ))}
                            </Group>
                        </Stack>
                        <ActionIcon
                            color="red"
                            variant="outline"
                            disabled={busy}
                            onClick={() => handleDelete(v.id)}
                            aria-label="Delete preset filter"
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
                        <AbstractClauseListEditor
                            form={form}
                            path="clauses"
                            color={color}
                        />
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
                                Add
                            </Button>
                        </Group>
                    </Stack>
                </Paper>
            )}

            {views.length > 0 && (
                <Stack gap="sm">
                    <MultiSelect
                        label="Offered on this widget"
                        placeholder="Pick preset filters"
                        data={presetData}
                        value={presetIds}
                        onChange={onPresetIdsChange}
                    />
                    <Select
                        label="Starting preset"
                        placeholder="None"
                        data={presetData.filter((o) => presetIds.includes(o.value))}
                        value={selectedPresetId}
                        onChange={onSelectedPresetIdChange}
                        clearable
                    />
                </Stack>
            )}
        </Stack>
    );
}
