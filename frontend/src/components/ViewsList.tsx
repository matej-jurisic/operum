import { ActionIcon, Button, Card, Group, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import { RiFileListFill } from "react-icons/ri";
import { useTracker } from "../context/TrackerContext";
import { TrackerDto } from "../model/TrackerDto";
import { ViewDto } from "../model/ViewDto";
import ConfirmationDialog from "./ConfirmationDialog";
import ViewDetailsDialog from "./ViewDetailsDialog";
import ViewFormDialog from "./ViewFormDialog";

interface Props {
    tracker: TrackerDto;
}

enum OpenDialogType {
    ViewDetails,
    CreateView,
    DeleteView,
}

export default function ViewsList(props: Props) {
    const [selectedView, setSelectedView] = useState<ViewDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const { views, refreshViews, DeleteView } = useTracker();

    useEffect(() => {
        refreshViews();
    }, []);

    return (
        <>
            <Stack gap="md">
                <Group justify="space-between" w="100%" h={36}>
                    <Button
                        color={props.tracker.color}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateView)
                        }
                        leftSection={<FiPlus size={18} />}
                    >
                        Create
                    </Button>
                </Group>
                {views.map((view) => (
                    <Card key={view.id} p="md" radius="md" withBorder>
                        <Group
                            wrap="nowrap"
                            justify="space-between"
                            align="flex-start"
                        >
                            <Stack gap="sm">
                                <Text
                                    className="wrapped-text"
                                    fw={600}
                                    size="md"
                                    lineClamp={1}
                                >
                                    {view.name}
                                </Text>
                            </Stack>
                            <Group gap="xs" wrap="nowrap">
                                <ActionIcon
                                    variant="outline"
                                    color={props.tracker.color}
                                    size="lg"
                                    onClick={() => {
                                        setSelectedView(view);
                                        setOpenDialogType(
                                            OpenDialogType.ViewDetails
                                        );
                                    }}
                                    aria-label={`Edit field ${view.name}`}
                                >
                                    <RiFileListFill size={16} />
                                </ActionIcon>
                                <ActionIcon
                                    variant="outline"
                                    color="red"
                                    size="lg"
                                    onClick={() => {
                                        setSelectedView(view);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteView
                                        );
                                    }}
                                    aria-label={`Delete view ${view.name}`}
                                >
                                    <MdDelete size={16} />
                                </ActionIcon>
                            </Group>
                        </Group>
                    </Card>
                ))}
            </Stack>

            {selectedView && openDialogType === OpenDialogType.ViewDetails && (
                <ViewDetailsDialog
                    viewId={selectedView.id}
                    tracker={props.tracker}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}

            {openDialogType === OpenDialogType.CreateView && (
                <ViewFormDialog
                    tracker={props.tracker}
                    onClose={() => setOpenDialogType(undefined)}
                    onCreated={async () => refreshViews()}
                />
            )}

            {selectedView && openDialogType === OpenDialogType.DeleteView && (
                <ConfirmationDialog
                    isOpen={selectedView !== undefined}
                    onClose={() => setOpenDialogType(undefined)}
                    onConfirm={async () => {
                        if (selectedView) {
                            await DeleteView(props.tracker.id, selectedView.id);
                            refreshViews();
                            setOpenDialogType(undefined);
                            setSelectedView(undefined);
                        }
                    }}
                    severity="warning"
                    message={`Are you sure you want to delete the view "${selectedView.name}"?`}
                />
            )}
        </>
    );
}
