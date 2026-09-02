import { Paper, PaperProps, Stack, StackProps } from "@mantine/core";
import { ReactNode } from "react";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import { CardLayout, cardBodyProps, cardShellProps } from "./cardSizing";
import "./WidgetShell.css";

interface Props {
    /** From `useCardLayout` — its ref is attached to the outer Paper, so every card
        measures the same box it always did. */
    layout: CardLayout;
    /** Dashboard grid cell (`true`) vs. the fixed-height masonry on a tracker page.
        Also what switches on the borderless read mode: a widget being read on a board
        has no border, only a shadow (or, in dark mode, a lifted surface). */
    fillHeight?: boolean;
    isConfiguring: boolean;
    color: string | undefined;
    /** The analytic or widget id, passed to the header's edit/remove controls. */
    itemId: string;
    onRemove?: (id: string) => void;
    onEdit?: (id: string) => void;

    /** The header row's text. Omit to render no header at all (a Divider draws its own). */
    title?: string;
    /** Collapse the header to a centered arrange-mode overlay carrying only its icons,
        for widgets whose body is the whole card (Header, Divider, Quick add, shortcuts). */
    compactHeader?: boolean;
    headerActions?: ReactNode;
    titleAdornment?: ReactNode;

    /** A layout accent (Divider, Header, shortcut) rather than data: no surface at all
        while the board is read, a border only while it is arranged. */
    accent?: boolean;
    /** Outer padding. Defaults to `layout.padding`; pass 0 for cards that pad their own
        body instead. */
    padding?: PaperProps["p"];
    /** Merged onto the inner content Stack (e.g. `justify`, `h`, a tighter `gap`). */
    bodyProps?: StackProps;
    children: ReactNode;
    /** Rendered inside the Paper but outside the content Stack, for a card's own modals
        and dialogs. */
    after?: ReactNode;
}

/**
 * The shell every widget card is drawn in: the outer Paper, the content Stack, and the
 * shared header row. Pulled out of the ~13 cards that each repeated it so the board's
 * chrome, most of all whether a widget shows a border, is decided in one place.
 */
export function WidgetShell({
    layout,
    fillHeight,
    isConfiguring,
    color,
    itemId,
    onRemove,
    onEdit,
    title,
    compactHeader,
    headerActions,
    titleAdornment,
    accent,
    padding,
    bodyProps,
    children,
    after,
}: Props) {
    const readMode = !!fillHeight && !isConfiguring;
    // An accent (Header, Divider, shortcut) sheds its surface entirely while the board
    // is read; a data widget keeps a lifted panel, drawn by WidgetShell.css off the
    // data-read-mode flag rather than Mantine's plain border.
    const softPanel = readMode && !accent;

    return (
        <Paper
            ref={layout.ref}
            className="widget-shell"
            data-read-mode={softPanel || undefined}
            withBorder={!readMode}
            bg={
                accent && readMode
                    ? "transparent"
                    : softPanel
                      ? "var(--widget-surface)"
                      : undefined
            }
            p={padding ?? layout.padding}
            radius="md"
            w="100%"
            {...cardShellProps(fillHeight)}
        >
            <Stack gap="xs" {...cardBodyProps(fillHeight)} {...bodyProps}>
                {title !== undefined && (
                    <AnalyticCardHeader
                        title={title}
                        layout={layout}
                        color={color}
                        isConfiguring={isConfiguring}
                        analyticId={itemId}
                        onRemove={onRemove}
                        onEdit={onEdit}
                        actions={headerActions}
                        titleAdornment={titleAdornment}
                        compact={compactHeader}
                    />
                )}
                {children}
            </Stack>
            {after}
        </Paper>
    );
}
