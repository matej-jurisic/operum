import { Group, NumberInput, SegmentedControl, Select, Stack, Text } from "@mantine/core";
import FieldValueInput from "../../features/fields/components/FieldValueInput";
import {
    anchorOptions,
    DateAnchor,
    DateAnchors,
    describeOffset,
    isDynamicDateToken,
    isLookbackToken,
    LookbackPrefix,
    LookbackPrefixes,
    parseAnchorToken,
    parseLookbackToken,
    resolveDynamicDateToken,
    serializeAnchorToken,
    serializeLookbackToken,
} from "../constants/dynamicDateTokens";
import { formatDateTimeFromDate } from "../utils/formatters/TypeFormatter";

interface Props {
    isDateType: boolean;
    value: string | number | Date | undefined;
    onChange: (value: string | number | Date | undefined) => void;
    field: any;
    form: any;
    fieldPath: string;
    label?: string;
}

const UNIT_OPTIONS = [
    { value: LookbackPrefixes.LastNHours, label: "Hours" },
    { value: LookbackPrefixes.LastNDays, label: "Days" },
    { value: LookbackPrefixes.LastNWeeks, label: "Weeks" },
    { value: LookbackPrefixes.LastNMonths, label: "Months" },
];

/** Offsets a filter realistically needs; anything further out is a fixed date in practice. */
const OFFSET_OPTIONS = [-3, -2, -1, 0, 1];

type DateMode = "date" | "named" | "relative";

export default function DynamicDateValueInput({
    isDateType,
    value,
    onChange,
    field,
    form,
    fieldPath,
    label,
}: Props) {
    const isRelative = isLookbackToken(value);
    const isNamed = isDynamicDateToken(value) && !isRelative;

    const anchorToken = isNamed ? parseAnchorToken(value as string) : null;
    const lookback = isRelative ? parseLookbackToken(value as string) : null;

    const dateMode: DateMode = isRelative ? "relative" : isNamed ? "named" : "date";

    const anchor = anchorToken?.anchor ?? DateAnchors.Today;
    const offset = anchorToken?.offset ?? 0;
    const relativeUnit = lookback?.prefix ?? LookbackPrefixes.LastNDays;
    const relativeAmount = lookback?.n ?? 7;

    // The token itself says nothing about what it currently points at, so show the resolved date.
    const preview =
        typeof value === "string" && isDynamicDateToken(value)
            ? resolveDynamicDateToken(value)
            : null;

    const handleModeChange = (v: string) => {
        if (v === dateMode) return;
        if (v === "named") onChange(DateAnchors.Today);
        else if (v === "relative")
            onChange(serializeLookbackToken(LookbackPrefixes.LastNDays, 7));
        else onChange(undefined);
    };

    return (
        <Stack flex={1} gap={4}>
            {isDateType && (
                <SegmentedControl
                    size="xs"
                    data={[
                        { value: "date", label: "Date" },
                        { value: "named", label: "Named" },
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
                />
            )}

            {isDateType && dateMode === "named" && (
                <Group gap="xs" align="flex-end" grow>
                    <Select
                        label={label ?? "Value"}
                        placeholder="Select named date"
                        data={anchorOptions}
                        value={anchor}
                        onChange={(v) =>
                            onChange(serializeAnchorToken((v as DateAnchor) ?? anchor, offset))
                        }
                        allowDeselect={false}
                    />
                    <Select
                        label="Period"
                        data={OFFSET_OPTIONS.map((o) => ({
                            value: String(o),
                            label: capitalize(describeOffset(anchor, o)),
                        }))}
                        value={String(offset)}
                        onChange={(v) =>
                            onChange(serializeAnchorToken(anchor, v ? parseInt(v, 10) : 0))
                        }
                        allowDeselect={false}
                    />
                </Group>
            )}

            {isDateType && dateMode === "relative" && (
                <Group gap="xs" align="flex-end">
                    <NumberInput
                        label="Amount"
                        value={relativeAmount}
                        onChange={(v) => {
                            if (typeof v === "number" && v !== 0) {
                                onChange(serializeLookbackToken(relativeUnit, v));
                            }
                        }}
                        style={{ flex: 1 }}
                    />
                    <Select
                        label="Unit"
                        allowDeselect={false}
                        data={UNIT_OPTIONS}
                        value={relativeUnit}
                        onChange={(v) => {
                            if (v)
                                onChange(
                                    serializeLookbackToken(v as LookbackPrefix, relativeAmount),
                                );
                        }}
                        style={{ flex: 1 }}
                    />
                </Group>
            )}

            {preview && (
                <Text size="xs" c="dimmed">
                    Right now: {formatDateTimeFromDate(preview)}
                </Text>
            )}
        </Stack>
    );
}

function capitalize(text: string): string {
    return text.charAt(0).toUpperCase() + text.slice(1);
}
