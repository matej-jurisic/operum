import { ActionIcon, Checkbox, Group, Menu, Text } from "@mantine/core";
import { IoMdEye } from "react-icons/io";
import { useTracker } from "../context/TrackerContext";

export function ColumnVisibilityMenu() {
    const { fields, visibleColumns, toggleColumn, tracker } = useTracker();
    return (
        <Menu
            shadow="md"
            position="bottom-end"
            closeOnItemClick={false}
            width={200}
        >
            <Menu.Target>
                <ActionIcon
                    variant="outline"
                    color={tracker.color}
                    size={"lg"}
                    disabled={fields.length === 0}
                >
                    <IoMdEye size={18} />
                </ActionIcon>
            </Menu.Target>

            <Menu.Dropdown>
                {fields.map((field) => (
                    <Menu.Item
                        key={field.id}
                        onClick={() => toggleColumn(field.id)}
                        px="xs"
                    >
                        <Group justify="space-between">
                            <Text
                                className="wrapped-text"
                                size="sm"
                                maw={"70%"}
                            >
                                {field.name}
                            </Text>
                            <Checkbox
                                size="sm"
                                color={tracker.color}
                                checked={visibleColumns[field.id] || false}
                                readOnly
                            />
                        </Group>
                    </Menu.Item>
                ))}

                <Menu.Divider />

                <Menu.Item onClick={() => toggleColumn("createdAt")} px="xs">
                    <Group justify="space-between">
                        <Text size="sm">Created At</Text>
                        <Checkbox
                            size="sm"
                            color={tracker.color}
                            checked={visibleColumns["createdAt"] || false}
                            readOnly
                            tabIndex={-1}
                        />
                    </Group>
                </Menu.Item>

                <Menu.Item onClick={() => toggleColumn("actions")} px="xs">
                    <Group justify="space-between">
                        <Text size="sm">Actions</Text>
                        <Checkbox
                            color={tracker.color}
                            size="sm"
                            checked={visibleColumns["actions"] || false}
                            readOnly
                            tabIndex={-1}
                        />
                    </Group>
                </Menu.Item>
            </Menu.Dropdown>
        </Menu>
    );
}
