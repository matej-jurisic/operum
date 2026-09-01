import {
    Button,
    Checkbox,
    Group,
    MultiSelect,
    Select,
    Stack,
    Text,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { fieldTypesCompatible } from "../../../shared/constants/DataTypes";
import { describeAbstractClause } from "../../../shared/utils/formatters/QueryFormatter";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import {
    DashboardItemDto,
    DashboardViewClauseDto,
    DashboardViewDto,
    SaveViewSelectorItemDto,
    ViewSelectorLink,
    WidgetTypes,
} from "../types/DashboardDto";
import { DashboardViewsPanel } from "./DashboardViewsPanel";

interface Candidate {
    itemId: string;
    trackerId: string;
    label: string;
}

function candidatesFor(items: DashboardItemDto[]): Candidate[] {
    const out: Candidate[] = [];
    const seen = new Map<string, number>();
    for (const item of items) {
        // An Analytic widget reads from one tracker per source; an Entries widget from
        // exactly one (on item.trackerIds, since it has no sources). Every other kind reads
        // no tracker and can't be narrowed.
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
    initial?: SaveViewSelectorItemDto;
    submitLabel: string;
    color?: string;
    onBack: () => void;
    onSubmit: (dto: SaveViewSelectorItemDto) => Promise<void>;
}

export function ViewSelectorForm({
    initial,
    submitLabel,
    color,
    onBack,
    onSubmit,
}: Props) {
    const { dashboardId } = useDashboard();
    const [views, setViews] = useState<DashboardViewDto[]>([]);
    const [items, setItems] = useState<DashboardItemDto[]>([]);
    const [fieldsByTracker, setFieldsByTracker] = useState<Record<string, FieldDto[]>>(
        {},
    );
    const [busy, setBusy] = useState(false);

    const [optionIds, setOptionIds] = useState<string[]>(initial?.optionIds ?? []);
    const [selectedId, setSelectedId] = useState<string | null>(
        initial?.selectedId ?? null,
    );
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

    // Distinct queries across the chosen options, each with the data type its field must be.
    const optionQueries = useMemo(() => {
        const map = new Map<string, DashboardViewClauseDto>();
        for (const v of views) {
            if (!optionIds.includes(v.id)) continue;
            for (const c of v.clauses) if (!map.has(c.queryId)) map.set(c.queryId, c);
        }
        return [...map.values()];
    }, [views, optionIds]);

    const eligibleFields = (trackerId: string, q: DashboardViewClauseDto) =>
        (fieldsByTracker[trackerId] ?? []).filter((f) =>
            fieldTypesCompatible(f.type, q.dataType),
        );

    // When a tracker offers exactly one field of the right type there is no choice to make,
    // so fill it in automatically. On a board where every widget has a single date field
    // this leaves nothing to map by hand.
    useEffect(() => {
        setLinks((cur) => {
            let changed = false;
            const next = cur.map((l) => {
                let fieldByQuery = l.fieldByQuery;
                for (const q of optionQueries) {
                    if (fieldByQuery[q.queryId]) continue;
                    const matches = eligibleFields(l.trackerId, q);
                    if (matches.length !== 1) continue;
                    if (fieldByQuery === l.fieldByQuery) fieldByQuery = { ...fieldByQuery };
                    fieldByQuery[q.queryId] = matches[0].id;
                    changed = true;
                }
                return fieldByQuery === l.fieldByQuery ? l : { ...l, fieldByQuery };
            });
            return changed ? next : cur;
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [fieldsByTracker, optionQueries]);

    const linkFor = (c: Candidate) =>
        links.find((l) => l.itemId === c.itemId && l.trackerId === c.trackerId);

    const setLink = (c: Candidate, link: ViewSelectorLink | null) => {
        const rest = links.filter(
            (l) => !(l.itemId === c.itemId && l.trackerId === c.trackerId),
        );
        setLinks(link ? [...rest, link] : rest);
    };

    const optionData = views.map((v) => ({ value: v.id, label: v.name }));

    // A link is complete once every option query is mapped to a field of the right type.
    const linksComplete = links.every((l) =>
        optionQueries.every((q) => {
            const fieldId = l.fieldByQuery[q.queryId];
            if (!fieldId) return false;
            const field = (fieldsByTracker[l.trackerId] ?? []).find(
                (f) => f.id === fieldId,
            );
            return field != null && fieldTypesCompatible(field.type, q.dataType);
        }),
    );

    const canSubmit = optionIds.length > 0 && linksComplete;

    const handleSubmit = async () => {
        if (!canSubmit) return;
        setBusy(true);
        await onSubmit({
            optionIds,
            selectedId: selectedId && optionIds.includes(selectedId) ? selectedId : null,
            links,
        });
        setBusy(false);
    };

    return (
        <Stack gap="lg">
            <DashboardViewsPanel onChange={setViews} color={color} />

            <Stack gap="md">
                <Text fw={500} size="md">
                    Dropdown
                </Text>
                <MultiSelect
                    label="Options in the dropdown"
                    placeholder="Pick filter sets"
                    data={optionData}
                    value={optionIds}
                    onChange={setOptionIds}
                />

                <Select
                    label="Starting selection"
                    placeholder="None"
                    data={optionData.filter((o) => optionIds.includes(o.value))}
                    value={selectedId}
                    onChange={setSelectedId}
                    clearable
                />
            </Stack>

            {optionIds.length > 0 && candidates.length > 0 && (
                <Stack gap="md">
                    <Text fw={500} size="md">
                        Followed widgets
                    </Text>
                    {candidates.map((c) => {
                        const eligible = optionQueries.every(
                            (q) => eligibleFields(c.trackerId, q).length > 0,
                        );
                        const link = linkFor(c);
                        // Only the queries with more than one candidate field need a
                        // dropdown; the rest are pinned automatically by the effect above.
                        const choices = link
                            ? optionQueries.filter(
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
                                                key={q.queryId}
                                                size="xs"
                                                w={200}
                                                label={describeAbstractClause(q)}
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
                                                    link.fieldByQuery[q.queryId] ??
                                                    null
                                                }
                                                onChange={(fieldId) =>
                                                    setLink(c, {
                                                        ...link,
                                                        fieldByQuery: {
                                                            ...link.fieldByQuery,
                                                            ...(fieldId
                                                                ? {
                                                                      [q.queryId]:
                                                                          fieldId,
                                                                  }
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
