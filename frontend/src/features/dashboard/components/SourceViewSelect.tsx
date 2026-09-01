import { Select } from "@mantine/core";

/** The filter half of a WidgetTypes.Entries item's Config — the only part a placement
    stores. Parsed defensively: Config is free-form JSON per widget type. */
interface EntriesItemConfig {
    viewId?: string | null;
}

export function parseEntriesItemConfig(config: string | undefined): EntriesItemConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return typeof parsed === "object" && parsed !== null ? parsed : null;
    } catch {
        return null;
    }
}

/** How one source or Entries table is filtered: a fixed view of its own tracker, or none.
    A view selector widget on the board can layer further clauses on top. */
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

/** Picks the fixed tracker view a source or Entries table reads through. */
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
