import { Button, Menu } from "@mantine/core";
import { CiSettings } from "react-icons/ci";
import { MdDelete, MdEdit } from "react-icons/md";

interface Props {
    color?: string;
    isMobile: boolean;
    onEdit: () => void;
    onDelete: () => void;
}

/**
 * Edit / delete for the tracker whose page this is, as a cog in the top-right
 * corner -- the same control, in the same place, as a dashboard's BoardActions.
 * Only rendered for the tracker's owner.
 */
export default function TrackerActions({
    color,
    isMobile,
    onEdit,
    onDelete,
}: Props) {
    return (
        <Menu shadow="md" position="bottom-end" withinPortal>
            <Menu.Target>
                <Button
                    variant="outline"
                    color={color}
                    px={isMobile ? "xs" : undefined}
                    aria-label="Tracker actions"
                    style={{ flexShrink: 0 }}
                >
                    <CiSettings size={18} />
                </Button>
            </Menu.Target>
            <Menu.Dropdown miw={180}>
                <Menu.Item leftSection={<MdEdit size={16} />} onClick={onEdit}>
                    Edit tracker
                </Menu.Item>
                <Menu.Item
                    color="red"
                    leftSection={<MdDelete size={16} />}
                    onClick={onDelete}
                >
                    Delete tracker
                </Menu.Item>
            </Menu.Dropdown>
        </Menu>
    );
}
