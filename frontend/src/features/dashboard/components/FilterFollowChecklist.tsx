import { Checkbox, Group, Select, Stack, Text } from "@mantine/core";
import { useEffect } from "react";
import { fieldTypesCompatible } from "../../../shared/constants/DataTypes";
import { FieldDto } from "../../fields/types/FieldDto";
import { FilterCandidate } from "./filterLinkUtils";

interface Props {
    /** The new widget's own tracker fields -- what each filter clause can map to. */
    fields: FieldDto[];
    /** The board's existing filter widgets this widget could follow. */
    filters: FilterCandidate[];
    /** filterItemId -> (that filter's pooled query id -> field id), for the filters
        currently checked. */
    links: Record<string, Record<string, string>>;
    onLinksChange: (links: Record<string, Record<string, string>>) => void;
}

/**
 * A "Follow filters" checklist offered while adding a widget: one checkbox per existing
 * filter widget on the board, expanding into a per-clause field picker once checked. A
 * tracker offering exactly one field of the right type for a clause has it pinned
 * automatically, the same shortcut the filter widget's own "Followed by" editor takes.
 */
export function FilterFollowChecklist({ fields, filters, links, onLinksChange }: Props) {
    const eligibleFields = (dataType: string) =>
        fields.filter((f) => fieldTypesCompatible(f.type, dataType));

    // Fills in any clause with exactly one eligible field the moment its filter is checked
    // (or the fields load), the same auto-pin FollowedWidgetsSection does.
    useEffect(() => {
        let changed = false;
        const next = { ...links };
        for (const filter of filters) {
            const current = next[filter.itemId];
            if (!current) continue;
            let patched = current;
            for (const q of filter.queries) {
                if (patched[q.queryId]) continue;
                const matches = eligibleFields(q.dataType);
                if (matches.length !== 1) continue;
                if (patched === current) patched = { ...patched };
                patched[q.queryId] = matches[0].id;
                changed = true;
            }
            if (patched !== current) next[filter.itemId] = patched;
        }
        if (changed) onLinksChange(next);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [fields, filters, links]);

    const setChecked = (filter: FilterCandidate, checked: boolean) => {
        const next = { ...links };
        if (checked) next[filter.itemId] = {};
        else delete next[filter.itemId];
        onLinksChange(next);
    };

    if (filters.length === 0) return null;

    return (
        <Stack gap="xs">
            <Text size="sm" fw={500}>
                Follow filters
            </Text>
            {filters.map((filter) => {
                const eligible = filter.queries.every(
                    (q) => eligibleFields(q.dataType).length > 0,
                );
                const fieldByQuery = links[filter.itemId];
                const checked = !!fieldByQuery;
                // Only the queries with more than one candidate field need a dropdown; the
                // rest are pinned automatically by the effect above.
                const choices = checked
                    ? filter.queries.filter((q) => eligibleFields(q.dataType).length > 1)
                    : [];
                return (
                    <Stack key={filter.itemId} gap={4}>
                        <Checkbox
                            label={filter.label}
                            disabled={!eligible}
                            checked={checked}
                            onChange={(e) => setChecked(filter, e.currentTarget.checked)}
                        />
                        {checked && choices.length > 0 && (
                            <Group gap="sm" pl="lg" wrap="wrap">
                                {choices.map((q) => (
                                    <Select
                                        key={q.queryId}
                                        size="xs"
                                        w={200}
                                        label={q.describe}
                                        placeholder="Field"
                                        allowDeselect={false}
                                        data={eligibleFields(q.dataType).map((f) => ({
                                            value: f.id,
                                            label: f.name,
                                        }))}
                                        value={fieldByQuery[q.queryId] ?? null}
                                        onChange={(fieldId) =>
                                            onLinksChange({
                                                ...links,
                                                [filter.itemId]: {
                                                    ...fieldByQuery,
                                                    ...(fieldId ? { [q.queryId]: fieldId } : {}),
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
