import {
    closestCenter,
    DndContext,
    DragEndEvent,
    KeyboardSensor,
    PointerSensor,
    useSensor,
    useSensors,
} from "@dnd-kit/core";
import { restrictToParentElement } from "@dnd-kit/modifiers";
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import {
    ActionIcon,
    Button,
    Group,
    Menu,
    ScrollArea,
    Stack,
} from "@mantine/core";
import { ReactNode, useEffect, useState } from "react";
import { CiBoxList } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { IoChevronDownCircle } from "react-icons/io5";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import EmptyState from "../../../shared/components/EmptyState";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../../trackers/context/TrackerContext";

import { MdCheck } from "react-icons/md";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../api/viewsController";
import { useViews } from "../context/ViewsContext";
import { ViewDto } from "../types/ViewDto";
import SortableViewCard from "./SortableViewCard";
import ViewDetailsDialog from "./ViewDetailsDialog";
import ViewFormDialog from "./ViewFormDialog";

interface Props {
    tracker: TrackerDto;
    // Rendered in the action row right of Create, so the Views/Queries switch shares it.
    headerSection?: ReactNode;
}

enum OpenDialogType {
    ViewDetails,
    CreateView,
    EditView,
    DeleteView,
}

export default function Views(props: Props) {
    const [selectedView, setSelectedView] = useState<ViewDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [sortedViews, setSortedViews] = useState<ViewDto[]>([]);
    const [isReordering, setIsReordering] = useState(false);

    const { tracker, setTracker } = useTracker();
    const { views, refreshViewsIfDirty } = useViews();
    const { fields } = useFields();
    const { deleteView, updateViewOrder } = useTrackerOperations();

    const [defaultViewId, setDefaultViewId] = useState<string | null>(
        tracker.defaultViewId ?? null,
    );

    const sensors = useSensors(
        useSensor(PointerSensor, {
            activationConstraint: {
                distance: 4,
            },
        }),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates,
        }),
    );

    useEffect(() => {
        refreshViewsIfDirty();
    }, []);

    useEffect(() => {
        setSortedViews([...views]);
    }, [views]);

    const handleDragEnd = async (event: DragEndEvent) => {
        const { active, over } = event;

        if (over && active.id !== over.id) {
            const oldIndex = sortedViews.findIndex((v) => v.id === active.id);
            const newIndex = sortedViews.findIndex((v) => v.id === over.id);

            const newSortedViews = arrayMove(sortedViews, oldIndex, newIndex);
            setSortedViews(newSortedViews);
            try {
                await updateViewOrder(newSortedViews.map((v) => v.id));
            } catch (error) {
                setSortedViews([...views]);
                console.error("Failed to update view order:", error);
            }
        }
    };

    const MAX_VIEWS = 25;
    const { isOwner, canEditSchema } = useTracker();

    return (
        <>
            <Stack gap="md" h={"100%"}>
                {(canEditSchema || props.headerSection) && (
                    <Group justify="space-between" w="100%">
                        <Group gap="sm">
                            {canEditSchema && (
                                <Button
                                    color={props.tracker.color}
                                    onClick={() =>
                                        setOpenDialogType(
                                            OpenDialogType.CreateView,
                                        )
                                    }
                                    disabled={
                                        fields.length === 0 ||
                                        views.length >= MAX_VIEWS
                                    }
                                    variant="outline"
                                    leftSection={<FiPlus size={18} />}
                                >
                                    Create
                                </Button>
                            )}
                            {props.headerSection}
                        </Group>
                        <Group>
                            {canEditSchema && isOwner && (
                                <ActionIcon
                                    size={"lg"}
                                    variant={
                                        isReordering ? "filled" : "outline"
                                    }
                                    onClick={() =>
                                        setIsReordering((prev) => !prev)
                                    }
                                    color={props.tracker.color}
                                >
                                    <CiBoxList size={18} />
                                </ActionIcon>
                            )}
                            {canEditSchema && isOwner && (
                                <Menu
                                    position="bottom-end"
                                    width={280}
                                    styles={{
                                        itemLabel: {
                                            minWidth: 0,
                                            overflow: "hidden",
                                        },
                                    }}
                                >
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
                                                const prev = defaultViewId;
                                                setDefaultViewId(null);
                                                try {
                                                    await viewsController.setDefaultView(
                                                        tracker.id,
                                                        null,
                                                    );
                                                    setTracker((t) => ({
                                                        ...t,
                                                        defaultViewId:
                                                            undefined,
                                                    }));
                                                } catch {
                                                    setDefaultViewId(prev);
                                                }
                                            }}
                                            rightSection={
                                                defaultViewId === null ? (
                                                    <MdCheck size={16} />
                                                ) : null
                                            }
                                            c="dimmed"
                                            style={{ fontStyle: "italic" }}
                                        >
                                            None
                                        </Menu.Item>
                                        {sortedViews.map((v) => {
                                            const isDefault =
                                                defaultViewId === v.id;
                                            return (
                                                <Menu.Item
                                                    onClick={async () => {
                                                        const prev =
                                                            defaultViewId;
                                                        setDefaultViewId(v.id);
                                                        try {
                                                            await viewsController.setDefaultView(
                                                                tracker.id,
                                                                v.id,
                                                            );
                                                            setTracker((t) => ({
                                                                ...t,
                                                                defaultViewId:
                                                                    v.id,
                                                            }));
                                                        } catch {
                                                            setDefaultViewId(
                                                                prev,
                                                            );
                                                        }
                                                    }}
                                                    rightSection={
                                                        isDefault ? (
                                                            <MdCheck
                                                                size={16}
                                                            />
                                                        ) : null
                                                    }
                                                    key={v.id}
                                                >
                                                    <span
                                                        style={{
                                                            overflow: "hidden",
                                                            textOverflow:
                                                                "ellipsis",
                                                            whiteSpace:
                                                                "nowrap",
                                                            display: "block",
                                                        }}
                                                    >
                                                        {v.name}
                                                    </span>
                                                </Menu.Item>
                                            );
                                        })}
                                    </Menu.Dropdown>
                                </Menu>
                            )}
                        </Group>
                    </Group>
                )}
                <ScrollArea flex={1} mih={0}>
                    {sortedViews.length > 0 ? (
                        <DndContext
                            sensors={sensors}
                            collisionDetection={closestCenter}
                            onDragEnd={handleDragEnd}
                            modifiers={[restrictToParentElement]}
                        >
                            <SortableContext
                                items={sortedViews.map((v) => v.id)}
                                strategy={verticalListSortingStrategy}
                            >
                                <Stack gap="md">
                                    {sortedViews.map((view) => (
                                        <SortableViewCard
                                            key={view.id}
                                            view={view}
                                            color={props.tracker.color}
                                            isReordering={
                                                isReordering && isOwner
                                            }
                                            onDetails={(v) => {
                                                setSelectedView(v);
                                                setOpenDialogType(
                                                    OpenDialogType.ViewDetails,
                                                );
                                            }}
                                            onEdit={(v) => {
                                                setSelectedView(v);
                                                setOpenDialogType(
                                                    OpenDialogType.EditView,
                                                );
                                            }}
                                            onDelete={(v) => {
                                                setSelectedView(v);
                                                setOpenDialogType(
                                                    OpenDialogType.DeleteView,
                                                );
                                            }}
                                        />
                                    ))}
                                </Stack>
                            </SortableContext>
                        </DndContext>
                    ) : (
                        <EmptyState
                            title="No views yet"
                            hint="A view is a saved combination of filters and sorting you can switch between."
                        />
                    )}
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

            {selectedView && openDialogType === OpenDialogType.EditView && (
                <ViewFormDialog
                    tracker={props.tracker}
                    viewId={selectedView.id}
                    initialView={selectedView}
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedView(undefined);
                    }}
                />
            )}

            {selectedView && openDialogType === OpenDialogType.DeleteView && (
                <ConfirmationDialog
                    isOpen={selectedView !== undefined}
                    onClose={() => setOpenDialogType(undefined)}
                    onConfirm={async () => {
                        if (selectedView) {
                            await deleteView(selectedView.id);
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
