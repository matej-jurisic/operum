import { ActionIcon, Group, Text } from "@mantine/core";
import { ReactNode } from "react";
import { MdDelete, MdEdit } from "react-icons/md";
import { CardLayout, CARD_HEADER_CLASS } from "./cardSizing";

interface Props {
  /** What the card is called, from `cardTitle`. Also used as the hover tooltip, for
        whenever the row truncates it with an ellipsis. */
  title: string;
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
  compact?: boolean;
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
  onRename,
  titleAdornment,
  actions,
  compact,
}: Props) {
  if (compact) return null;

  return (
    <Group
      className={CARD_HEADER_CLASS}
      justify={compact ? "center" : "space-between"}
      align={compact ? "center" : "flex-start"}
      wrap="nowrap"
      w="100%"
      h={compact ? "100%" : "auto"}
      gap="xs"
      pos={compact ? "absolute" : "inherit"}
      top={compact ? 0 : undefined}
      left={compact ? 0 : undefined}
      right={compact ? layout.padding : undefined}
      style={{ zIndex: compact ? 2 : undefined }}
      p={0}
    >
      {!compact && (
        <Group
          align="flex-start"
          gap="xs"
          wrap="nowrap"
          miw={0}
          style={{ flex: 1 }}
        >
          <Text
            size="sm"
            truncate="end"
            mb={layout.isCompact ? 0 : "sm"}
            // flex + minWidth: 0 is what lets the ellipsis kick in only once
            // the title has actually claimed the row's full leftover width,
            // rather than being cut at some narrower intrinsic size.
            style={{ flex: 1, minWidth: 0 }}
            title={title}
          >
            {title}
          </Text>
          {titleAdornment}
        </Group>
      )}
      <Group gap="xs" wrap="nowrap" align="center">
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
