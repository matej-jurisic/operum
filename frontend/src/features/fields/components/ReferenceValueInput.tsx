import { Select } from "@mantine/core";
import { UseFormReturnType } from "@mantine/form";
import { useDebouncedValue } from "@mantine/hooks";
import { CSSProperties, useEffect, useMemo, useState } from "react";
import { entriesController } from "../../entries/api/entriesController";
import { FieldDto } from "../types/FieldDto";

// Matches FieldValueInput's loose form typing so both can share the same call sites.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyForm = UseFormReturnType<any>;

interface ReferenceValueInputProps {
    field: FieldDto;
    form: AnyForm;
    fieldPath?: string;
    styles?: CSSProperties;
    /** "id" (entry forms) stores the linked entry's id; "label" (filter editors) stores the label text. */
    valueMode?: "id" | "label";
    /** Label for the currently selected value, so it renders before a search runs. */
    referenceLabel?: string;
}

interface Option {
    value: string;
    label: string;
}

export default function ReferenceValueInput({
    field,
    form,
    fieldPath,
    styles,
    valueMode = "id",
    referenceLabel,
}: ReferenceValueInputProps) {
    const path = fieldPath || field.name;
    const inputProps = form.getInputProps(path);
    const currentValue = (inputProps.value as string) || "";

    const [search, setSearch] = useState("");
    const [debounced] = useDebouncedValue(search, 300);
    const [fetched, setFetched] = useState<Option[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (!field.referencedTrackerId) return;

        let cancelled = false;
        setLoading(true);
        entriesController
            .getEntryOptions(
                field.referencedTrackerId,
                field.referencedDisplayFieldId,
                debounced.trim() || undefined,
                20,
            )
            .then((res) => {
                if (cancelled) return;
                setFetched(
                    (res.data ?? []).map((o) => ({
                        value: valueMode === "label" ? o.label : o.id,
                        label: o.label,
                    })),
                );
            })
            .catch(() => {
                if (!cancelled) setFetched([]);
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [
        debounced,
        field.referencedTrackerId,
        field.referencedDisplayFieldId,
        valueMode,
    ]);

    // Keep the current value selectable even when the latest search doesn't include it.
    const data = useMemo<Option[]>(() => {
        const byValue = new Map<string, Option>();
        for (const option of fetched) byValue.set(option.value, option);
        if (currentValue && !byValue.has(currentValue)) {
            byValue.set(currentValue, {
                value: currentValue,
                label:
                    valueMode === "label"
                        ? currentValue
                        : referenceLabel || currentValue,
            });
        }
        return [...byValue.values()];
    }, [fetched, currentValue, referenceLabel, valueMode]);

    return (
        <Select
            label={field.name}
            description={field.description || undefined}
            required={field.required}
            placeholder="Search entries"
            searchable
            clearable
            nothingFoundMessage={loading ? "Searching..." : "No matches"}
            style={styles}
            data={data}
            searchValue={search}
            onSearchChange={setSearch}
            value={currentValue || null}
            onChange={(val) => form.setFieldValue(path, val ?? "")}
            error={inputProps.error}
        />
    );
}
