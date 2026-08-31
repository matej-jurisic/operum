import { Button, Center, Modal, Paper, Stack } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { createElement, ReactNode, useState } from "react";
import { IconType } from "react-icons";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";

interface Props {
    widgetId: string;
    title: string;
    icon: IconType;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Analytic widgets only: an Entries widget has nothing else to edit today. */
    onEdit?: (itemId: string) => void;
    /** The widget's own full-size card. Called lazily, only once the modal is actually
        opened, so a collapsed widget never mounts (and never fetches) the thing it stands
        in for until someone actually asks to see it. */
    renderExpanded: () => ReactNode;
}

/**
 * Wraps an Analytic or Entries widget marked expandable on the grid it's currently being
 * drawn on: instead of the chart or table itself, the card is nothing but a button
 * carrying the widget's name, and pressing it opens the real thing at full size in a
 * modal with a plain close button.
 *
 * Set once from the widget's own create/edit form (see WidgetLibraryModal's forms and
 * EditWidgetModal) — never by arranging the board. The button never grows or resizes in
 * place, so opening it can never disturb anything else on the grid.
 */
export function ExpandableWidgetCard({
    widgetId,
    title,
    icon,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    renderExpanded,
}: Props) {
    const layout = useCardLayout(true);
    const [opened, setOpened] = useState(false);
    // Full screen is what actually gives the widget the room it has none of on the grid
    // it was collapsed on; a normal centered modal is plenty on a screen wide enough to
    // have made the widget expandable a deliberate choice rather than a necessity.
    const isMobile = useMediaQuery("(max-width: 48em)");

    return (
        <Paper
            ref={layout.ref}
            withBorder={isConfiguring}
            p={0}
            radius="md"
            w="100%"
            {...cardShellProps(true)}
        >
            <Stack
                gap="xs"
                justify="center"
                {...cardBodyProps(true)}
                h="100%"
                style={{ position: "relative" }}
            >
                <AnalyticCardHeader
                    title={title}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={widgetId}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    compact
                />
                <Center style={{ flex: 1, minHeight: 0, zIndex: 1 }}>
                    <Button
                        color={color}
                        disabled={isConfiguring}
                        variant="light"
                        radius="md"
                        w="100%"
                        h="100%"
                        style={{
                            pointerEvents: isConfiguring ? "none" : "all",
                        }}
                        leftSection={createElement(icon, { size: 18 })}
                        onClick={() => setOpened(true)}
                    >
                        {title}
                    </Button>
                </Center>
            </Stack>

            {opened && (
                <Modal
                    opened
                    onClose={() => setOpened(false)}
                    title={title}
                    size="xl"
                    fullScreen={isMobile}
                    centered={!isMobile}
                >
                    {renderExpanded()}
                </Modal>
            )}
        </Paper>
    );
}
