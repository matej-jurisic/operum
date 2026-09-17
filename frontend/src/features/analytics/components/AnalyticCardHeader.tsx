import { ActionIcon, Group, Text } from "@mantine/core";
import { ReactNode } from "react";
import { MdDelete, MdEdit } from "react-icons/md";
import { CardLayout, CARD_HEADER_CLASS } from "./cardSizing";

interface Props {
  title: string;
  layout: CardLayout;
  color: string | undefined;
  isConfiguring: boolean;
  analyticId: string;
  onRemove?: (analyticId: string) => void;
  onEdit?: (analyticId: string) => void;
  titleAdornment?: ReactNode;
  actions?: ReactNode;
  compact?: boolean;
}

export function AnalyticCardHeader({
  title,
  layout,
  color,
  isConfiguring,
  analyticId,
  onRemove,
  onEdit,
  titleAdornment,
  actions,
  compact,
}: Props) {
  // Float edit/remove over the card while arranging so they don't grow the header and shift content.
  const floatControls = isConfiguring && !compact;

  return (
    <Group
      className={CARD_HEADER_CLASS}
      justify={compact ? "center" : "space-between"}
      align={compact ? "center" : "flex-start"}
      wrap="nowrap"
      w="100%"
      h={compact ? "100%" : "auto"}
      gap="xs"
      pos={compact ? "absolute" : floatControls ? "relative" : "inherit"}
      top={compact ? 0 : undefined}
      left={compact ? 0 : undefined}
      right={compact ? layout.padding : undefined}
      style={{
        zIndex: compact && isConfiguring ? 10 : "auto",
        // Disabled here so the empty compact header doesn't block taps to what's under it; icons opt back in below.
        pointerEvents: compact ? "none" : undefined,
      }}
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
            // minWidth: 0 is required for the ellipsis truncation to work with flex: 1.
            style={{ flex: 1, minWidth: 0 }}
            title={title}
          >
            {title}
          </Text>
          {titleAdornment}
        </Group>
      )}
      <Group
        gap="xs"
        wrap="nowrap"
        align="center"
        pos={floatControls ? "absolute" : undefined}
        top={floatControls ? 0 : undefined}
        left={floatControls ? "50%" : undefined}
        style={{
          zIndex: floatControls ? 10 : undefined,
          transform: floatControls ? "translateX(-50%)" : undefined,
          background: floatControls ? "var(--mantine-color-body)" : undefined,
          borderRadius: floatControls
            ? "var(--mantine-radius-sm)"
            : undefined,
        }}
      >
        {actions}
        {isConfiguring && onEdit && (
          <ActionIcon
            size="md"
            color={color}
            variant="outline"
            style={{ pointerEvents: "auto" }}
            onClick={() => onEdit(analyticId)}
          >
            <MdEdit size={18} />
          </ActionIcon>
        )}
        {isConfiguring && onRemove && (
          <ActionIcon
            size="md"
            color={color}
            variant="outline"
            style={{ pointerEvents: "auto" }}
            onClick={() => onRemove(analyticId)}
          >
            <MdDelete size={18} />
          </ActionIcon>
        )}
      </Group>
    </Group>
  );
}
