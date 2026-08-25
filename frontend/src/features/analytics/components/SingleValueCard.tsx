import { ActionIcon, Box, Group, Paper, Stack, Text } from "@mantine/core";
import { useElementSize } from "@mantine/hooks";
import { MdDelete, MdLink } from "react-icons/md";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { SingleValueAnalyticDto } from "../types/AnalyticDto";
import {
    cardBodyProps,
    CARD_HEADER_CLASS,
    cardShellProps,
    cardTitle,
    useCardLayout,
} from "./cardSizing";

interface Props {
    analytic: SingleValueAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEntryClick?: (entryId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

// The value is the whole card, so on a dashboard it is set to whatever the room left
// under the header will take rather than to one fixed size: a single number is the one
// analytic that reads as well on two cells as it does on twenty.
const MIN_VALUE_FONT = 18;
const MAX_VALUE_FONT = 72;

// The value shrinks to whatever the name leaves over, so this card can spend more of its
// height on a long name than a chart card can: the alternative is an ellipsis on a card
// that is mostly empty anyway.
const MAX_TITLE_LINES = 3;

export function SingleValueCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEntryClick,
    fillHeight,
}: Props) {
    const layout = useCardLayout(fillHeight, MAX_TITLE_LINES);
    const valueBox = useElementSize<HTMLDivElement>();

    const subtitle = analytic.valueField?.name;
    const fullTitle = subtitle ? `${analytic.name}: ${subtitle}` : analytic.name;

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
                <Group
                    className={CARD_HEADER_CLASS}
                    justify="space-between"
                    align="center"
                    mih={28}
                    wrap="nowrap"
                >
                    <Text
                        size={layout.titleSize}
                        c="dimmed"
                        fw={500}
                        lineClamp={layout.titleLineClamp}
                        // A clamped Text is a -webkit-box, which a flex row will not
                        // hand its leftover width to on its own: without this the name
                        // is cut at the width of the icons beside it and the rest of the
                        // row sits empty.
                        style={{ flex: 1, minWidth: 0 }}
                        title={fullTitle}
                    >
                        {cardTitle(layout, analytic.name, subtitle)}
                    </Text>
                    <Group gap="xs" wrap="nowrap">
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
