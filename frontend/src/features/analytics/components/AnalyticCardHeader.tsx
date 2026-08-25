import { ActionIcon, Group, Text } from "@mantine/core";
import { ReactNode } from "react";
import { MdDelete } from "react-icons/md";
import { CardLayout, CARD_HEADER_CLASS } from "./cardSizing";

interface Props {
    /** The analytic's name and the fields it was built from. */
    title: string;
    layout: CardLayout;
    color: string | undefined;
    isConfiguring: boolean;
    analyticId: string;
    onRemove?: (analyticId: string) => void;
    /** Sits next to the title, for anything that qualifies it. */
    titleAdornment?: ReactNode;
    /** Sits with the remove button, for anything else the card can be acted on with. */
    actions?: ReactNode;
}

/**
 * The row every card leads with. It carries the class the dashboard grid indents to
 * clear its drag handle, and takes its type and spacing from the card's measured size:
 * on a widget dragged down to a few cells the header gives its room up to the chart.
 */
export function AnalyticCardHeader({
    title,
    layout,
    color,
    isConfiguring,
    analyticId,
    onRemove,
    titleAdornment,
    actions,
}: Props) {
    return (
        <Group
            className={CARD_HEADER_CLASS}
            justify="space-between"
            wrap="nowrap"
            align="flex-start"
            gap="xs"
        >
            <Group align="flex-start" gap="xs" wrap="nowrap" miw={0}>
                <Text
                    size={layout.titleSize}
                    lineClamp={layout.titleLineClamp}
                    mb={layout.isCompact ? 0 : "sm"}
                    style={{ minWidth: 0 }}
                >
                    {title}
                </Text>
                {titleAdornment}
            </Group>
            <Group gap="xs" wrap="nowrap">
                {actions}
                {isConfiguring && onRemove && (
                    <ActionIcon
                        size="md"
                        color={color}
                        variant="outline"
                        onClick={() => onRemove(analyticId)}
                    >
                        <MdDelete size={18} />
                    </ActionIcon>
                )}
            </Group>
        </Group>
    );
}
