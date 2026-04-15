import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
    ActionIcon,
    Card,
    Group,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { CSSProperties } from "react";
import { MdDelete, MdDragHandle } from "react-icons/md";
import { RiFileListFill } from "react-icons/ri";
import globalStore from "../../../shared/stores/GlobalStore";
import { useTracker } from "../../trackers/context/TrackerContext";
import { ViewDto } from "../types/ViewDto";

interface SortableViewCardProps {
    view: ViewDto;
    color?: string;
    isReordering: boolean;
    onDetails: (view: ViewDto) => void;
    onDelete: (view: ViewDto) => void;
}

export default function SortableViewCard({
    view,
    color,
    isReordering,
    onDetails,
    onDelete,
}: SortableViewCardProps) {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition,
        isDragging,
    } = useSortable({ id: view.id });

    const style = {
        transform: CSS.Translate.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    } as CSSProperties;

    const { tracker } = useTracker();

    return (
        <Card ref={setNodeRef} style={style} p="md" radius="md" withBorder>
            <Group align="flex-start" justify="space-between" wrap="nowrap">
                {isReordering && (
                    <ActionIcon
                        variant="subtle"
                        color={color}
                        size="xl"
                        {...attributes}
                        {...listeners}
                        style={{
                            cursor: "grab",
                            alignSelf: "center",
                            touchAction: "none",
                        }}
                        aria-label={`Drag to reorder view ${view.name}`}
                    >
                        <MdDragHandle size={25} />
                    </ActionIcon>
                )}
                <Stack gap="sm" flex={1}>
                    <Title order={4} lineClamp={1} className="wrapped-text">
                        {view.name}
                    </Title>
                    <Text
                        c="dimmed"
                        size="sm"
                        lineClamp={3}
                        className="wrapped-text"
                    >
                        {view.description || "No description"}
                    </Text>
                </Stack>
                <Group gap="xs" wrap="nowrap">
                    <ActionIcon
                        variant="outline"
                        color={color}
                        size="lg"
                        onClick={() => onDetails(view)}
                        aria-label={`View details ${view.name}`}
                    >
                        <RiFileListFill size={16} />
                    </ActionIcon>
                    {globalStore.currentUser?.id === tracker.ownerId && (
                        <ActionIcon
                            variant="outline"
                            color="red"
                            size="lg"
                            onClick={() => onDelete(view)}
                            aria-label={`Delete view ${view.name}`}
                        >
                            <MdDelete size={16} />
                        </ActionIcon>
                    )}
                </Group>
            </Group>
        </Card>
    );
}
