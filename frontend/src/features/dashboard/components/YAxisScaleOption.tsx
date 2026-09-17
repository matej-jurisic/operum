import { Checkbox, Stack, Text } from "@mantine/core";

interface Props {
    yAxisFromZero: boolean;
    onChange: (value: boolean) => void;
}

export function YAxisScaleOption({ yAxisFromZero, onChange }: Props) {
    return (
        <Stack gap="xs">
            <Text size="sm" c="dimmed">
                Y axis scaling
            </Text>
            <Checkbox
                label="Start the Y axis at zero"
                description="Uncheck to fit the axis to the data's range instead, useful when values stay within a narrow band far from zero."
                checked={yAxisFromZero}
                onChange={(event) => onChange(event.currentTarget.checked)}
            />
        </Stack>
    );
}
