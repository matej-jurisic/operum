import { Button, Menu } from "@mantine/core";
import { useEffect, useMemo } from "react";
import { LuFilter } from "react-icons/lu";
import { MdCheck } from "react-icons/md";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../context/ViewsContext";
import { ViewDto } from "../types/ViewDto";

export default function SelectViewMenu() {
    const { selectedViewId, tracker } = useTracker();
    const { setSelectedView } = useTrackerOperations();
    const { views, refreshViewsIfDirty } = useViews();

    useEffect(() => {
        refreshViewsIfDirty();
    }, []);

    const selectViewList = useMemo(() => {
        return views.map((v: ViewDto) => ({
            value: v.id,
            label: v.name,
        }));
    }, [views]);

    return (
        <Menu position="bottom-start" width={280} styles={{ itemLabel: { minWidth: 0, overflow: "hidden" } }}>
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
                    onClick={() => {
                        setSelectedView(undefined);
                    }}
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
                        onClick={() => {
                            setSelectedView(item.value);
                        }}
                        rightSection={
                            selectedViewId === item.value ? (
                                <MdCheck size={16} />
                            ) : null
                        }
                    >
                        <span style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", display: "block" }}>
                            {item.label}
                        </span>
                    </Menu.Item>
                ))}
            </Menu.Dropdown>
        </Menu>
    );
}
