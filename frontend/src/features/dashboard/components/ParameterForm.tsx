import { Button, Checkbox, Group, Select, Stack, Text } from "@mantine/core";
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
    SaveParameterItemDto,
    ViewSelectorLink,
    WidgetTypes,
} from "../types/DashboardDto";
import AbstractClauseListEditor, {
    AbstractClauseRow,
} from "./AbstractClauseListEditor";

interface Candidate {
    itemId: string;
    trackerId: string;
    label: string;
}

/** One of the widget's own clauses, identified by its position in the list — the key its
    links use, since a clause has no pooled query id until the save resolves one. */
interface ParameterClause {
    key: string;
    dataType: string;
    operator: string;
}

/** The string form the backend stores a clause value in — mirrors DashboardViewsPanel. */
function normalizeClauseValue(value: unknown): string | null {
    if (value === undefined || value === null || value === "") return null;
    if (value instanceof Date) return value.toISOString();
    return String(value);
}

// The Analytic/Entries widgets a parameter widget can narrow — identical to the view
// selector's, since both layer clauses onto the same kinds of follower.
function candidatesFor(items: DashboardItemDto[]): Candidate[] {
    const out: Candidate[] = [];
    const seen = new Map<string, number>();
    for (const item of items) {
        const byTracker = new Map<string, string>();
        if (item.type === WidgetTypes.Analytic) {
            for (const s of item.sources) byTracker.set(s.trackerId, s.trackerName);
        } else if (item.type === WidgetTypes.Entries) {
            for (const trackerId of item.trackerIds) byTracker.set(trackerId, "");
        } else {
            continue;
        }

        const base =
            item.name ||
            (item.type === WidgetTypes.Entries ? "Entries table" : "Untitled widget");
        const n = (seen.get(base) ?? 0) + 1;
        seen.set(base, n);
        const name = n > 1 ? `${base} (${n})` : base;

        for (const [trackerId, trackerName] of byTracker) {
            out.push({
                itemId: item.id,
                trackerId,
                label:
                    byTracker.size > 1 && trackerName
                        ? `${name} · ${trackerName}`
                        : name,
            });
        }
    }
    return out;
}

interface Props {
    initial?: { clauses: AbstractClauseRow[]; links: ViewSelectorLink[] };
    submitLabel: string;
    color?: string;
    onBack: () => void;
    onSubmit: (dto: SaveParameterItemDto) => Promise<void>;
}

/** Adds or edits a parameter widget: a set of filter clauses whose values are typed on the
    board, re-filtering the Analytic/Entries widgets wired to it. */
