import { Button, Menu, useMantineTheme } from "@mantine/core";
import { createElement } from "react";
import { CiSettings } from "react-icons/ci";
import { FiCheck, FiChevronDown, FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { DashboardDto } from "../types/DashboardDto";

interface Props {
    boards: DashboardDto[];
    activeBoardId: string;
    isConfiguring: boolean;
    onSelect: (boardId: string) => void;
    onCreate: () => void;
    onEdit: () => void;
    onDelete: () => void;
    onAddItem: () => void;
    onToggleArrange: () => void;
}

/**
 * The board's single control: it switches boards and holds every action on the board
 * itself, so the page above the grid stays one row of chrome no matter how many boards
 * exist.
 */
export default function BoardSwitcher({
    boards,
    activeBoardId,
    isConfiguring,
    onSelect,
    onCreate,
    onEdit,
    onDelete,
    onAddItem,
    onToggleArrange,
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

                <Menu.Divider />
                <Menu.Label>This board</Menu.Label>
                <Menu.Item
                    leftSection={<FiPlus size={16} />}
                    onClick={onAddItem}
                >
                    Add widget
                </Menu.Item>
                <Menu.Item
                    leftSection={<CiSettings size={16} />}
                    onClick={onToggleArrange}
                >
                    {isConfiguring ? "Stop arranging" : "Arrange board"}
                </Menu.Item>
                <Menu.Item leftSection={<MdEdit size={16} />} onClick={onEdit}>
                    Edit board
                </Menu.Item>
                <Menu.Item
                    color="red"
                    leftSection={<MdDelete size={16} />}
                    onClick={onDelete}
                >
                    Delete board
                </Menu.Item>
            </Menu.Dropdown>
        </Menu>
    );
}
