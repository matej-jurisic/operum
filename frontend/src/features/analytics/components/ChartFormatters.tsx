import { Box, Group, Paper, Stack, Text } from "@mantine/core";
import { FieldTypes } from "../../../shared/constants/DataTypes";
import {
    formatBoolean,
    formatMinutesToTime,
} from "../../../shared/utils/formatters/TypeFormatter";

export const getAxisFormatter = (fieldType: string) => {
    if (fieldType === FieldTypes.TimeSpan) return formatMinutesToTime;
    if (fieldType === FieldTypes.Bool) return formatBoolean;
    return undefined;
};

export const createTooltipContent = (
    fieldType: string,
    fieldName: string,
    color: string
) => {
    return ({ payload, label }: any) => {
        if (!payload?.[0]) return null;

        const value = payload[0].value as number;
        let formatted: string | number = value;

        if (fieldType === FieldTypes.TimeSpan) {
            formatted = formatMinutesToTime(value);
        } else if (fieldType === FieldTypes.Bool) {
            formatted = formatBoolean(value);
        }

        return (
            <Paper p="sm" shadow="sm" withBorder>
                <Text size="sm" c="dimmed" mb="xs">
                    {label}
                </Text>
                <Group gap="xs" wrap="nowrap" maw={300}>
                    <Box
                        w={10}
                        h={10}
                        style={{ borderRadius: "50%" }}
                        bg={color}
                    />
                    <Text size="sm">{fieldName}</Text>
                    <Text size="sm" ml="auto">
                        {formatted}
                    </Text>
                </Group>
            </Paper>
        );
    };
};

export const createScatterTooltipContent = (
    xFieldType: string,
    yFieldType: string,
    xFieldName: string,
    yFieldName: string,
    color: string
) => {
    return ({ payload }: any) => {
        if (!payload?.[0]) return null;

        const dataPoint = payload[0].payload;
        const xValue = dataPoint.x;
        const yValue = dataPoint.y;

        const formatValue = (value: any, fieldType: string) => {
            if (fieldType === FieldTypes.TimeSpan)
                return formatMinutesToTime(value);
            if (fieldType === FieldTypes.Bool) return formatBoolean(value);
            return value;
        };

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
                            {xFieldName}
                        </Text>
                        <Text size="sm" ml="auto">
                            {formatValue(xValue, xFieldType)}
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
                            {yFieldName}
                        </Text>
                        <Text size="sm" ml="auto">
                            {formatValue(yValue, yFieldType)}
                        </Text>
                    </Group>
                </Stack>
            </Paper>
        );
    };
};
