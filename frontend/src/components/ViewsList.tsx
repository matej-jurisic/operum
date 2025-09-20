import {
    ActionIcon,
    Button,
    Card,
    Group,
    Menu,
    Paper,
    ScrollArea,
    Stack,
    Text,
    Title,
    Tooltip,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { IoChevronDownCircle } from "react-icons/io5";
import { MdCheck, MdDelete } from "react-icons/md";
import { RiFileListFill } from "react-icons/ri";
import api from "../api/api";
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

const SetDefaultView = async (trackerId: string, viewId?: string) => {
    await api.put(`/trackers/${trackerId}/default-view`, null, {
        params: { defaultViewId: viewId },
    });
};

export default function ViewsList(props: Props) {
    const [selectedView, setSelectedView] = useState<ViewDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const { views, refreshViewsIfDirty, DeleteView, fields, tracker } =
        useTracker();
    const [defaultView, setDefaultView] = useState<string | null>(
        tracker.defaultViewId ?? null
    );

    const selectViewList = useMemo(() => {
        const list = views.map((v: ViewDto) => ({
            value: v.id,
            label: v.name,
        }));

        // If something is selected, move it to the top
        if (defaultView) {
            const selectedIndex = list.findIndex(
                (v) => v.value === defaultView
            );
            if (selectedIndex > -1) {
                const [selected] = list.splice(selectedIndex, 1);
                list.unshift(selected);
            }
        } else {
            setDefaultView(null);
        }

        return list;
    }, [views, defaultView]);

    useEffect(() => {
        refreshViewsIfDirty();
    }, []);

    return (
        <>
            <Stack gap="md" h={"100%"}>
                <Group justify="space-between" w="100%">
                    <Tooltip
                        label={
                            fields.length === 0
                                ? "Cannot create view: No fields available"
                                : ""
                        }
                        disabled={fields.length > 0}
                        withArrow
                    >
                        <Button
                            color={props.tracker.color}
                            onClick={() =>
                                setOpenDialogType(OpenDialogType.CreateView)
                            }
                            disabled={fields.length === 0}
                            variant="outline"
                            leftSection={<FiPlus size={18} />}
                        >
                            Create
                        </Button>
                    </Tooltip>
                    <Menu position="bottom-end">
                        <Menu.Target>
                            <ActionIcon
                                variant="outline"
                                color={tracker.color}
                                size={"lg"}
                            >
                                <IoChevronDownCircle size={18} />
                            </ActionIcon>
                        </Menu.Target>
                        <Menu.Dropdown>
                            <Menu.Label>Default View</Menu.Label>
                            <Menu.Divider />
                            <Menu.Item
                                onClick={async () => {
                                    setDefaultView(null);
                                    await SetDefaultView(tracker.id, undefined);
                                }}
                                rightSection={
                                    !defaultView ? <MdCheck size={16} /> : null
                                }
                                c="dimmed"
                                style={{ fontStyle: "italic" }}
                            >
                                None
                            </Menu.Item>
                            {selectViewList.map((x) => (
                                <Menu.Item
                                    onClick={async () => {
                                        setDefaultView(x.value);
                                        await SetDefaultView(
                                            tracker.id,
                                            x.value
                                        );
                                    }}
                                    rightSection={
                                        defaultView === x.value ? (
                                            <MdCheck size={16} />
                                        ) : null
                                    }
                                    key={x.value}
                                >
                                    {x.label}
                                </Menu.Item>
                            ))}
                        </Menu.Dropdown>
                    </Menu>
                </Group>
                <ScrollArea flex={1} mih={0}>
                    <Stack gap="md">
                        {views.length > 0 ? (
                            views.map((view) => (
                                <Card
                                    key={view.id}
                                    p="md"
                                    radius="md"
                                    withBorder
                                >
                                    <Group
                                        wrap="nowrap"
                                        justify="space-between"
                                        align="flex-start"
                                    >
                                        <Stack gap="sm">
                                            <Title
                                                order={4}
                                                lineClamp={1}
                                                className="wrapped-text"
                                            >
                                                {view.name}
                                            </Title>
                                            <Text
                                                c="dimmed"
                                                size="sm"
                                                lineClamp={3}
                                                className="wrapped-text"
                                            >
                                                {view.description ||
                                                    "No description"}
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
                            ))
                        ) : (
                            <Paper withBorder p="xl" radius="md">
                                <Stack gap="md" align="center">
                                    <Text size="lg" fw={500} c="dimmed">
                                        No Views Available
                                    </Text>
                                    <Text ta="center" c="dimmed">
                                        Views will appear here when you create
                                        them.
                                    </Text>
                                </Stack>
                            </Paper>
                        )}
                    </Stack>
                </ScrollArea>
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
                />
            )}

            {selectedView && openDialogType === OpenDialogType.DeleteView && (
                <ConfirmationDialog
                    isOpen={selectedView !== undefined}
                    onClose={() => setOpenDialogType(undefined)}
                    onConfirm={async () => {
                        if (selectedView) {
                            await DeleteView(props.tracker.id, selectedView.id);
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
