import { Select } from "@mantine/core";

/** How one analytic source is filtered: a fixed view of its own tracker, or none. A view
    selector widget on the board can layer further clauses on top. */
export interface ViewSelection {
    viewId: string | null;
}

interface Props {
    /** The tracker's views, any of which can be fixed onto the source. */
    views: { id: string; name: string }[];
    value: ViewSelection;
    onChange: (value: ViewSelection) => void;
    disabled?: boolean;
    placeholder?: string;
}

/** Picks the fixed tracker view an analytic source reads through. */
export function SourceViewSelect({
    views,
    value,
    onChange,
    disabled,
    placeholder,
}: Props) {
    return (
        <Select
            label="Filter by view (optional)"
            placeholder={placeholder ?? "All entries"}
            data={views.map((v) => ({ value: v.id, label: v.name }))}
            value={value.viewId}
            onChange={(viewId) => onChange({ viewId })}
            disabled={disabled}
            clearable
        />
    );
}
