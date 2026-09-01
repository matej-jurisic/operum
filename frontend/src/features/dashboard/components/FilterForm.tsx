import { Button, Group, Stack, Text } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useMemo, useState } from "react";
import { fieldTypesCompatible } from "../../../shared/constants/DataTypes";
import { fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { QueryKinds } from "../../../shared/constants/QueryKinds";
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
import { describeAbstractClause } from "../../../shared/utils/formatters/QueryFormatter";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import {
    ClauseDto,
    DashboardItemDto,
    DashboardViewDto,
    SaveFilterItemDto,
    WidgetLink,
} from "../types/DashboardDto";
import AbstractClauseListEditor, {
    AbstractClauseRow,
} from "./AbstractClauseListEditor";
import { DashboardViewsPanel } from "./DashboardViewsPanel";
import { candidatesFor, FollowedWidgetsSection } from "./FollowedWidgetsSection";

/** One of the widget's own clauses, identified by its position in the list — the key its
    links use, since a clause has no pooled query id until the save resolves one. */
interface FilterClause {
    key: string;
    dataType: string;
    operator: string;
}

interface Props {
    initial?: {
        clauses: AbstractClauseRow[];
        links: WidgetLink[];
        presetIds: string[];
        selectedPresetId: string | null;
        presetLinks: WidgetLink[];
    };
    submitLabel: string;
    color?: string;
    onBack: () => void;
    onSubmit: (dto: SaveFilterItemDto) => Promise<void>;
}

/** Adds or edits a filter widget: an ordered set of filter clauses whose values are typed
    on the board, plus an optional dropdown of the board's saved DashboardViews to quick-apply
    as presets (filters and sorts alike) -- both facets independently re-filter the
    Analytic/Entries widgets wired to them. */
export function FilterForm({
    initial,
    submitLabel,
    color,
    onBack,
    onSubmit,
}: Props) {
    const { dashboardId } = useDashboard();
    const [items, setItems] = useState<DashboardItemDto[]>([]);
    const [fieldsByTracker, setFieldsByTracker] = useState<Record<string, FieldDto[]>>(
        {},
    );
    const [busy, setBusy] = useState(false);

    const form = useForm<{ clauses: AbstractClauseRow[] }>({
        initialValues: { clauses: initial?.clauses ?? [] },
    });

    const [links, setLinks] = useState<WidgetLink[]>(initial?.links ?? []);

    const [views, setViews] = useState<DashboardViewDto[]>([]);
    const [presetIds, setPresetIds] = useState<string[]>(initial?.presetIds ?? []);
    const [selectedPresetId, setSelectedPresetId] = useState<string | null>(
        initial?.selectedPresetId ?? null,
    );
    const [presetLinks, setPresetLinks] = useState<WidgetLink[]>(
        initial?.presetLinks ?? [],
    );

    useEffect(() => {
        dashboardController.getDashboard(dashboardId).then((res) => {
            setItems(res.data?.items ?? []);
        });
    }, [dashboardId]);

    const candidates = useMemo(() => candidatesFor(items), [items]);

    useEffect(() => {
        const trackerIds = [...new Set(candidates.map((c) => c.trackerId))];
        const missing = trackerIds.filter((id) => !(id in fieldsByTracker));
        if (missing.length === 0) return;
        Promise.all(missing.map((id) => fieldsController.getFields(id))).then((res) => {
            setFieldsByTracker((cur) => {
                const next = { ...cur };
                missing.forEach((id, i) => (next[id] = res[i].data ?? []));
                return next;
            });
        });
    }, [candidates, fieldsByTracker]);

    const rows = form.values.clauses;

    // The complete clauses, each keyed by its list position — the key the links map to.
    const filterClauses = useMemo<FilterClause[]>(
        () =>
            rows
                .map((c, i) => ({
                    key: String(i),
                    dataType: c.dataType,
                    operator: c.operator,
                }))
                .filter((c) => c.dataType && c.operator),
        [rows],
    );

    const clausesComplete =
        rows.length > 0 && filterClauses.length === rows.length;

    const describe = (c: FilterClause) => {
        const type =
            fieldTypes.find((t) => t.value === c.dataType)?.label ?? c.dataType;
        return `${type} ${formatOperator(c.operator)}`.trim();
    };

    const ownQueries = useMemo(
        () =>
            filterClauses.map((c) => ({
                key: c.key,
                dataType: c.dataType,
                describe: describe(c),
            })),
        [filterClauses],
    );

    // Distinct queries across the chosen presets, each with the data type its field must be.
    const presetQueries = useMemo(() => {
        const map = new Map<string, DashboardViewDto["clauses"][number]>();
        for (const v of views) {
            if (!presetIds.includes(v.id)) continue;
            for (const c of v.clauses) if (!map.has(c.queryId)) map.set(c.queryId, c);
        }
        return [...map.values()].map((q) => ({
            key: q.queryId,
            dataType: q.dataType,
            describe: describeAbstractClause(q),
        }));
    }, [views, presetIds]);

    const eligibleFields = (trackerId: string, dataType: string) =>
        (fieldsByTracker[trackerId] ?? []).filter((f) => fieldTypesCompatible(f.type, dataType));

    const linksComplete = links.every((l) =>
        ownQueries.every((q) => {
            const fieldId = l.fieldByQuery[q.key];
            if (!fieldId) return false;
            const field = eligibleFields(l.trackerId, q.dataType).find((f) => f.id === fieldId);
            return field != null;
        }),
    );

    const presetLinksComplete = presetLinks.every((l) =>
        presetQueries.every((q) => {
            const fieldId = l.fieldByQuery[q.key];
            if (!fieldId) return false;
            const field = eligibleFields(l.trackerId, q.dataType).find((f) => f.id === fieldId);
            return field != null;
        }),
    );

    const canSubmit =
        (clausesComplete || presetIds.length > 0) && linksComplete && presetLinksComplete;

    const handleSubmit = async () => {
        if (!canSubmit) return;
        setBusy(true);
        // Own clauses always start inactive -- no value is ever collected here, only
        // type + operator. A baked-in starting value belongs in a preset filter instead.
        const clauses: ClauseDto[] = rows.map((c) => ({
            kind: QueryKinds.Filter,
            dataType: c.dataType,
            operator: c.operator,
            value: null,
            descending: false,
        }));
        // Drop mappings for clauses that no longer exist so the payload is clean.
        const cleanLinks = links.map((l) => ({
            ...l,
            fieldByQuery: Object.fromEntries(
                Object.entries(l.fieldByQuery).filter(([key]) =>
                    filterClauses.some((c) => c.key === key),
                ),
            ),
        }));
        await onSubmit({
            clauses,
            links: cleanLinks,
            presetIds,
            selectedPresetId:
                selectedPresetId && presetIds.includes(selectedPresetId)
                    ? selectedPresetId
                    : null,
            presetLinks,
        });
        setBusy(false);
    };

    return (
        <Stack gap="lg">
            <Stack gap="md">
                <Text fw={500} size="md">
                    What this widget filters
                </Text>
                <Text size="xs" c="dimmed">
                    Each clause becomes an input on the board — its value is typed in
                    there, not here.
                </Text>
                <AbstractClauseListEditor
                    form={form}
                    path="clauses"
                    color={color}
                    filterOnly
                />
            </Stack>

            {clausesComplete && (
                <FollowedWidgetsSection
                    title="Followed by typed filters"
                    candidates={candidates}
                    fieldsByTracker={fieldsByTracker}
                    queries={ownQueries}
                    links={links}
                    onLinksChange={setLinks}
                />
            )}

            <DashboardViewsPanel
                onChange={setViews}
                color={color}
                presetIds={presetIds}
                onPresetIdsChange={setPresetIds}
                selectedPresetId={selectedPresetId}
                onSelectedPresetIdChange={setSelectedPresetId}
            />

            {presetIds.length > 0 && (
                <FollowedWidgetsSection
                    title="Followed by presets"
                    candidates={candidates}
                    fieldsByTracker={fieldsByTracker}
                    queries={presetQueries}
                    links={presetLinks}
                    onLinksChange={setPresetLinks}
                />
            )}

            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onBack}>
                    Back
                </Button>
                <Button
                    color={color}
                    disabled={!canSubmit}
                    loading={busy}
                    onClick={handleSubmit}
                >
                    {submitLabel}
                </Button>
            </Group>
        </Stack>
    );
}
