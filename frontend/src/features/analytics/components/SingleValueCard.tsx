import { ActionIcon, Box, Stack, Text } from "@mantine/core";
import { MdLink } from "react-icons/md";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { SingleValueAnalyticDto } from "../types/AnalyticDto";
import { useCardLayout, useSyncedElementSize } from "./cardSizing";
import { TrendSparkline } from "./TrendSparkline";
import { WidgetShell } from "./WidgetShell";

interface Props {
    analytic: SingleValueAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    onEntryClick?: (entryId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

const MIN_VALUE_FONT = 18;
const MAX_VALUE_FONT = 60;

export function SingleValueCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    onEntryClick,
    fillHeight,
}: Props) {
    const layout = useCardLayout(fillHeight);
    const valueBox = useSyncedElementSize<HTMLDivElement>(!!fillHeight);

    const valueFontSize =
        fillHeight && valueBox.width > 0 && valueBox.height > 0
            ? Math.max(
                  MIN_VALUE_FONT,
                  Math.min(
                      MAX_VALUE_FONT,
                      valueBox.height * 0.55,
                      valueBox.width * 0.22,
                  ),
              )
            : undefined;

    const secondaryFontSize = valueFontSize
        ? Math.max(12, Math.min(22, valueFontSize * 0.4))
        : undefined;

    return (
        <WidgetShell
            layout={layout}
            fillHeight={fillHeight}
            isConfiguring={isConfiguring}
            color={color}
            itemId={analytic.id}
            onRemove={onRemove}
            onEdit={onEdit}
            title={analytic.name}
            headerActions={
                analytic.entryId &&
                onEntryClick && (
                    <ActionIcon
                        variant="outline"
                        color={color}
                        onClick={() => onEntryClick(analytic.entryId!)}
                    >
                        <MdLink size={18} />
                    </ActionIcon>
                )
            }
        >
            <Box
                ref={valueBox.ref}
                style={
                    fillHeight
                        ? {
                              flex: 1,
                              minHeight: 0,
                              display: "flex",
                              alignItems: "center",
                              overflow: "hidden",
                          }
                        : undefined
                }
            >
                <Stack w={"100%"} h={"100%"} gap={4} justify="space-evenly">
                    <Text
                        size={valueFontSize ? undefined : "xl"}
                        fw={600}
                        style={{
                            wordBreak: "break-word",
                            lineHeight: 1.2,
                            textAlign: "center",
                            fontSize: valueFontSize,
                        }}
                    >
                        {renderValue(analytic.valueField?.type, analytic.value)}
                    </Text>
                    {analytic.secondaryValue && (
                        <Text
                            size={secondaryFontSize ? undefined : "sm"}
                            c="dimmed"
                            style={{
                                wordBreak: "break-word",
                                lineHeight: 1.2,
                                fontSize: secondaryFontSize,
                            }}
                        >
                            {analytic.secondaryValueField?.name
                                ? `${analytic.secondaryValueField.name}: `
                                : ""}
                            {renderValue(
                                analytic.secondaryValueField?.type,
                                analytic.secondaryValue,
                            )}
                        </Text>
                    )}
                    {analytic.trend && (
                        <TrendSparkline
                            trend={analytic.trend}
                            currentValue={analytic.value}
                            valueFieldType={analytic.valueField?.type}
                            color={color}
                            direction="neutral"
                        />
                    )}
                </Stack>
            </Box>
        </WidgetShell>
    );
}
