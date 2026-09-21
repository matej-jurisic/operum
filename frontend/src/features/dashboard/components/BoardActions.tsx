import { Button, Menu } from "@mantine/core";
import { CiSettings } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import { TbArrowsMove } from "react-icons/tb";

interface Props {
    color: string;
    isConfiguring: boolean;
    isMobile: boolean;
    onEdit: () => void;
    onDelete: () => void;
    onToggleArrange: () => void;
    onOpenWidgets: () => void;
}

export default function BoardActions({
    color,
    isConfiguring,
    isMobile,
    onEdit,
    onDelete,
    onToggleArrange,
    onOpenWidgets,
}: Props) {
    return (
        <Menu shadow="md" position="bottom-start" withinPortal>
            <Menu.Target>
                <Button
                    variant="outline"
                    color={color}
                    px={isMobile ? "xs" : undefined}
                    aria-label="Dashboard actions"
                    style={{ flexShrink: 0 }}
                >
                    <CiSettings size={18} />
                </Button>
            </Menu.Target>
            <Menu.Dropdown miw={200}>
                <Menu.Item
                    leftSection={<FiPlus size={16} />}
                    onClick={onOpenWidgets}
                >
                    Add widget
                </Menu.Item>
                <Menu.Item
                    leftSection={<TbArrowsMove size={16} />}
                    onClick={onToggleArrange}
                >
                    {isConfiguring ? "Stop arranging" : "Arrange dashboard"}
                </Menu.Item>
                <Menu.Item leftSection={<MdEdit size={16} />} onClick={onEdit}>
                    Edit dashboard
                </Menu.Item>
                <Menu.Divider />
                <Menu.Item
                    color="red"
                    leftSection={<MdDelete size={16} />}
                    onClick={onDelete}
                >
                    Delete dashboard
                </Menu.Item>
            </Menu.Dropdown>
        </Menu>
    );
}
