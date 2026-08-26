import { Box, Group, Paper, Stack, Text } from "@mantine/core";
import { RefObject } from "react";
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
import { PortalTooltip } from "./PortalTooltip";

export const getAxisFormatter = (fieldType: string) => {
    if (fieldType === FieldTypes.TimeSpan) return formatMinutesToTime;
    if (fieldType === FieldTypes.Bool) return formatBoolean;
    if (fieldType === FieldTypes.DateTime) return formatDateTime;
    if (fieldType === FieldTypes.Date) return formatDateOnly;
    return (value: any): string => value;
};

export const createTooltipContent = (
    analytic: LineChartAnalyticDto,
    color: string,
    containerRef: RefObject<HTMLElement | null>
) => {
    return ({ payload, label, coordinate }: any) => {
        if (!payload?.[0]) return null;

        const value = payload[0].payload.y;
        const f = getAxisFormatter(analytic.yField.type);

        return (
            <PortalTooltip coordinate={coordinate} containerRef={containerRef}>
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
            </PortalTooltip>
        );
    };
};

export const createDonutTooltipContent = (
    analytic: DonutChartAnaylticDto,
    containerRef: RefObject<HTMLElement | null>
) => {
    return ({ payload, coordinate }: any) => {
        if (!payload?.[0]) return null;
        const name = payload[0].name;
        const value = payload[0].payload.value;
        const color = payload[0].payload.color;
        const f = getAxisFormatter(analytic.valueField.type);

        return (
            <PortalTooltip coordinate={coordinate} containerRef={containerRef}>
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
            </PortalTooltip>
        );
    };
};

export const createBarChartTooltipContent = (
    analytic: BarChartAnalyticDto,
    color: string,
    containerRef: RefObject<HTMLElement | null>
) => {
    return ({ payload, label, coordinate }: any) => {
        if (!payload?.[0]) return null;

        const value = payload[0].payload.value;
        const valueLabel = analytic.valueField?.name ?? "Count";
        const f = analytic.valueField ? getAxisFormatter(analytic.valueField.type) : (v: any) => String(v);

        return (
            <PortalTooltip coordinate={coordinate} containerRef={containerRef}>
                <Paper p="sm" shadow="sm" withBorder>
                    <Text size="sm" c="dimmed" mb="xs">
                        {renderValue(analytic.nameField.type, label)}
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
            </PortalTooltip>
        );
    };
};

export const createComposedTooltipContent = (
    analytic: ComposedChartAnalyticDto,
    containerRef: RefObject<HTMLElement | null>
) => {
    return ({ payload, label, coordinate }: any) => {
        if (!payload?.length) return null;

        // Sources may bucket by different x semantics (dates vs. category names), so this
        // shared axis label is only formatted using the first series' field type — see the
        // `warnings` surfaced on the card for when that's not accurate for every series.
        const xField = analytic.series[0]?.xField;

        return (
            <PortalTooltip coordinate={coordinate} containerRef={containerRef}>
                <Paper p="sm" shadow="sm" withBorder>
                    <Text size="sm" c="dimmed" mb="xs">
                        {xField ? renderValue(xField.type, label) : label}
                    </Text>
                    <Stack gap={4}>
                        {payload.map((entry: any) => {
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
            </PortalTooltip>
        );
    };
};

export const createScatterTooltipContent = (
    analytic: ScatterChartAnalyticDto,
    color: string,
    containerRef: RefObject<HTMLElement | null>
) => {
    return ({ payload, coordinate }: any) => {
        if (!payload?.[0]) return null;

        const dataPoint = payload[0].payload;
        const xValue = dataPoint.x;
        const yValue = dataPoint.y;

        return (
            <PortalTooltip coordinate={coordinate} containerRef={containerRef}>
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
                                {getAxisFormatter(analytic.xField.type)(xValue)}
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
                                {getAxisFormatter(analytic.yField.type)(yValue)}
                            </Text>
                        </Group>
                    </Stack>
                </Paper>
            </PortalTooltip>
        );
    };
};
