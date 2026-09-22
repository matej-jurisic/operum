import { Group, NumberInput, SegmentedControl, Select, Stack, Text } from "@mantine/core";
import { UseFormReturnType } from "@mantine/form";
import { useEffect, useState } from "react";
import FieldValueInput from "../../features/fields/components/FieldValueInput";
import { FieldDto } from "../../features/fields/types/FieldDto";
import {
    anchorOptionsForField,
    DateAnchor,
    DateAnchors,
    isDynamicDateToken,
    lookbackToAnchorToken,
    NOW_TOKEN,
    parseAnchorToken,
    resolveDynamicDateToken,
    serializeAnchorToken,
} from "../constants/dynamicDateTokens";
import {
    formatDateOnlyFromDate,
    formatDateTimeFromDate,
} from "../utils/formatters/TypeFormatter";

interface Props {
    isDateType: boolean;
    value: string | number | Date | undefined;
    onChange: (value: string | number | Date | undefined) => void;
    field: FieldDto;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    form: UseFormReturnType<any>;
    fieldPath: string;
    label?: string;
}

type DateMode = "date" | "relative";

export default function DynamicDateValueInput({
    isDateType,
    value,
    onChange,
    field,
    form,
    fieldPath,
    label,
}: Props) {
    // A pure date has no time of day, so start/end of day collapse and "now" makes no sense.
    const isDateOnly = field?.type === "date";

    // Old filters stored a lookback token; open them in the same editor as the anchor form.
    const token =
        typeof value === "string" ? (lookbackToAnchorToken(value) ?? value) : undefined;
    const isToken = token !== undefined && isDynamicDateToken(token);
    const isNow = token === NOW_TOKEN;

    const dateMode: DateMode = isToken ? "relative" : "date";

    const anchorToken = isToken && !isNow ? parseAnchorToken(token) : null;
    const anchor = anchorToken?.anchor ?? DateAnchors.Today;
    const offset = anchorToken?.offset ?? 0;

    // Offset 0 has no sign (serializeAnchorToken drops it entirely), so direction
    // can't be recovered from the stored value alone once the magnitude hits 0.
    // Track it separately and only resync it from the offset when the offset
    // actually carries a sign.
    const [direction, setDirection] = useState<"past" | "future">(
        offset < 0 ? "past" : "future",
    );
    useEffect(() => {
        if (offset !== 0) setDirection(offset < 0 ? "past" : "future");
    }, [offset]);

    const currentToken = isNow ? NOW_TOKEN : serializeAnchorToken(anchor, offset);
    const preview = isToken ? resolveDynamicDateToken(currentToken) : null;

    const anchorData = [
        ...(isDateOnly ? [] : [{ value: NOW_TOKEN, label: "Now" }]),
        ...anchorOptionsForField(isDateOnly),
    ];

    const handleModeChange = (v: string) => {
        if (v === dateMode) return;
        onChange(v === "relative" ? DateAnchors.Today : undefined);
    };

    return (
        <Stack flex={1} gap={4}>
            {isDateType && (
                <SegmentedControl
                    size="xs"
                    data={[
                        { value: "date", label: "Date" },
                        { value: "relative", label: "Relative" },
                    ]}
                    value={dateMode}
                    onChange={handleModeChange}
                />
            )}

            {(!isDateType || dateMode === "date") && (
                <FieldValueInput
                    field={field}
                    form={form}
                    fieldPath={fieldPath}
                    styles={{ flex: 1 }}
                    referenceValueMode="label"
                />
            )}

            {isDateType && dateMode === "relative" && (
                <Stack gap={4}>
                    <Group gap="xs" align="flex-end" grow>
                        <Select
                            label={label ?? "Value"}
                            data={anchorData}
                            value={isNow ? NOW_TOKEN : anchor}
                            onChange={(v) => {
                                if (!v) return;
                                onChange(
                                    v === NOW_TOKEN
                                        ? NOW_TOKEN
                                        : serializeAnchorToken(v as DateAnchor, offset),
                                );
                            }}
                            allowDeselect={false}
                        />
                        {!isNow && (
                            <NumberInput
                                label="Offset"
                                min={0}
                                value={Math.abs(offset)}
                                onChange={(v) => {
                                    const magnitude = typeof v === "number" && v > 0 ? v : 0;
                                    const signed = direction === "future" ? magnitude : -magnitude;
                                    onChange(serializeAnchorToken(anchor, signed));
                                }}
                            />
                        )}
                    </Group>
                    {!isNow && (
                        <SegmentedControl
                            size="xs"
                            fullWidth
                            data={[
                                { value: "past", label: "Ago" },
                                { value: "future", label: "From now" },
                            ]}
                            value={direction}
                            onChange={(v) => {
                                const next = v as "past" | "future";
                                setDirection(next);
                                const magnitude = Math.abs(offset);
                                onChange(
                                    serializeAnchorToken(
                                        anchor,
                                        next === "future" ? magnitude : -magnitude,
                                    ),
                                );
                            }}
                        />
                    )}
                </Stack>
            )}

            {preview && (
                <Text size="xs" c="dimmed">
                    Right now:{" "}
                    {isDateOnly
                        ? formatDateOnlyFromDate(preview)
                        : formatDateTimeFromDate(preview)}
                </Text>
            )}
        </Stack>
    );
}
