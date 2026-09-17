import { fieldTypesCompatible } from "../../../shared/constants/DataTypes";
import { FieldDto } from "../../fields/types/FieldDto";
import { QueryKinds } from "../../../shared/constants/QueryKinds";
import {
    ClauseDto,
    DashboardWidgetDto,
    SaveFilterItemDto,
    WidgetLink,
    WidgetTypes,
    parseFilterWidgetConfig,
} from "../types/DashboardDto";
import { clauseLabel } from "./filterClauseInput";

/** An existing filter widget offered as something a newly created widget can follow.
    Clauses are keyed by their stable slot id, since this widget's shape is already saved. */
export interface FilterCandidate {
    itemId: string;
    label: string;
    queries: {
        slotId: string;
        dataType: string;
        operator?: string | null;
        describe: string;
    }[];
}

/** One entry per Filter item with complete clauses, deduped by label so two filter widgets
    with the same clause set never read the same. */
export function filterCandidatesFor(widgets: DashboardWidgetDto[]): FilterCandidate[] {
    const filterWidgets = widgets.filter(
        (w) => w.type === WidgetTypes.Filter && (w.filter?.clauses.length ?? 0) > 0,
    );
    const seen = new Map<string, number>();
    return filterWidgets.map((w) => {
        const clauses = w.filter!.clauses;
        const base = clauses.map((c) => clauseLabel(c.dataType, c.operator)).join(", ");
        const n = (seen.get(base) ?? 0) + 1;
        seen.set(base, n);
        return {
            itemId: w.id,
            label: n > 1 ? `${base} (${n})` : base,
            queries: clauses.map((c) => ({
                slotId: c.slotId,
                dataType: c.dataType,
                operator: c.operator,
                describe: clauseLabel(c.dataType, c.operator),
            })),
        };
    });
}

/** A filter clause a goal placement follows, offered as something a conditional target can
    key off. `fieldName` is the field this clause runs against for this goal widget, shown so
    two same-shape clauses read apart. */
export interface ConnectedClause {
    slotId: string;
    dataType: string;
    operator?: string | null;
    fieldName?: string;
}

/** Builds GoalConditionalTargetsEditor's clause list from an in-memory "follow filters"
    selection, before it's saved. Mirrors what EditWidgetModal derives once persisted. */
export function connectedClausesFromLinks(
    linksByFilter: Record<string, Record<string, string>>,
    filters: FilterCandidate[],
    fieldNameById: Record<string, string>,
): ConnectedClause[] {
    const out: ConnectedClause[] = [];
    const seen = new Set<string>();
    for (const [filterItemId, fieldBySlot] of Object.entries(linksByFilter)) {
        const filter = filters.find((f) => f.itemId === filterItemId);
        if (!filter) continue;
        for (const q of filter.queries) {
            const fieldId = fieldBySlot[q.slotId];
            if (!fieldId || seen.has(q.slotId)) continue;
            seen.add(q.slotId);
            out.push({
                slotId: q.slotId,
                dataType: q.dataType,
                operator: q.operator,
                fieldName: fieldNameById[fieldId],
            });
        }
    }
    return out;
}

/** Passed alongside a widget's create/place dto to link it to filters in the same step. */
export interface FilterFollowLinks {
    trackerId: string;
    /** filterItemId -> (that filter's clause slot id -> field id on `trackerId`) */
    links: Record<string, Record<string, string>>;
}

/** Same completeness check a filter widget's own edit dialog runs, gating "Add" the same way. */
export function followLinksComplete(
    links: Record<string, Record<string, string>>,
    filters: FilterCandidate[],
    fields: FieldDto[],
): boolean {
    const eligibleFields = (dataType: string) =>
        fields.filter((f) => fieldTypesCompatible(f.type, dataType));
    return Object.entries(links).every(([filterItemId, fieldBySlot]) => {
        const filter = filters.find((f) => f.itemId === filterItemId);
        if (!filter) return false;
        return filter.queries.every((q) => {
            const fieldId = fieldBySlot[q.slotId];
            return !!fieldId && eligibleFields(q.dataType).some((f) => f.id === fieldId);
        });
    });
}

/** Slot id -> clause index, the key SaveFilterItemDto's links expect (the backend rewrites
    indices back to slot ids on save). */
export function filterWidgetIndexBySlotId(widget: DashboardWidgetDto): Map<string, string> {
    return new Map((widget.filter?.clauses ?? []).map((c, i) => [c.slotId, String(i)]));
}

/** Rebuilds the SaveFilterItemDto an existing filter widget would resubmit unchanged, so a
    follower link can be appended without going through its own edit dialog. Values are
    omitted; the backend carries current ones across when clauses still pool the same query. */
export function filterWidgetToSaveDto(widget: DashboardWidgetDto): SaveFilterItemDto {
    const config = parseFilterWidgetConfig(widget.config);
    const clauseDtos = widget.filter?.clauses ?? [];
    const indexBySlotId = filterWidgetIndexBySlotId(widget);

    const clauses: ClauseDto[] = clauseDtos.map((c) => ({
        kind: QueryKinds.Filter,
        dataType: c.dataType,
        operator: c.operator ?? "",
        value: null,
        descending: false,
    }));

    const links: WidgetLink[] = (config?.links ?? []).map((l) => ({
        itemId: l.itemId,
        trackerId: l.trackerId,
        fieldByQuery: Object.fromEntries(
            Object.entries(l.fieldByQuery).flatMap(([slotId, fieldId]) => {
                const index = indexBySlotId.get(slotId);
                return index !== undefined ? [[index, fieldId]] : [];
            }),
        ),
    }));

    return { clauses, links, presetIds: config?.presetIds ?? [] };
}

/** Converts one follower's slot-id-keyed field mapping into the clause-index-keyed
    WidgetLink a filter widget's SaveFilterItemDto expects. */
export function toFollowerLink(
    indexBySlotId: Map<string, string>,
    follower: { itemId: string; trackerId: string; fieldBySlotId: Record<string, string> },
): WidgetLink {
    return {
        itemId: follower.itemId,
        trackerId: follower.trackerId,
        fieldByQuery: Object.fromEntries(
            Object.entries(follower.fieldBySlotId).flatMap(([slotId, fieldId]) => {
                const index = indexBySlotId.get(slotId);
                return index !== undefined ? [[index, fieldId]] : [];
            }),
        ),
    };
}
