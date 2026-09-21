import { ActionIcon, Checkbox, Group, Menu, Text, Tooltip } from "@mantine/core";
import { IoMdEye } from "react-icons/io";
import { useTracker } from "../../trackers/context/TrackerContext";
import { ExtraColumns, useVisibleColumns } from "../hooks/useVisibleColumns";

export function ColumnVisibilityMenu() {
    const { tracker } = useTracker();
    const { fields, isColumnVisible, toggleColumn } = useVisibleColumns();
    return (
        <Menu
            shadow="md"
            position="bottom-end"
            closeOnItemClick={false}
            width={200}
        >
            <Menu.Target>
                <Tooltip label="Show or hide columns">
                    <ActionIcon
                        variant="outline"
                        color={tracker.color}
                        size={"lg"}
                        disabled={fields.length === 0}
                        aria-label="Show or hide columns"
                    >
                        <IoMdEye size={18} />
                    </ActionIcon>
                </Tooltip>
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
                                checked={isColumnVisible(field.id)}
                                readOnly
                            />
                        </Group>
                    </Menu.Item>
                ))}

                <Menu.Divider />

                <Menu.Item
                    onClick={() => toggleColumn(ExtraColumns.CreatedAt)}
                    px="xs"
                >
                    <Group justify="space-between">
                        <Text size="sm">Created at</Text>
                        <Checkbox
                            size="sm"
                            color={tracker.color}
                            checked={isColumnVisible(ExtraColumns.CreatedAt)}
                            readOnly
                            tabIndex={-1}
                        />
                    </Group>
                </Menu.Item>

                <Menu.Item
                    onClick={() => toggleColumn(ExtraColumns.Actions)}
                    px="xs"
                >
                    <Group justify="space-between">
                        <Text size="sm">Actions</Text>
                        <Checkbox
                            color={tracker.color}
                            size="sm"
                            checked={isColumnVisible(ExtraColumns.Actions)}
                            readOnly
                            tabIndex={-1}
                        />
                    </Group>
                </Menu.Item>
            </Menu.Dropdown>
        </Menu>
    );
}
