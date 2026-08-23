import {
    Button,
    Group,
    ScrollArea,
    Tooltip,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { createElement } from "react";
import { FiPlus } from "react-icons/fi";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { DashboardDto } from "../types/DashboardDto";

interface Props {
    boards: DashboardDto[];
    activeBoardId: string;
    onSelect: (boardId: string) => void;
    onCreate: () => void;
}

export default function BoardSwitcher({
    boards,
    activeBoardId,
    onSelect,
    onCreate,
}: Props) {
    const theme = useMantineTheme();
    const isMobile = useMediaQuery("(max-width: 48em)");

    return (
        <ScrollArea
            type="auto"
            scrollbarSize={6}
            offsetScrollbars="x"
            style={{ minWidth: 0 }}
            flex={1}
        >
            <Group gap="xs" wrap="nowrap">
                {boards.map((board) => {
                    const color =
                        board.color && board.color in theme.colors
                            ? board.color
                            : "indigo";
                    const isActive = board.id === activeBoardId;
                    // On mobile, only the active board keeps its label so more
                    // pills fit on screen at once; the rest collapse to icons.
                    const showLabel = !isMobile || isActive;

                    return (
                        <Tooltip
                            key={board.id}
                            label={board.name}
                            withArrow
                            disabled={showLabel}
                        >
                            <Button
                                size="sm"
                                radius="xl"
                                color={color}
                                variant={isActive ? "filled" : "outline"}
                                onClick={() => onSelect(board.id)}
                                px={showLabel ? undefined : "xs"}
                                leftSection={createElement(
                                    resolveTrackerIcon(board.icon),
                                    { size: 16 },
                                )}
                                style={{ flexShrink: 0 }}
                                aria-label={
                                    showLabel ? undefined : board.name
                                }
                            >
                                {showLabel && board.name}
                            </Button>
                        </Tooltip>
                    );
                })}
                <Tooltip label="New board" withArrow>
                    <Button
                        size="sm"
                        radius="xl"
                        variant="outline"
                        color="gray"
                        onClick={onCreate}
                        px="sm"
                        style={{ flexShrink: 0 }}
                        aria-label="Create a new board"
                    >
                        <FiPlus size={18} />
                    </Button>
                </Tooltip>
            </Group>
        </ScrollArea>
    );
}
