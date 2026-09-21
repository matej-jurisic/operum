import { Button, Menu } from "@mantine/core";
import { CiSettings } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import { TbArrowsMove } from "react-icons/tb";
import { VscJson } from "react-icons/vsc";

interface Props {
    color: string;
    isConfiguring: boolean;
    isMobile: boolean;
    onEdit: () => void;
    onDelete: () => void;
    onToggleArrange: () => void;
    onOpenWidgets: () => void;
    onEditDocument: () => void;
}

export default function BoardActions({
    color,
    isConfiguring,
    isMobile,
    onEdit,
    onDelete,
    onToggleArrange,
    onOpenWidgets,
    onEditDocument,
}: Props) {
    return (
        <Menu shadow="md" position="bottom-start" withinPortal>
            <Menu.Target>
                <Button
                    variant="outline"
                    color={color}
                    px={isMobile ? "xs" : undefined}
                    aria-label="Board actions"
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
                    {isConfiguring ? "Stop arranging" : "Arrange board"}
                </Menu.Item>
                <Menu.Item leftSection={<MdEdit size={16} />} onClick={onEdit}>
                    Edit board
                </Menu.Item>
                <Menu.Item
                    leftSection={<VscJson size={16} />}
                    onClick={onEditDocument}
                >
                    Edit as JSON
                </Menu.Item>
                <Menu.Divider />
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
