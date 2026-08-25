import { ActionIcon, Group, Text } from "@mantine/core";
import { ReactNode } from "react";
import { MdDelete, MdEdit } from "react-icons/md";
import { CardLayout, CARD_HEADER_CLASS } from "./cardSizing";

interface Props {
    /** What the card is called, from `cardTitle`. */
    title: string;
    /** The whole of it, for the card that had to shorten what it shows. */
    fullTitle?: string;
    layout: CardLayout;
    color: string | undefined;
    isConfiguring: boolean;
    analyticId: string;
    onRemove?: (analyticId: string) => void;
    /** Opens the rename dialog for the analytic this card was built from. */
    onRename?: (analyticId: string) => void;
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
    fullTitle,
    layout,
    color,
    isConfiguring,
    analyticId,
    onRemove,
    onRename,
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
            <Group
                align="flex-start"
                gap="xs"
                wrap="nowrap"
                miw={0}
                style={{ flex: 1 }}
            >
                <Text
                    size={layout.titleSize}
                    lineClamp={layout.titleLineClamp}
                    mb={layout.isCompact ? 0 : "sm"}
                    // A clamped Text is a -webkit-box, which a flex row will not hand
                    // its leftover width to on its own: without this the name is cut at
                    // the width of the row's other children and the rest sits empty.
                    style={{ flex: 1, minWidth: 0 }}
                    title={fullTitle ?? title}
                >
                    {title}
                </Text>
                {titleAdornment}
            </Group>
            <Group gap="xs" wrap="nowrap">
                {actions}
                {isConfiguring && onRename && (
                    <ActionIcon
                        size="md"
                        color={color}
                        variant="outline"
                        onClick={() => onRename(analyticId)}
                    >
                        <MdEdit size={18} />
                    </ActionIcon>
                )}
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
