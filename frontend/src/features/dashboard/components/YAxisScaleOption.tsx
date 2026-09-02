import { Checkbox, Stack, Text } from "@mantine/core";

interface Props {
    yAxisFromZero: boolean;
    onChange: (value: boolean) => void;
}

/**
 * The line-chart-only display option offered by every Analytic widget's create/edit form:
 * whether the Y axis is pinned to zero (the default, good for "how much" comparisons) or
 * fitted to the data's own range (good for a series that only ever moves within a narrow
 * band well above zero, which would otherwise draw as a flat line at the top). Shared the
 * same way ExpandableOptionFields is, since CustomAnalyticForm, PlaceFromLibraryForm and
 * EditWidgetModal only agree on this part.
 */
export function YAxisScaleOption({ yAxisFromZero, onChange }: Props) {
    return (
        <Stack gap="xs">
            <Text size="sm" c="dimmed">
                Y axis scaling
            </Text>
            <Checkbox
                label="Start the Y axis at zero"
                description="Uncheck to fit the axis to the data's range instead — useful when values stay within a narrow band far from zero."
                checked={yAxisFromZero}
                onChange={(event) => onChange(event.currentTarget.checked)}
            />
        </Stack>
    );
}
