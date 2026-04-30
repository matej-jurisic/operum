import { Button, Menu, Text } from "@mantine/core";
import { useEffect, useMemo } from "react";
import { CiFilter } from "react-icons/ci";
import { MdCheck } from "react-icons/md";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../context/ViewsContext";
import { ViewDto } from "../types/ViewDto";

export default function SelectViewMenu() {
    const { selectedViewIds, tracker } = useTracker();
    const { toggleSelectedView, clearSelectedViews } = useTrackerOperations();
    const { views, refreshViewsIfDirty } = useViews();

    useEffect(() => {
        refreshViewsIfDirty();
    }, []);

    // Compute sort conflicts: first-view-wins — later views lose on duplicate field IDs.
    const sortConflicts = useMemo(() => {
        const activeViews = selectedViewIds
            .map((id) => views.find((v) => v.id === id))
            .filter((v): v is ViewDto => v !== undefined);

        const seenFieldIds = new Map<string, string>(); // fieldId -> viewName that claimed it
        const conflicts: {
            fieldName: string;
            skippedView: string;
            keptView: string;
        }[] = [];

        for (const view of activeViews) {
            for (const sort of [...view.sorts].sort(
                (a, b) => a.order - b.order,
            )) {
                const claimedBy = seenFieldIds.get(sort.field.id);
                if (claimedBy) {
                    conflicts.push({
                        fieldName: sort.field.name,
                        skippedView: view.name,
                        keptView: claimedBy,
                    });
                } else {
                    seenFieldIds.set(sort.field.id, view.name);
                }
            }
        }

        return conflicts;
    }, [selectedViewIds, views]);

    return (
        <Menu
            position="bottom-start"
            width={300}
            styles={{ itemLabel: { minWidth: 0, overflow: "hidden" } }}
            closeOnItemClick={false}
        >
            <Menu.Target>
                <Button
                    variant={selectedViewIds.length > 0 ? "filled" : "outline"}
                    color={tracker.color}
                >
                    <CiFilter size={16} />
                    {selectedViewIds.length > 1 && (
                        <Text span size="xs" ml={6}>
                            {selectedViewIds.length}
                        </Text>
                    )}
                </Button>
            </Menu.Target>

            <Menu.Dropdown>
                <Menu.Label>Active Views</Menu.Label>
                <Menu.Divider />

                <Menu.Item
                    onClick={clearSelectedViews}
                    rightSection={
                        selectedViewIds.length === 0 ? (
                            <MdCheck size={16} />
                        ) : null
                    }
                    c="dimmed"
                    style={{ fontStyle: "italic" }}
                >
                    None
                </Menu.Item>

                {views.map((view) => (
                    <Menu.Item
                        key={view.id}
                        onClick={() => toggleSelectedView(view.id)}
                        rightSection={
                            selectedViewIds.includes(view.id) ? (
                                <MdCheck size={16} />
                            ) : null
                        }
                    >
                        <span
                            style={{
                                overflow: "hidden",
                                textOverflow: "ellipsis",
                                whiteSpace: "nowrap",
                                display: "block",
                            }}
                        >
                            {view.name}
                        </span>
                    </Menu.Item>
                ))}

                {sortConflicts.length > 0 && (
                    <>
                        <Menu.Divider />
                        <Menu.Label c="orange">Sort conflicts</Menu.Label>
                        {sortConflicts.map((c, i) => (
                            <Menu.Item
                                key={i}
                                disabled
                                style={{ cursor: "default" }}
                            >
                                <Text
                                    size="xs"
                                    c="dimmed"
                                    style={{ whiteSpace: "normal" }}
                                >
                                    <Text span c="orange" fw={500}>
                                        {c.fieldName}
                                    </Text>{" "}
                                    sort from{" "}
                                    <Text span fw={500}>
                                        {c.skippedView}
                                    </Text>{" "}
                                    skipped — already set by{" "}
                                    <Text span fw={500}>
                                        {c.keptView}
                                    </Text>
                                </Text>
                            </Menu.Item>
                        ))}
                    </>
                )}
            </Menu.Dropdown>
        </Menu>
    );
}
