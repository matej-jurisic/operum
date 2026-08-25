import { Button, Menu, useMantineTheme } from "@mantine/core";
import { createElement } from "react";
import { FiCheck, FiChevronDown, FiPlus } from "react-icons/fi";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { DashboardDto } from "../types/DashboardDto";

interface Props {
    boards: DashboardDto[];
    activeBoardId: string;
    onSelect: (boardId: string) => void;
    onCreate: () => void;
}

/**
 * Picks which board is shown, and nothing else: the actions on a board live in
 * BoardActions next to it. It ellipsizes its name rather than pushing the row wider, so
 * it is the one control here that gives up width when the viewport is narrow.
 */
export default function BoardSwitcher({
    boards,
    activeBoardId,
    onSelect,
    onCreate,
}: Props) {
    const theme = useMantineTheme();

    const resolveColor = (board: DashboardDto) =>
        board.color && board.color in theme.colors ? board.color : "indigo";

    const activeBoard = boards.find((b) => b.id === activeBoardId);
    const color = activeBoard ? resolveColor(activeBoard) : "indigo";

    return (
        <Menu shadow="md" position="bottom-start" withinPortal>
            <Menu.Target>
                <Button
                    size="sm"
                    radius="xl"
                    color={color}
                    variant="outline"
                    leftSection={
                        activeBoard &&
                        createElement(resolveTrackerIcon(activeBoard.icon), {
                            size: 16,
                        })
                    }
                    rightSection={<FiChevronDown size={14} />}
                    maw={280}
                    style={{ minWidth: 0 }}
                    styles={{
                        label: {
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap",
                        },
                        inner: { justifyContent: "space-between" },
                        section: { flexShrink: 0 },
                    }}
                >
                    {activeBoard?.name}
                </Button>
            </Menu.Target>
            <Menu.Dropdown miw={220}>
                <Menu.Label>Boards</Menu.Label>
                {boards.map((board) => {
                    const isActive = board.id === activeBoardId;
                    return (
                        <Menu.Item
                            key={board.id}
                            leftSection={createElement(
                                resolveTrackerIcon(board.icon),
                                { size: 16 },
                            )}
                            rightSection={isActive && <FiCheck size={16} />}
                            onClick={() => onSelect(board.id)}
                            fw={isActive ? 600 : undefined}
                            color={isActive ? resolveColor(board) : undefined}
                        >
                            {board.name}
                        </Menu.Item>
                    );
                })}
                <Menu.Item
                    leftSection={<FiPlus size={16} />}
                    onClick={onCreate}
                >
                    New board
                </Menu.Item>
            </Menu.Dropdown>
        </Menu>
    );
}