export function ParameterForm({
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

    const [links, setLinks] = useState<ViewSelectorLink[]>(initial?.links ?? []);

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
    const parameterClauses = useMemo<ParameterClause[]>(
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
        rows.length > 0 && parameterClauses.length === rows.length;

    const eligibleFields = (trackerId: string, c: ParameterClause) =>
        (fieldsByTracker[trackerId] ?? []).filter((f) =>
            fieldTypesCompatible(f.type, c.dataType),
        );

    // Pin the field automatically wherever a tracker offers exactly one of the right type —
    // same as the view selector, so a board of single-date widgets needs no mapping by hand.
    useEffect(() => {
        setLinks((cur) => {
            let changed = false;
            const next = cur.map((l) => {
                let fieldByQuery = l.fieldByQuery;
                for (const c of parameterClauses) {
                    if (fieldByQuery[c.key]) continue;
                    const matches = eligibleFields(l.trackerId, c);
                    if (matches.length !== 1) continue;
                    if (fieldByQuery === l.fieldByQuery) fieldByQuery = { ...fieldByQuery };
                    fieldByQuery[c.key] = matches[0].id;
                    changed = true;
                }
                return fieldByQuery === l.fieldByQuery ? l : { ...l, fieldByQuery };
            });
            return changed ? next : cur;
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [fieldsByTracker, parameterClauses]);

    const linkFor = (c: Candidate) =>
        links.find((l) => l.itemId === c.itemId && l.trackerId === c.trackerId);

    const setLink = (c: Candidate, link: ViewSelectorLink | null) => {
        const rest = links.filter(
            (l) => !(l.itemId === c.itemId && l.trackerId === c.trackerId),
        );
        setLinks(link ? [...rest, link] : rest);
    };

    const describe = (c: ParameterClause) => {
        const type =
            fieldTypes.find((t) => t.value === c.dataType)?.label ?? c.dataType;
        return `${type} ${formatOperator(c.operator)}`.trim();
    };

    // Only clauses that still exist can carry a mapping — a link keeps working as clauses
    // are added, and stale keys from a removed clause are ignored on submit.
    const linksComplete = links.every((l) =>
        parameterClauses.every((c) => {
            const fieldId = l.fieldByQuery[c.key];
            if (!fieldId) return false;
            const field = (fieldsByTracker[l.trackerId] ?? []).find(
                (f) => f.id === fieldId,
            );
            return field != null && fieldTypesCompatible(field.type, c.dataType);
        }),
    );

    const canSubmit = clausesComplete && linksComplete;

    const handleSubmit = async () => {
        if (!canSubmit) return;
        setBusy(true);
        const clauses: ClauseDto[] = rows.map((c) => ({
            kind: QueryKinds.Filter,
            dataType: c.dataType,
            operator: c.operator,
            value: normalizeClauseValue(c.value),
            descending: false,
        }));
        // Drop mappings for clauses that no longer exist so the payload is clean.
        const cleanLinks = links.map((l) => ({
            ...l,
            fieldByQuery: Object.fromEntries(
                Object.entries(l.fieldByQuery).filter(([key]) =>
                    parameterClauses.some((c) => c.key === key),
                ),
            ),
        }));
        await onSubmit({ clauses, links: cleanLinks });
        setBusy(false);
    };

    return (
        <Stack gap="lg">
            <Stack gap="md">
                <Text fw={500} size="md">
                    What this widget filters
                </Text>
                <Text size="xs" c="dimmed">
                    Each clause becomes an input on the board. Leave a value blank here to
                    let it be set on the dashboard.
                </Text>
                <AbstractClauseListEditor
                    form={form}
                    path="clauses"
                    color={color}
                    filterOnly
                />
            </Stack>

            {clausesComplete && candidates.length > 0 && (
                <Stack gap="md">
                    <Text fw={500} size="md">
                        Followed widgets
                    </Text>
                    {candidates.map((c) => {
                        const eligible = parameterClauses.every(
                            (q) => eligibleFields(c.trackerId, q).length > 0,
                        );
                        const link = linkFor(c);
                        const choices = link
                            ? parameterClauses.filter(
                                  (q) => eligibleFields(c.trackerId, q).length > 1,
                              )
                            : [];
                        return (
                            <Stack key={`${c.itemId}:${c.trackerId}`} gap={4}>
                                <Checkbox
                                    label={c.label}
                                    disabled={!eligible}
                                    checked={!!link}
                                    onChange={(e) =>
                                        setLink(
                                            c,
                                            e.currentTarget.checked
                                                ? {
                                                      itemId: c.itemId,
                                                      trackerId: c.trackerId,
                                                      fieldByQuery: {},
                                                  }
                                                : null,
                                        )
                                    }
                                />
                                {link && choices.length === 0 && (
                                    <Text size="xs" c="dimmed" pl="lg">
                                        Fields matched automatically
                                    </Text>
                                )}
                                {link && choices.length > 0 && (
                                    <Group gap="sm" pl="lg" wrap="wrap">
                                        {choices.map((q) => (
                                            <Select
                                                key={q.key}
                                                size="xs"
                                                w={200}
                                                label={describe(q)}
                                                placeholder="Field"
                                                allowDeselect={false}
                                                data={eligibleFields(
                                                    c.trackerId,
                                                    q,
                                                ).map((f) => ({
                                                    value: f.id,
                                                    label: f.name,
                                                }))}
                                                value={
                                                    link.fieldByQuery[q.key] ?? null
                                                }
                                                onChange={(fieldId) =>
                                                    setLink(c, {
                                                        ...link,
                                                        fieldByQuery: {
                                                            ...link.fieldByQuery,
                                                            ...(fieldId
                                                                ? { [q.key]: fieldId }
                                                                : {}),
                                                        },
                                                    })
                                                }
                                            />
                                        ))}
                                    </Group>
                                )}
                            </Stack>
                        );
                    })}
                </Stack>
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
