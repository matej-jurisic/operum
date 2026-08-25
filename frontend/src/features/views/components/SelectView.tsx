import {
    Button,
    Group,
    Modal,
    Radio,
    ScrollArea,
    Stack,
    Text,
    UnstyledButton,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { CiFilter } from "react-icons/ci";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../context/ViewsContext";

export default function SelectViewMenu() {
    const { selectedViewId, tracker } = useTracker();
    const { setSelectedView } = useTrackerOperations();
    const { views, refreshViewsIfDirty } = useViews();

    const [opened, setOpened] = useState(false);
    const [draftViewId, setDraftViewId] = useState<string | null>(
        selectedViewId,
    );

    useEffect(() => {
        refreshViewsIfDirty();
    }, []);

    const openModal = () => {
        setDraftViewId(selectedViewId);
        setOpened(true);
    };

    const handleApply = async () => {
        setOpened(false);
        await setSelectedView(draftViewId);
    };

    return (
        <>
            <Button
                variant={selectedViewId ? "filled" : "outline"}
                color={tracker.color}
                onClick={openModal}
            >
                <CiFilter size={16} />
            </Button>

            <Modal
                opened={opened}
                centered
                onClose={() => setOpened(false)}
                title="Active View"
                size="md"
            >
                <Stack gap="lg">
                    {views.length > 0 ? (
                        <ScrollArea.Autosize mah={360}>
                            <Radio.Group
                                value={draftViewId ?? ""}
                                onChange={(value) =>
                                    setDraftViewId(value || null)
                                }
                            >
                                <Stack gap="xs">
                                    <UnstyledButton
                                        onClick={() => setDraftViewId(null)}
                                        px="xs"
                                        py={6}
                                    >
                                        <Group justify="space-between" wrap="nowrap">
                                            <Text
                                                size="sm"
                                                c="dimmed"
                                                fs="italic"
                                            >
                                                None (every entry)
                                            </Text>
                                            <Radio value="" readOnly tabIndex={-1} />
                                        </Group>
                                    </UnstyledButton>
                                    {views.map((view) => (
                                        <UnstyledButton
                                            key={view.id}
                                            onClick={() =>
                                                setDraftViewId(view.id)
                                            }
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
                                                        textOverflow:
                                                            "ellipsis",
                                                        whiteSpace: "nowrap",
                                                    }}
                                                >
                                                    {view.name}
                                                </Text>
                                                <Radio
                                                    value={view.id}
                                                    color={tracker.color}
                                                    readOnly
                                                    tabIndex={-1}
                                                />
                                            </Group>
                                        </UnstyledButton>
                                    ))}
                                </Stack>
                            </Radio.Group>
                        </ScrollArea.Autosize>
                    ) : (
                        <Text size="sm" c="dimmed" ta="center">
                            No views available.
                        </Text>
                    )}

                    <Group justify="flex-end">
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
                </Stack>
            </Modal>
        </>
    );
}
