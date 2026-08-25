import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
    ActionIcon,
    Badge,
    Card,
    Group,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { CSSProperties } from "react";
import { CiBoxList } from "react-icons/ci";
import { MdDelete, MdDragHandle, MdEdit } from "react-icons/md";
import { useTracker } from "../../trackers/context/TrackerContext";
import { ViewDto } from "../types/ViewDto";

interface SortableViewCardProps {
    view: ViewDto;
    color?: string;
    isReordering: boolean;
    onDetails: (view: ViewDto) => void;
    onEdit: (view: ViewDto) => void;
    onDelete: (view: ViewDto) => void;
}

export default function SortableViewCard({
    view,
    color,
    isReordering,
    onDetails,
    onEdit,
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

    const { canEditSchema } = useTracker();

    const filterCount = view.queries.reduce(
        (sum, q) => sum + q.filters.length,
        0,
    );
    const sortCount = view.queries.reduce((sum, q) => sum + q.sorts.length, 0);

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
                <Stack gap="xs" flex={1}>
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
                    {(view.queries.length > 0 ||
                        filterCount > 0 ||
                        sortCount > 0) && (
                        <Group wrap="wrap">
                            <Badge variant="light" color="gray" size="sm">
                                {view.queries.length}{" "}
                                {view.queries.length === 1
                                    ? "query"
                                    : "queries"}
                            </Badge>
                            {filterCount > 0 && (
                                <Badge variant="light" color="blue" size="sm">
                                    {filterCount}{" "}
                                    {filterCount === 1 ? "filter" : "filters"}
                                </Badge>
                            )}
                            {sortCount > 0 && (
                                <Badge variant="light" color="teal" size="sm">
                                    {sortCount}{" "}
                                    {sortCount === 1 ? "sort" : "sorts"}
                                </Badge>
                            )}
                        </Group>
                    )}
                </Stack>
                <Group gap="xs" wrap="nowrap">
                    <ActionIcon
                        variant="outline"
                        color={color}
                        size="lg"
                        onClick={() => onDetails(view)}
                        aria-label={`View details ${view.name}`}
                    >
                        <CiBoxList size={16} />
                    </ActionIcon>
                    {canEditSchema && (
                        <>
                            <ActionIcon
                                variant="outline"
                                color="green"
                                size="lg"
                                onClick={() => onEdit(view)}
                                aria-label={`Edit view ${view.name}`}
                            >
                                <MdEdit size={16} />
                            </ActionIcon>
                            <ActionIcon
                                variant="outline"
                                color="red"
                                size="lg"
                                onClick={() => onDelete(view)}
                                aria-label={`Delete view ${view.name}`}
                            >
                                <MdDelete size={16} />
                            </ActionIcon>
                        </>
                    )}
                </Group>
            </Group>
        </Card>
    );
}
