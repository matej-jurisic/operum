import {
    Button,
    Checkbox,
    Group,
    Modal,
    ScrollArea,
    Stack,
    Text,
    UnstyledButton,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { CiFilter } from "react-icons/ci";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../context/ViewsContext";
import { ViewDto } from "../types/ViewDto";

export default function SelectViewMenu() {
    const { selectedViewIds, tracker } = useTracker();
    const { setSelectedViews } = useTrackerOperations();
    const { views, refreshViewsIfDirty } = useViews();

    const [opened, setOpened] = useState(false);
    const [draftViewIds, setDraftViewIds] = useState<string[]>(selectedViewIds);

    useEffect(() => {
        refreshViewsIfDirty();
    }, []);

    const openModal = () => {
        setDraftViewIds(selectedViewIds);
        setOpened(true);
    };

    const toggleDraftView = (viewId: string) => {
        setDraftViewIds((prev) =>
            prev.includes(viewId)
                ? prev.filter((id) => id !== viewId)
                : [...prev, viewId],
        );
    };

    const handleApply = async () => {
        setOpened(false);
        await setSelectedViews(draftViewIds);
    };

    // Compute sort conflicts: first-view-wins — later views lose on duplicate field IDs.
    const sortConflicts = useMemo(() => {
        const activeViews = draftViewIds
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
    }, [draftViewIds, views]);

    return (
        <>
            <Button
                variant={selectedViewIds.length > 0 ? "filled" : "outline"}
                color={tracker.color}
                onClick={openModal}
            >
                <CiFilter size={16} />
                {selectedViewIds.length > 1 && (
                    <Text span size="xs" ml={6}>
                        {selectedViewIds.length}
                    </Text>
                )}
            </Button>

            <Modal
                opened={opened}
                centered
                onClose={() => setOpened(false)}
                title="Active Views"
                size="md"
            >
                <Stack gap="lg">
                    {views.length > 0 ? (
                        <ScrollArea.Autosize mah={360}>
                            <Stack gap="xs">
                                {views.map((view) => (
                                    <UnstyledButton
                                        key={view.id}
                                        onClick={() => toggleDraftView(view.id)}
                                        px="xs"
                                        py={6}
                                    >
                                        <Group
                                            justify="space-between"
                                            wrap="nowrap"
                                        >
                                            <Text
                                                size="sm"
                                                style={{
                                                    overflow: "hidden",
                                                    textOverflow: "ellipsis",
                                                    whiteSpace: "nowrap",
                                                }}
                                            >
                                                {view.name}
                                            </Text>
                                            <Checkbox
                                                size="sm"
                                                color={tracker.color}
                                                checked={draftViewIds.includes(
                                                    view.id,
                                                )}
                                                readOnly
                                                tabIndex={-1}
                                            />
                                        </Group>
                                    </UnstyledButton>
                                ))}
                            </Stack>
                        </ScrollArea.Autosize>
                    ) : (
                        <Text size="sm" c="dimmed" ta="center">
                            No views available.
                        </Text>
                    )}

                    {sortConflicts.length > 0 && (
                        <Stack gap={4}>
                            <Text size="sm" fw={500} c="orange">
                                Sort conflicts
                            </Text>
                            {sortConflicts.map((c, i) => (
                                <Text key={i} size="xs" c="dimmed">
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
                            ))}
                        </Stack>
                    )}

                    <Group justify="space-between">
                        <Button
                            variant="subtle"
                            color={tracker.color}
                            onClick={() => setDraftViewIds([])}
                            disabled={draftViewIds.length === 0}
                        >
                            Clear all
                        </Button>
                        <Group>
                            <Button
                                variant="default"
                                onClick={() => setOpened(false)}
                            >
                                Cancel
                            </Button>
                            <Button color={tracker.color} onClick={handleApply}>
                                Apply
                            </Button>
                        </Group>
                    </Group>
                </Stack>
            </Modal>
        </>
    );
}
