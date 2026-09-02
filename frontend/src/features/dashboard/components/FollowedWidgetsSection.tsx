import { Checkbox, Group, Select, Stack, Text } from "@mantine/core";
import { useEffect } from "react";
import { fieldTypesCompatible } from "../../../shared/constants/DataTypes";
import { FieldDto } from "../../fields/types/FieldDto";
import { DashboardItemDto, WidgetLink, WidgetTypes } from "../types/DashboardDto";

export interface Candidate {
    itemId: string;
    trackerId: string;
    label: string;
}

// The Analytic/Entries widgets a filter widget's typed clauses or presets can narrow. An
// Analytic widget reads from one tracker per source; an Entries widget from exactly one (on
// item.trackerIds, since it has no sources). Every other kind reads no tracker and can't be
// narrowed.
export function candidatesFor(items: DashboardItemDto[]): Candidate[] {
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

/** One clause a followed widget's link can be mapped to a field for -- kept deliberately
    thin (just what the checklist below needs) so it fits both a filter widget's own typed
    clauses (keyed by clause index until save) and a preset's clauses (keyed by their real
    pooled query id). */
export interface FollowedWidgetQuery {
    key: string;
    dataType: string;
    describe: string;
}

interface Props {
    title: string;
    candidates: Candidate[];
    fieldsByTracker: Record<string, FieldDto[]>;
    queries: FollowedWidgetQuery[];
    links: WidgetLink[];
    onLinksChange: (links: WidgetLink[]) => void;
}

/**
 * The "which widgets follow this" checklist shared by a filter widget's two independent
 * link lists (its own typed clauses, and whichever preset is applied): one checkbox per
 * candidate Analytic/Entries widget + tracker, expanding into a per-clause field picker once
 * checked. A tracker offering exactly one field of the right type for a clause has it pinned
 * automatically, so a board of single-date widgets needs no mapping by hand.
 */
export function FollowedWidgetsSection({
    title,
    candidates,
    fieldsByTracker,
    queries,
    links,
    onLinksChange,
}: Props) {
    const eligibleFields = (trackerId: string, q: FollowedWidgetQuery) =>
        (fieldsByTracker[trackerId] ?? []).filter((f) =>
            fieldTypesCompatible(f.type, q.dataType),
        );

    // When a tracker offers exactly one field of the right type there is no choice to make,
    // so fill it in automatically. Runs on `links` too so a widget just checked gets its
    // fields pinned right away, not only when the queries or fields next change -- otherwise
    // its link stays incomplete and the submit button never enables. Safe against a loop: the
    // updater returns `cur` unchanged once everything single-valued is filled.
    useEffect(() => {
        const next = links.map((l) => {
            let fieldByQuery = l.fieldByQuery;
            for (const q of queries) {
                if (fieldByQuery[q.key]) continue;
                const matches = eligibleFields(l.trackerId, q);
                if (matches.length !== 1) continue;
                if (fieldByQuery === l.fieldByQuery) fieldByQuery = { ...fieldByQuery };
                fieldByQuery[q.key] = matches[0].id;
            }
            return fieldByQuery === l.fieldByQuery ? l : { ...l, fieldByQuery };
        });
        if (next.some((l, i) => l !== links[i])) onLinksChange(next);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [fieldsByTracker, queries, links]);

    const linkFor = (c: Candidate) =>
        links.find((l) => l.itemId === c.itemId && l.trackerId === c.trackerId);

    const setLink = (c: Candidate, link: WidgetLink | null) => {
        const rest = links.filter(
            (l) => !(l.itemId === c.itemId && l.trackerId === c.trackerId),
        );
        onLinksChange(link ? [...rest, link] : rest);
    };

    if (queries.length === 0 || candidates.length === 0) return null;

    return (
        <Stack gap="md">
            <Text fw={500} size="md">
                {title}
            </Text>
            {candidates.map((c) => {
                const eligible = queries.every((q) => eligibleFields(c.trackerId, q).length > 0);
                const link = linkFor(c);
                // Only the queries with more than one candidate field need a dropdown; the
                // rest are pinned automatically by the effect above.
                const choices = link
                    ? queries.filter((q) => eligibleFields(c.trackerId, q).length > 1)
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
                                        ? { itemId: c.itemId, trackerId: c.trackerId, fieldByQuery: {} }
                                        : null,
                                )
                            }
                        />
                        {link && choices.length > 0 && (
                            <Group gap="sm" pl="lg" wrap="wrap">
                                {choices.map((q) => (
                                    <Select
                                        key={q.key}
                                        size="xs"
                                        w={200}
                                        label={q.describe}
                                        placeholder="Field"
                                        allowDeselect={false}
                                        data={eligibleFields(c.trackerId, q).map((f) => ({
                                            value: f.id,
                                            label: f.name,
                                        }))}
                                        value={link.fieldByQuery[q.key] ?? null}
                                        onChange={(fieldId) =>
                                            setLink(c, {
                                                ...link,
                                                fieldByQuery: {
                                                    ...link.fieldByQuery,
                                                    ...(fieldId ? { [q.key]: fieldId } : {}),
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
    );
}
