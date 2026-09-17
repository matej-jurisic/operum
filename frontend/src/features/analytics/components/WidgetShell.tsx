import { Paper, PaperProps, Stack, StackProps } from "@mantine/core";
import { ReactNode } from "react";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import { CardLayout, cardBodyProps, cardShellProps } from "./cardSizing";
import "./WidgetShell.css";

interface Props {
    layout: CardLayout;
    /** True for a dashboard grid cell, false for the fixed-height tracker masonry; also toggles the borderless read mode. */
    fillHeight?: boolean;
    isConfiguring: boolean;
    color: string | undefined;
    itemId: string;
    onRemove?: (id: string) => void;
    onEdit?: (id: string) => void;

    /** Omit to render no header at all (a Divider draws its own). */
    title?: string;
    /** Collapses the header to a centered arrange-mode overlay carrying only its icons. */
    compactHeader?: boolean;
    headerActions?: ReactNode;
    titleAdornment?: ReactNode;

    /** For a layout accent (Divider, Header, shortcut) rather than data: no surface while read, a border only while arranged. */
    accent?: boolean;
    /** Defaults to `layout.padding`; pass 0 for cards that pad their own body instead. */
    padding?: PaperProps["p"];
    bodyProps?: StackProps;
    children: ReactNode;
    /** Rendered inside the Paper but outside the content Stack, for a card's own modals. */
    after?: ReactNode;
}

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
    // A data widget keeps a lifted panel in read mode (drawn by WidgetShell.css via data-read-mode); an accent sheds its surface entirely.
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
