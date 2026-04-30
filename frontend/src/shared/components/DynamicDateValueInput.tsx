import { Group, NumberInput, SegmentedControl, Select } from "@mantine/core";
import FieldValueInput from "../../features/fields/components/FieldValueInput";
import {
    DynamicDateTokens,
    dynamicDateTokenOptions,
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

export default function DynamicDateValueInput({
    isDateType,
    value,
    onChange,
    field,
    form,
    fieldPath,
    label,
}: Props) {
    const isDynamic = isDynamicDateToken(value);
    const isParam = typeof value === "string" && isParameterizedDateToken(value);
    const parsed = isParam && typeof value === "string" ? parseParameterizedToken(value) : null;

    const handleTokenTypeChange = (v: string | null) => {
        if (!v) return;
        const isParamPrefix = (Object.values(ParameterizedDateTokenPrefixes) as string[]).includes(v);
        if (isParamPrefix) {
            onChange(serializeParameterizedToken(v as any, parsed?.n ?? 7));
        } else {
            onChange(v);
        }
    };

    const handleNChange = (v: number | string) => {
        if (typeof v === "number" && v > 0 && parsed) {
            onChange(serializeParameterizedToken(parsed.prefix, v));
        }
    };

    return (
        <Group flex={1} align="flex-end" gap="xs">
            {isDateType && (
                <SegmentedControl
                    size="xs"
                    data={[
                        { value: "fixed", label: "Fixed" },
                        { value: "dynamic", label: "Dynamic" },
                    ]}
                    value={isDynamic ? "dynamic" : "fixed"}
                    onChange={(v) =>
                        onChange(v === "dynamic" ? DynamicDateTokens.Today : undefined)
                    }
                />
            )}

            {isDateType && isDynamic ? (
                <>
                    <Select
                        flex={1}
                        label={label ?? "Value"}
                        placeholder="Select dynamic value"
                        data={dynamicDateTokenOptions}
                        value={isParam ? (parsed?.prefix ?? null) : (typeof value === "string" ? value : null)}
                        onChange={handleTokenTypeChange}
                        allowDeselect={false}
                    />
                    {isParam && (
                        <NumberInput
                            w={80}
                            label="N"
                            min={1}
                            value={parsed?.n ?? 1}
                            onChange={handleNChange}
                        />
                    )}
                </>
            ) : (
                <FieldValueInput
                    field={field}
                    form={form}
                    fieldPath={fieldPath}
                    styles={{ flex: 1 }}
                />
            )}
        </Group>
    );
}
