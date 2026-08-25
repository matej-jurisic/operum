import { ActionIcon, Box, Paper, Stack, Text } from "@mantine/core";
import { useElementSize } from "@mantine/hooks";
import { MdLink } from "react-icons/md";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { SingleValueAnalyticDto } from "../types/AnalyticDto";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import { cardBodyProps, cardShellProps, cardTitle, useCardLayout } from "./cardSizing";

interface Props {
    analytic: SingleValueAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onRename?: (analyticId: string) => void;
    onEntryClick?: (entryId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

// The value is the whole card, so on a dashboard it is set to whatever the room left
// under the header will take rather than to one fixed size: a single number is the one
// analytic that reads as well on two cells as it does on twenty.
const MIN_VALUE_FONT = 18;
const MAX_VALUE_FONT = 72;

export function SingleValueCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onRename,
    onEntryClick,
    fillHeight,
}: Props) {
    const layout = useCardLayout(fillHeight);
    const valueBox = useElementSize<HTMLDivElement>();

    const subtitle = analytic.valueField?.name;

    const valueFontSize =
        fillHeight && valueBox.width > 0 && valueBox.height > 0
            ? Math.max(
                  MIN_VALUE_FONT,
                  Math.min(
                      MAX_VALUE_FONT,
                      // Height caps how tall a line can be; width caps how long a value
                      // can get before it has to wrap to stay inside the card.
                      valueBox.height * 0.55,
                      valueBox.width * 0.22,
                  ),
              )
            : undefined;

    return (
        <Paper
            ref={layout.ref}
            withBorder
            p={layout.padding}
            radius="md"
            w={"100%"}
            {...cardShellProps(fillHeight)}
        >
            <Stack gap="xs" {...cardBodyProps(fillHeight)}>
                <AnalyticCardHeader
                    title={cardTitle(analytic.name, subtitle)}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={analytic.id}
                    onRemove={onRemove}
                    onRename={onRename}
                    actions={
                        analytic.entryId &&
                        onEntryClick && (
                            <ActionIcon
                                color={color}
                                onClick={() => onEntryClick(analytic.entryId!)}
                            >
                                <MdLink size={18} />
                            </ActionIcon>
                        )
                    }
                />
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
                    <Text
                        size={valueFontSize ? undefined : "xl"}
                        fw={600}
                        style={{
                            wordBreak: "break-word",
                            lineHeight: 1.2,
                            fontSize: valueFontSize,
                        }}
                    >
                        {renderValue(analytic.valueField?.type, analytic.value)}
                    </Text>
                </Box>
            </Stack>
        </Paper>
    );
}
