import { ActionIcon, Button, Card, Group, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdEdit } from "react-icons/md";
import api from "../api/api";
import { TrackerDto } from "../model/TrackerDto";
import { ViewDto } from "../model/ViewDto";
import ViewDetailsDialog from "./ViewDetailsDialog";
import ViewFormDialog from "./ViewFormDialog";

interface Props {
    tracker: TrackerDto;
}

const GetViewList = async (trackerId: string) => {
    const response = await api.get(`trackers/${trackerId}/views`);
    return response.data.data;
};

enum OpenDialogType {
    ViewDetails,
    CreateView,
}

export default function ViewsList(props: Props) {
    const [views, setViews] = useState<ViewDto[]>([]);
    const [selectedView, setSelectedView] = useState<ViewDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    useEffect(() => {
        const GetViews = async () => {
            setViews(await GetViewList(props.tracker.id));
        };

        GetViews();
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
                        <Group wrap="nowrap" justify="space-between">
                            <Stack gap="sm">
                                <Text fw={600} size="md" truncate="end">
                                    {view.name}
                                </Text>
                            </Stack>
                            <ActionIcon
                                variant="outline"
                                color="green"
                                size="lg"
                                onClick={() => {
                                    setSelectedView(view);
                                    setOpenDialogType(
                                        OpenDialogType.ViewDetails
                                    );
                                }}
                                aria-label={`Edit field ${view.name}`}
                            >
                                <MdEdit size={16} />
                            </ActionIcon>
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
                    onCreated={async () =>
                        setViews(await GetViewList(props.tracker.id))
                    }
                />
            )}
        </>
    );
}
