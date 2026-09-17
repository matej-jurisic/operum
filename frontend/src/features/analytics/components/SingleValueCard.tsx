import { ActionIcon, Box, Stack, Text, em } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
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
const MAX_VALUE_FONT_COMPACT = 32;
// Average glyph width for the bold value text, as a fraction of its font size; bounds the
// font so long values (e.g. "08:00:00") can't grow past the box and overflow.
const CHAR_WIDTH_RATIO = 0.62;

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
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const compact = isMobile || layout.isCompact;
    const valueBox = useSyncedElementSize<HTMLDivElement>(!!fillHeight);

    const renderedValue = renderValue(analytic.valueField?.type, analytic.value);
    const valueLength = String(renderedValue).length;

    const maxValueFont = compact ? MAX_VALUE_FONT_COMPACT : MAX_VALUE_FONT;
    const valueFontSize =
        fillHeight && valueBox.width > 0 && valueBox.height > 0
            ? Math.max(
                  MIN_VALUE_FONT,
                  Math.min(
                      maxValueFont,
                      valueBox.height * (compact ? 0.4 : 0.55),
                      valueBox.width * (compact ? 0.16 : 0.22),
                      valueLength > 0
                          ? valueBox.width / (valueLength * CHAR_WIDTH_RATIO)
                          : maxValueFont,
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
                <Stack w={"100%"} h={"100%"} gap={compact ? 6 : 10} justify="center">
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
                        {renderedValue}
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
                            compact={compact}
                        />
                    )}
                </Stack>
            </Box>
        </WidgetShell>
    );
}
