import { useMemo } from "react";
import { FieldDto } from "../../fields/types/FieldDto";
import { useFields } from "../../fields/context/FieldsContext";
import { useViews } from "../../views/context/ViewsContext";

/** Columns of the entries table that are not fields of the tracker. */
export const ExtraColumns = {
    CreatedAt: "createdAt",
    Actions: "actions",
} as const;

/**
 * Which columns the entries table shows, and in which order.
 *
 * Columns are the last step of a view, applied here rather than on the server: the
 * entries endpoint keeps returning every value, so a filter or a sort over a column the
 * view hides goes on working. A view names the fields it shows, in the order it shows
 * them; a view naming none shows every field, which is what every view did before
 * columns existed.
 *
 * On top of that the eye menu can tick a column on or off for the session. Those picks
 * live in FieldsContext and are dropped when the view changes.
 */
export function useVisibleColumns() {
    const { fields, columnOverrides, setColumnVisible } = useFields();
    const { selectedView } = useViews();

    // The fields the view asks for, in view order, skipping repeats and any field the
    // tracker no longer has.
    const viewFields = useMemo(() => {
        if (!selectedView) return null;

        const seen = new Set<string>();
        const picked: FieldDto[] = [];

        for (const fieldId of selectedView.columnFieldIds) {
            if (seen.has(fieldId)) continue;
            seen.add(fieldId);

            const field = fields.find((f) => f.id === fieldId);
            if (field) picked.push(field);
        }

        return picked.length > 0 ? picked : null;
    }, [selectedView, fields]);

    const viewControlsColumns = viewFields !== null;

    const isColumnVisible = (columnId: string) => {
        const override = columnOverrides[columnId];
        if (override !== undefined) return override;
        if (!viewControlsColumns) return true;
        // Created At and the actions column are not fields, so no view speaks for them.
        if (!fields.some((f) => f.id === columnId)) return true;
        return viewFields.some((f) => f.id === columnId);
    };

    const toggleColumn = (columnId: string) =>
        setColumnVisible(columnId, !isColumnVisible(columnId));

    // Fields the view named come first in its own order; anything switched on by hand
    // afterwards follows in field order.
    const visibleFields = useMemo(() => {
        const ordered = viewFields ?? fields;
        const extras = viewFields
            ? fields.filter((f) => !viewFields.includes(f))
            : [];

        return [...ordered, ...extras].filter((f) => isColumnVisible(f.id));
    }, [viewFields, fields, columnOverrides]);

    return {
        /** Every field of the tracker, whether shown or not. */
        fields,
        visibleFields,
        isColumnVisible,
        toggleColumn,
        viewControlsColumns,
    };
}
