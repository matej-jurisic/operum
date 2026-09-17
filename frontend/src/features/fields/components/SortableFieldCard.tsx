import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
    ActionIcon,
    Badge,
    Card,
    Checkbox,
    Group,
    Stack,
    Text,
    Title,
    Tooltip,
} from "@mantine/core";
import { CSSProperties } from "react";
import { MdDelete, MdDragHandle, MdEdit } from "react-icons/md";
import { useTracker } from "../../trackers/context/TrackerContext";
import { FieldDto } from "../types/FieldDto";

interface SortableFieldCardProps {
    field: FieldDto;
    onEdit: (field: FieldDto) => void;
    onDelete: (field: FieldDto) => void;
    color?: string;
    isReordering: boolean;
    isSelecting?: boolean;
    selected?: boolean;
    selectableReason?: string;
    onToggleSelect?: (field: FieldDto) => void;
}

export default function SortableFieldCard({
    field,
    color,
    isReordering,
    isSelecting,
    selected,
    selectableReason,
    onToggleSelect,
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

    const disabledSelect = isSelecting && !!selectableReason;
    const handleCardClick = () => {
        if (isSelecting && !disabledSelect) onToggleSelect?.(field);
    };

    return (
        <Card
            ref={setNodeRef}
            style={{
                ...style,
                cursor:
                    isSelecting && !disabledSelect ? "pointer" : style.cursor,
            }}
            p="md"
            radius="md"
            withBorder
            onClick={handleCardClick}
        >
            <Group align="flex-start" justify="space-between" wrap="nowrap">
                {isSelecting && (
                    <Tooltip
                        label={selectableReason}
                        disabled={!disabledSelect}
                        multiline
                        w={220}
                    >
                        <Checkbox
                            checked={!!selected}
                            disabled={disabledSelect}
                            onChange={() => onToggleSelect?.(field)}
                            onClick={(e) => e.stopPropagation()}
                            color={color}
                            style={{ alignSelf: "center" }}
                            aria-label={`Select field ${field.name}`}
                        />
                    </Tooltip>
                )}
                {isReordering && (
                    <ActionIcon
                        variant="outline"
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

                {canEditSchema && !isSelecting && (
                    <Group gap="xs" wrap="nowrap">
                        <ActionIcon
                            variant="outline"
                            color="green"
                            size="lg"
                            onClick={() => onEdit(field)}
                            aria-label={`Edit field ${field.name}`}
                        >
                            <MdEdit size={16} />
                        </ActionIcon>
                        <ActionIcon
                            variant="outline"
                            color="red"
                            size="lg"
                            onClick={() => onDelete(field)}
                            aria-label={`Delete field ${field.name}`}
                        >
                            <MdDelete size={16} />
                        </ActionIcon>
                    </Group>
                )}
            </Group>
        </Card>
    );
}
