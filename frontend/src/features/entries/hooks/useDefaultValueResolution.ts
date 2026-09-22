import { useDebouncedValue } from "@mantine/hooks";
import { UseFormReturnType } from "@mantine/form";
import { useEffect, useMemo, useRef } from "react";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { evaluateVisibilityCondition } from "../../../shared/utils/evaluateVisibilityCondition";
import { GetStringValue } from "../components/EntryFormDialog";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyForm = UseFormReturnType<any>;

function coerceResolvedValue(type: string, value: string | undefined): unknown {
    if (!value) return "";
    if (type === "date" || type === "datetime") {
        const date = new Date(value);
        return isNaN(date.getTime()) ? "" : date;
    }
    return value;
}

/**
 * Live-fills fields whose default is linked to a (possibly conditional) constant, debounced
 * against the server, while a new entry is being created. A field the user has typed into
 * directly is never auto-overwritten again for the rest of this session. Also computes, entirely
 * client-side and with no debounce, the set of field names currently hidden by another field's
 * live value, so visibility never lags or flashes on open.
 */
export function useDefaultValueResolution(
    trackerId: string,
    fields: FieldDto[],
    form: AnyForm,
    enabled: boolean,
): { hiddenFieldNames: Set<string> } {
    const touchedRef = useRef<Set<string>>(new Set());
    const lastAutoFilledRef = useRef<Record<string, string | undefined>>({});

    const hasResolvableState = useMemo(
        () =>
            fields.some((f) => f.defaultValue || f.defaultValueConstantId),
        [fields],
    );

    const inputtableFields = useMemo(
        () => fields.filter((f) => !f.isCalculated),
        [fields],
    );

    const fieldsById = useMemo(
        () => new Map(fields.map((f) => [f.id, f])),
        [fields],
    );

    // Computed directly from live form state (no debounce, no round trip) so a hidden field
    // never flashes visible on open and updates the instant its trigger field changes.
    const hiddenFieldNames = useMemo(() => {
        const hidden = new Set<string>();
        for (const field of fields) {
            if (!field.visibilityFieldId || !field.visibilityOperator) continue;
            const target = fieldsById.get(field.visibilityFieldId);
            if (!target) continue;

            const currentRaw = GetStringValue(target.type, form.values[target.name]) || "";
            const visible = evaluateVisibilityCondition(
                target.type,
                currentRaw,
                field.visibilityOperator,
                field.visibilityValue,
            );
            if (!visible) hidden.add(field.name);
        }
        return hidden;
    }, [fields, fieldsById, form.values]);

    const valuesSnapshot = JSON.stringify(
        inputtableFields.map((f) => [
            f.name,
            GetStringValue(f.type, form.values[f.name]),
        ]),
    );
    const [debouncedSnapshot] = useDebouncedValue(valuesSnapshot, 400);

    useEffect(() => {
        if (!enabled || !hasResolvableState) return;

        // A field whose live value no longer matches what we last auto-filled it with was
        // changed by the user, so it must never be auto-filled again this session.
        for (const field of inputtableFields) {
            if (touchedRef.current.has(field.name)) continue;
            const lastAuto = lastAutoFilledRef.current[field.name];
            if (lastAuto === undefined) continue;
            const current = GetStringValue(field.type, form.values[field.name]);
            if (current !== lastAuto) touchedRef.current.add(field.name);
        }

        const fieldValues: Record<string, string | null> = {};
        inputtableFields.forEach((field) => {
            fieldValues[field.name] =
                GetStringValue(field.type, form.values[field.name]) || null;
        });

        fieldsController
            .resolveDefaults(trackerId, fieldValues)
            .then((res) => {
                for (const resolved of res.data?.defaults ?? []) {
                    if (touchedRef.current.has(resolved.fieldName)) continue;
                    const field = inputtableFields.find(
                        (f) => f.id === resolved.fieldId,
                    );
                    if (!field) continue;

                    lastAutoFilledRef.current[resolved.fieldName] =
                        resolved.value ?? "";
                    form.setFieldValue(
                        resolved.fieldName,
                        coerceResolvedValue(field.type, resolved.value),
                    );
                }
            })
            .catch(() => {
                // Best-effort convenience; a failed resolve just leaves fields as they are.
            });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [debouncedSnapshot, enabled, hasResolvableState]);

    return { hiddenFieldNames };
}
