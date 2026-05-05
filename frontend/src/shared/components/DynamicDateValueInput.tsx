import { Group, NumberInput, SegmentedControl, Select, Stack } from "@mantine/core";
import FieldValueInput from "../../features/fields/components/FieldValueInput";
import {
    DynamicDateTokens,
    fixedDynamicDateTokenOptions,
    isDynamicDateToken,
    isParameterizedDateToken,
    parseParameterizedToken,
    ParameterizedDateTokenPrefixes,
    serializeParameterizedToken,
} from "../constants/dynamicDateTokens";

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
    { value: ParameterizedDateTokenPrefixes.LastNHours, label: "Hours" },
    { value: ParameterizedDateTokenPrefixes.LastNDays, label: "Days" },
    { value: ParameterizedDateTokenPrefixes.LastNWeeks, label: "Weeks" },
    { value: ParameterizedDateTokenPrefixes.LastNMonths, label: "Months" },
];

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
    const isRelative = typeof value === "string" && isParameterizedDateToken(value);
    const isNamed = isDynamicDateToken(value) && !isRelative;
    const parsed = isRelative ? parseParameterizedToken(value as string) : null;

    const dateMode: DateMode = isRelative ? "relative" : isNamed ? "named" : "date";
    const relativeUnit = parsed?.prefix ?? ParameterizedDateTokenPrefixes.LastNDays;
    const relativeAmount = parsed?.n ?? 7;

    const handleModeChange = (v: string) => {
        if (v === dateMode) return;
        if (v === "named") onChange(DynamicDateTokens.Today);
        else if (v === "relative") onChange(serializeParameterizedToken(ParameterizedDateTokenPrefixes.LastNDays, 7));
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
                <Select
                    label={label ?? "Value"}
                    placeholder="Select named date"
                    data={fixedDynamicDateTokenOptions}
                    value={typeof value === "string" ? value : null}
                    onChange={(v) => onChange(v ?? DynamicDateTokens.Today)}
                    allowDeselect={false}
                />
            )}

            {isDateType && dateMode === "relative" && (
                <Group gap="xs" align="flex-end">
                    <NumberInput
                        label="Amount"
                        value={relativeAmount}
                        onChange={(v) => {
                            if (typeof v === "number" && v !== 0) {
                                onChange(serializeParameterizedToken(relativeUnit, v));
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
                            if (v) onChange(serializeParameterizedToken(v as any, relativeAmount));
                        }}
                        style={{ flex: 1 }}
                    />
                </Group>
            )}
        </Stack>
    );
}
