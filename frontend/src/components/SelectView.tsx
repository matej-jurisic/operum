import { Button, Menu } from "@mantine/core";
import { useEffect, useMemo } from "react";
import { LuFilter } from "react-icons/lu";
import { MdCheck } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import { ViewDto } from "../model/ViewDto";

export default function SelectViewMenu() {
    const {
        views,
        selectedViewId,
        setSelectedViewId,
        refreshViewsIfDirty,
        tracker,
    } = useTracker();

    useEffect(() => {
        refreshViewsIfDirty();
    }, []);

    const selectViewList = useMemo(() => {
        const list = views.map((v: ViewDto) => ({
            value: v.id,
            label: v.name,
        }));

        // If something is selected, move it to the top
        if (selectedViewId) {
            const selectedIndex = list.findIndex(
                (v) => v.value === selectedViewId
            );
            if (selectedIndex > -1) {
                const [selected] = list.splice(selectedIndex, 1);
                list.unshift(selected);
            }
        }

        return list;
    }, [views, selectedViewId]);

    return (
        <Menu position="bottom-start" width={200}>
            <Menu.Target>
                <Button
                    variant={selectedViewId ? "filled" : "outline"}
                    color={tracker.color}
                >
                    <LuFilter size={16} />
                </Button>
            </Menu.Target>

            <Menu.Dropdown>
                <Menu.Label>Active View</Menu.Label>
                <Menu.Divider />
                {/* Deselect option */}
                <Menu.Item
                    onClick={() => setSelectedViewId(undefined)}
                    rightSection={
                        !selectedViewId ? <MdCheck size={16} /> : null
                    }
                    c="dimmed"
                    style={{ fontStyle: "italic" }}
                >
                    None
                </Menu.Item>

                {selectViewList.map((item) => (
                    <Menu.Item
                        key={item.value}
                        onClick={() => setSelectedViewId(item.value)}
                        rightSection={
                            selectedViewId === item.value ? (
                                <MdCheck size={16} />
                            ) : null
                        }
                    >
                        {item.label}
                    </Menu.Item>
                ))}
            </Menu.Dropdown>
        </Menu>
    );
}
