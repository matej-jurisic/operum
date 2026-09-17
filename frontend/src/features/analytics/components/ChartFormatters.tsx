import { Box, Group, Paper, Stack, Text } from "@mantine/core";
import { FieldTypes } from "../../../shared/constants/DataTypes";
import {
    formatBoolean,
    formatDateOnly,
    formatDateTime,
    formatMinutesToTime,
} from "../../../shared/utils/formatters/TypeFormatter";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import {
    BarChartAnalyticDto,
    ComposedChartAnalyticDto,
    DonutChartAnaylticDto,
    LineChartAnalyticDto,
    ScatterChartAnalyticDto,
} from "../types/AnalyticDto";

type AxisFormatter = (value: string | number | null | undefined) => string;

// Recharts' tooltip `content` payload shape varies by chart type; kept loose rather than cast per builder.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type TooltipRenderProps = any;

export const getAxisFormatter = (
    fieldType: string | undefined,
): AxisFormatter => {
    if (fieldType === FieldTypes.TimeSpan)
        return formatMinutesToTime as AxisFormatter;
    if (fieldType === FieldTypes.Bool) return formatBoolean as AxisFormatter;
    if (fieldType === FieldTypes.DateTime)
        return formatDateTime as AxisFormatter;
    if (fieldType === FieldTypes.Date) return formatDateOnly as AxisFormatter;
    return (value): string => String(value ?? "");
};

export const createTooltipContent = (
    analytic: LineChartAnalyticDto,
    color: string
) => {
    return ({ payload, label }: TooltipRenderProps) => {
        if (!payload?.[0]) return null;

        const value = payload[0].payload.y;
        const f = getAxisFormatter(analytic.yField?.type);

        return (
            <Paper p="sm" shadow="sm" withBorder>
                <Text size="sm" c="dimmed" mb="xs">
                    {renderValue(analytic.xField?.type, label)}
                </Text>
                <Group gap="xs" wrap="nowrap" maw={300}>
                    <Box
                        w={10}
                        h={10}
                        style={{ borderRadius: "50%" }}
                        bg={color}
                    />
                    <Text size="sm">{analytic.yField?.name}</Text>
                    <Text size="sm" ml="auto">
                        {f ? f(value) : ""}
                    </Text>
                </Group>
            </Paper>
        );
    };
};

export const createDonutTooltipContent = (analytic: DonutChartAnaylticDto) => {
    return ({ payload }: TooltipRenderProps) => {
        if (!payload?.[0]) return null;
        const name = payload[0].name;
        const value = payload[0].payload.value;
        const color = payload[0].payload.color;
        const f = getAxisFormatter(analytic.valueField.type);

        return (
            <Paper p="sm" shadow="sm" withBorder>
                <Text size="sm" c="dimmed" mb="xs">
                    {renderValue(analytic.nameField.type, name)}
                </Text>
                <Group gap="xs" wrap="nowrap" maw={300}>
                    <Box
                        w={10}
                        h={10}
                        style={{ borderRadius: "50%" }}
                        bg={color}
                    />
                    <Text size="sm">{analytic.valueField.name}</Text>
                    <Text size="sm" ml="auto">
                        {f ? f(value) : ""}
                    </Text>
                </Group>
            </Paper>
        );
    };
};

export const createBarChartTooltipContent = (
    analytic: BarChartAnalyticDto,
    color: string
) => {
    return ({ payload, label }: TooltipRenderProps) => {
        if (!payload?.[0]) return null;

        const value = payload[0].payload.value;
        const valueLabel = analytic.valueField?.name ?? "Count";
        const f: AxisFormatter = analytic.valueField
            ? getAxisFormatter(analytic.valueField.type)
            : (v) => String(v ?? "");

        return (
            <Paper p="sm" shadow="sm" withBorder>
                <Text size="sm" c="dimmed" mb="xs">
                    {renderValue(analytic.nameField?.type, label)}
                </Text>
                <Group gap="xs" wrap="nowrap" maw={300}>
                    <Box
                        w={10}
                        h={10}
                        style={{ borderRadius: "50%" }}
                        bg={color}
                    />
                    <Text size="sm">{valueLabel}</Text>
                    <Text size="sm" ml="auto">
                        {f(value)}
                    </Text>
                </Group>
            </Paper>
        );
    };
};

export const createComposedTooltipContent = (analytic: ComposedChartAnalyticDto) => {
    return ({ payload, label }: TooltipRenderProps) => {
        if (!payload?.length) return null;

        // Formatted using only the first series' field type; series may use different x semantics (see `warnings`).
        const xField = analytic.series[0]?.xField;

        return (
            <Paper p="sm" shadow="sm" withBorder>
                <Text size="sm" c="dimmed" mb="xs">
                    {xField ? renderValue(xField.type, label) : label}
                </Text>
                <Stack gap={4}>
                    {payload.map((entry: TooltipRenderProps) => {
                        const series = analytic.series.find((s) => s.key === entry.dataKey);
                        if (!series || entry.value == null) return null;
                        const f = getAxisFormatter(series.valueField.type);

                        return (
                            <Group key={entry.dataKey} gap="xs" wrap="nowrap" maw={300}>
                                <Box
                                    w={10}
                                    h={10}
                                    style={{ borderRadius: "50%" }}
                                    bg={entry.color}
                                />
                                <Text size="sm">{series.label}</Text>
                                <Text size="sm" ml="auto">
                                    {f ? f(entry.value) : entry.value}
                                </Text>
                            </Group>
                        );
                    })}
                </Stack>
            </Paper>
        );
    };
};

export const createScatterTooltipContent = (
    analytic: ScatterChartAnalyticDto,
    color: string
) => {
    return ({ payload }: TooltipRenderProps) => {
        if (!payload?.[0]) return null;

        const dataPoint = payload[0].payload;
        const xValue = dataPoint.x;
        const yValue = dataPoint.y;

        return (
            <Paper p="sm" shadow="sm" withBorder>
                <Stack gap="xs">
                    <Group gap="xs" wrap="nowrap" maw={300}>
                        <Box
                            w={10}
                            h={10}
                            style={{ borderRadius: "50%" }}
                            bg={color}
                        />
                        <Text size="sm" fw={500}>
                            {analytic.xField?.name}
                        </Text>
                        <Text size="sm" ml="auto">
                            {getAxisFormatter(analytic.xField?.type)(xValue)}
                        </Text>
                    </Group>
                    <Group gap="xs" wrap="nowrap" maw={300}>
                        <Box
                            w={10}
                            h={10}
                            style={{ borderRadius: "50%" }}
                            bg={color}
                        />
                        <Text size="sm" fw={500}>
                            {analytic.yField?.name}
                        </Text>
                        <Text size="sm" ml="auto">
                            {getAxisFormatter(analytic.yField?.type)(yValue)}
                        </Text>
                    </Group>
                </Stack>
            </Paper>
        );
    };
};
