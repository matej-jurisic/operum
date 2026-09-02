import { Button, Group, Stack, Text } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useMemo, useState } from "react";
import { fieldTypesCompatible } from "../../../shared/constants/DataTypes";
import { fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { QueryKinds } from "../../../shared/constants/QueryKinds";
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import {
    ClauseDto,
    DashboardItemDto,
    SaveFilterItemDto,
    WidgetLink,
} from "../types/DashboardDto";
import AbstractClauseListEditor, {
    AbstractClauseRow,
} from "./AbstractClauseListEditor";
import { FilterPresetsSection } from "./FilterPresetsSection";
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
    };
    submitLabel: string;
    color?: string;
    onBack: () => void;
    onSubmit: (dto: SaveFilterItemDto) => Promise<void>;
}

/** Adds or edits a filter widget: an ordered set of filter clauses whose values are typed
    on the board, re-filtering every Analytic/Entries widget wired to it. The clauses come
    first; once they are complete the widget can offer matching-shape presets -- named value
    sets picked on the board to fill those clauses in one go. */
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
    const [presetIds, setPresetIds] = useState<string[]>(initial?.presetIds ?? []);

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

    const clausesComplete = rows.length > 0 && filterClauses.length === rows.length;

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

    const eligibleFields = (trackerId: string, dataType: string) =>
        (fieldsByTracker[trackerId] ?? []).filter((f) =>
            fieldTypesCompatible(f.type, dataType),
        );

    const linksComplete = links.every((l) =>
        ownQueries.every((q) => {
            const fieldId = l.fieldByQuery[q.key];
            if (!fieldId) return false;
            const field = eligibleFields(l.trackerId, q.dataType).find(
                (f) => f.id === fieldId,
            );
            return field != null;
        }),
    );

    const canSubmit = clausesComplete && linksComplete;

    const handleSubmit = async () => {
        if (!canSubmit) return;
        setBusy(true);
        // Own clauses always start inactive -- no value is ever collected here, only
        // type + operator. A baked-in starting value belongs in a preset instead.
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
        await onSubmit({ clauses, links: cleanLinks, presetIds });
        setBusy(false);
    };

    return (
        <Stack gap="lg">
            <Stack gap="md">
                <Text fw={500} size="md">
                    What this widget filters
                </Text>
                <Text size="xs" c="dimmed">
                    Each clause becomes an input on the board. Its value is typed in
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
                    title="Followed by"
                    candidates={candidates}
                    fieldsByTracker={fieldsByTracker}
                    queries={ownQueries}
                    links={links}
                    onLinksChange={setLinks}
                />
            )}

            {clausesComplete && (
                <FilterPresetsSection
                    clauses={rows}
                    presetIds={presetIds}
                    onPresetIdsChange={setPresetIds}
                    color={color}
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
