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
import { CiEdit, CiTrash } from "react-icons/ci";
import { MdDragHandle } from "react-icons/md";
import { useTracker } from "../../trackers/context/TrackerContext";
import { FieldDto } from "../types/FieldDto";

// Sortable Field Card Component
interface SortableFieldCardProps {
    field: FieldDto;
    onEdit: (field: FieldDto) => void;
    onDelete: (field: FieldDto) => void;
    color?: string;
    isReordering: boolean;
}

export default function SortableFieldCard({
    field,
    color,
    isReordering,
    onEdit,
    onDelete,
}: SortableFieldCardProps) {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition,
        isDragging,
    } = useSortable({ id: field.id });

    const style = {
        transform: CSS.Translate.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    } as CSSProperties;

    const { canEditSchema } = useTracker();

    return (
        <Card ref={setNodeRef} style={style} p="md" radius="md" withBorder>
            <Group align="flex-start" justify="space-between" wrap="nowrap">
                {/* Drag handle */}
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
                        aria-label={`Drag to reorder field ${field.name}`}
                    >
                        <MdDragHandle size={25} />
                    </ActionIcon>
                )}
                {/* Field info section */}
                <Stack gap="xs" flex={1}>
                    <Title order={4} lineClamp={1} className="wrapped-text">
                        {field.name}
                    </Title>
                    <Text
                        c="dimmed"
                        size="sm"
                        lineClamp={3}
                        className="wrapped-text"
                    >
                        {field.description || "No description"}
                    </Text>
                    <Group wrap="wrap">
                        {field.required && (
                            <Badge variant="light" color="red" size="sm">
                                Required
                            </Badge>
                        )}
                        {field.isCalculated && (
                            <Badge variant="light" color="violet" size="sm">
                                Calculated
                            </Badge>
                        )}
                        <Badge variant="light" color="blue" size="sm">
                            {field.type}
                        </Badge>
                    </Group>
                </Stack>

                {/* Action buttons */}
                {canEditSchema && (
                    <Group gap="xs" wrap="nowrap">
                        <ActionIcon
                            variant="outline"
                            color="green"
                            size="lg"
                            onClick={() => onEdit(field)}
                            aria-label={`Edit field ${field.name}`}
                        >
                            <CiEdit size={16} />
                        </ActionIcon>
                        <ActionIcon
                            variant="outline"
                            color="red"
                            size="lg"
                            onClick={() => onDelete(field)}
                            aria-label={`Delete field ${field.name}`}
                        >
                            <CiTrash size={16} />
                        </ActionIcon>
                    </Group>
                )}
            </Group>
        </Card>
    );
}
