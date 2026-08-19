import {
    Button,
    Group,
    ScrollArea,
    Tooltip,
    useMantineTheme,
} from "@mantine/core";
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

    return (
        <ScrollArea
            type="hover"
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

                    return (
                        <Button
                            key={board.id}
                            size="sm"
                            radius="xl"
                            color={color}
                            variant={isActive ? "filled" : "light"}
                            onClick={() => onSelect(board.id)}
                            leftSection={createElement(
                                resolveTrackerIcon(board.icon),
                                { size: 16 },
                            )}
                            style={{ flexShrink: 0 }}
                        >
                            {board.name}
                        </Button>
                    );
                })}
                <Tooltip label="New board" withArrow>
                    <Button
                        size="sm"
                        radius="xl"
                        variant="subtle"
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
