import { Button, Center, Modal } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { createElement, ReactNode, useState } from "react";
import { IconType } from "react-icons";
import { useCardLayout } from "../../analytics/components/cardSizing";
import { WidgetShell } from "../../analytics/components/WidgetShell";

interface Props {
    widgetId: string;
    title: string;
    icon: IconType;
    color: string | undefined;
    isConfiguring: boolean;
    flat?: boolean;
    onRemove?: (itemId: string) => void;
    /** Analytic widgets only: an Entries widget has nothing else to edit today. */
    onEdit?: (itemId: string) => void;
    /** Called lazily, only once the modal opens, so a collapsed widget never mounts or
        fetches the thing it stands in for. */
    renderExpanded: () => ReactNode;
}
export function ExpandableWidgetCard({
    widgetId,
    title,
    icon,
    color,
    isConfiguring,
    flat,
    onRemove,
    onEdit,
    renderExpanded,
}: Props) {
    const layout = useCardLayout(true);
    const [opened, setOpened] = useState(false);
    const isMobile = useMediaQuery("(max-width: 48em)");

    return (
        <WidgetShell
            layout={layout}
            fillHeight
            flat={flat}
            isConfiguring={isConfiguring}
            color={color}
            itemId={widgetId}
            onRemove={onRemove}
            onEdit={onEdit}
            title={title}
            compactHeader
            accent
            padding={0}
            bodyProps={{
                justify: "center",
                h: "100%",
                style: { position: "relative" },
            }}
            after={
                opened && (
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
                )
            }
        >
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
        </WidgetShell>
    );
}
