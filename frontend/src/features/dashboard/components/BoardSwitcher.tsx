import {
    Button,
    Group,
    Menu,
    ScrollArea,
    Tooltip,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
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

export default function BoardSwitcher({
    boards,
    activeBoardId,
    onSelect,
    onCreate,
}: Props) {
    const theme = useMantineTheme();
    const isMobile = useMediaQuery("(max-width: 48em)");

    const resolveColor = (board: DashboardDto) =>
        board.color && board.color in theme.colors ? board.color : "indigo";

    if (isMobile) {
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
                            createElement(
                                resolveTrackerIcon(activeBoard.icon),
                                { size: 16 },
                            )
                        }
                        rightSection={<FiChevronDown size={14} />}
                        style={{ minWidth: 0, flex: 1 }}
                        styles={{
                            label: {
                                overflow: "hidden",
                                textOverflow: "ellipsis",
                                whiteSpace: "nowrap",
                            },
                            inner: { justifyContent: "space-between" },
                        }}
                        fullWidth
                    >
                        {activeBoard?.name}
                    </Button>
                </Menu.Target>
                <Menu.Dropdown miw={220}>
                    {boards.map((board) => {
                        const isActive = board.id === activeBoardId;
                        return (
                            <Menu.Item
                                key={board.id}
                                leftSection={createElement(
                                    resolveTrackerIcon(board.icon),
                                    { size: 16 },
                                )}
                                rightSection={
                                    isActive && <FiCheck size={16} />
                                }
                                onClick={() => onSelect(board.id)}
                                fw={isActive ? 600 : undefined}
                                color={
                                    isActive ? resolveColor(board) : undefined
                                }
                            >
                                {board.name}
                            </Menu.Item>
                        );
                    })}
                    <Menu.Divider />
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
                    const color = resolveColor(board);
                    const isActive = board.id === activeBoardId;

                    return (
                        <Button
                            key={board.id}
                            size="sm"
                            radius="xl"
                            color={color}
                            variant={isActive ? "filled" : "outline"}
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
