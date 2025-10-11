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
    DonutChartAnaylticDto,
    LineChartAnalyticDto,
    ScatterChartAnalyticDto,
} from "../types/AnalyticDto";

export const getAxisFormatter = (fieldType: string) => {
    if (fieldType === FieldTypes.TimeSpan) return formatMinutesToTime;
    if (fieldType === FieldTypes.Bool) return formatBoolean;
    if (fieldType === FieldTypes.DateTime) return formatDateTime;
    if (fieldType === FieldTypes.Date) return formatDateOnly;
    return (value: any): string => value;
};

export const createTooltipContent = (
    analytic: LineChartAnalyticDto,
    color: string
) => {
    return ({ payload, label }: any) => {
        if (!payload?.[0]) return null;

        const value = payload[0].payload.y;
        const f = getAxisFormatter(analytic.yField.type);

        return (
            <Paper p="sm" shadow="sm" withBorder>
                <Text size="sm" c="dimmed" mb="xs">
                    {renderValue(analytic.xField.type, label)}
                </Text>
                <Group gap="xs" wrap="nowrap" maw={300}>
                    <Box
                        w={10}
                        h={10}
                        style={{ borderRadius: "50%" }}
                        bg={color}
                    />
                    <Text size="sm">{analytic.yField.name}</Text>
                    <Text size="sm" ml="auto">
                        {f ? f(value) : ""}
                    </Text>
                </Group>
            </Paper>
        );
    };
};

export const createDonutTooltipContent = (analytic: DonutChartAnaylticDto) => {
    return ({ payload }: any) => {
        if (!payload?.[0]) return null;
        const name = payload[0].name;
        const value = payload[0].payload.value;
        const color = payload[0].payload.color;
        const f = getAxisFormatter(analytic.nameField.type);

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

export const createScatterTooltipContent = (
    analytic: ScatterChartAnalyticDto,
    color: string
) => {
    return ({ payload }: any) => {
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
                            {analytic.xField.name}
                        </Text>
                        <Text size="sm" ml="auto">
                            {renderValue(analytic.xField.type, xValue)}
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
                            {analytic.yField.name}
                        </Text>
                        <Text size="sm" ml="auto">
                            {renderValue(analytic.yField.type, yValue)}
                        </Text>
                    </Group>
                </Stack>
            </Paper>
        );
    };
};
