import { ActionIcon, Group, Paper, Stack, Text } from "@mantine/core";
import { MdDelete, MdLink } from "react-icons/md";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { cardBodyProps, cardShellProps } from "./cardSizing";
import { SingleValueAnalyticDto } from "../types/AnalyticDto";

interface Props {
    analytic: SingleValueAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEntryClick?: (entryId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

export function SingleValueCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEntryClick,
    fillHeight,
}: Props) {
    return (
        <Paper
            withBorder
            p="md"
            radius="md"
            w={"100%"}
            {...cardShellProps(fillHeight)}
        >
            <Stack gap="xs" {...cardBodyProps(fillHeight)}>
                <Group
                    justify="space-between"
                    align="center"
                    mih={28}
                    wrap="nowrap"
                >
                    <Group>
                        <Text size="sm" c="dimmed" fw={500}>
                            {analytic.valueField
                                ? `${analytic.name}: ${analytic.valueField.name}`
                                : analytic.name}
                        </Text>
                    </Group>
                    <Group>
                        {analytic.entryId && onEntryClick && (
                            <ActionIcon
                                color={color}
                                onClick={() => onEntryClick(analytic.entryId!)}
                            >
                                <MdLink size={18} />
                            </ActionIcon>
                        )}
                        {isConfiguring && onRemove && (
                            <ActionIcon
                                size="md"
                                color={color}
                                variant="outline"
                                onClick={() => onRemove(analytic.id)}
                            >
                                <MdDelete size={18} />
                            </ActionIcon>
                        )}
                    </Group>
                </Group>
                <Text
                    size="xl"
                    fw={600}
                    style={{ wordBreak: "break-word", lineHeight: 1.2 }}
                >
                    {renderValue(analytic.valueField?.type, analytic.value)}
                </Text>
            </Stack>
        </Paper>
    );
}
