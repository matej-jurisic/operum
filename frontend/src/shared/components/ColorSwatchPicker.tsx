import { Group, Stack, Text, UnstyledButton, useMantineTheme } from "@mantine/core";
import { FaCircle } from "react-icons/fa";
import { MdBlock } from "react-icons/md";

// Single source of truth so a color means the same swatch everywhere it's picked.
const colorOptions = [
    "indigo",
    "blue",
    "cyan",
    "grape",
    "green",
    "lime",
    "orange",
    "pink",
    "red",
    "teal",
    "yellow",
    "violet",
];

interface Props {
    label?: string;
    value: string | null | undefined;
    onChange: (color: string | null) => void;
    /** Adds a leading "no color" swatch that clears the value to null (falls back to tracker/dashboard color). */
    allowClear?: boolean;
}

export function ColorSwatchPicker({ label, value, onChange, allowClear }: Props) {
    const theme = useMantineTheme();

    return (
        <Stack gap="xs">
            {label && (
                <Text size="sm" fw={500}>
                    {label}
                </Text>
            )}
            <Group gap="xs">
                {allowClear && (
                    <UnstyledButton
                        onClick={() => onChange(null)}
                        aria-label="Auto"
                        title="Auto"
                        style={{
                            borderRadius: "50%",
                            padding: 2,
                            border: !value
                                ? `2px solid ${theme.colors.gray[6]}`
                                : "2px solid transparent",
                            lineHeight: 0,
                        }}
                    >
                        <MdBlock size={22} color={theme.colors.gray[6]} />
                    </UnstyledButton>
                )}
                {colorOptions.map((c) => (
                    <UnstyledButton
                        key={c}
                        onClick={() => onChange(c)}
                        aria-label={c}
                        title={c}
                        style={{
                            borderRadius: "50%",
                            padding: 2,
                            border:
                                value === c
                                    ? `2px solid ${theme.colors[c]?.[6]}`
                                    : "2px solid transparent",
                            lineHeight: 0,
                        }}
                    >
                        <FaCircle size={22} color={theme.colors[c]?.[6]} />
                    </UnstyledButton>
                ))}
            </Group>
        </Stack>
    );
}
