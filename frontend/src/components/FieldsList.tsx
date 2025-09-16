import {
    closestCenter,
    DndContext,
    DragEndEvent,
    KeyboardSensor,
    PointerSensor,
    TouchSensor,
    useSensor,
    useSensors,
} from "@dnd-kit/core";
import { restrictToParentElement } from "@dnd-kit/modifiers";
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    useSortable,
    verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Group,
    Paper,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { CSSProperties, useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdDragHandle, MdEdit } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import { FieldDto } from "../model/FieldDto";
import { FieldUpsertDto } from "../model/requests/FieldUpsertDto";
import { TrackerDto } from "../model/TrackerDto";
import ConfirmationDialog from "./ConfirmationDialog";
import { FieldFormDialog } from "./FieldFormDialog";

interface FieldsListProps {
    tracker: TrackerDto;
}

enum OpenDialogType {
    CreateField,
    DeleteField,
    EditField,
}

// Sortable Field Card Component
interface SortableFieldCardProps {
    field: FieldDto;
    onEdit: (field: FieldDto) => void;
    onDelete: (field: FieldDto) => void;
    color?: string;
}

function SortableFieldCard({
    field,
    color,
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
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    } as CSSProperties;

    return (
        <Card ref={setNodeRef} style={style} p="md" radius="md" withBorder>
            <Group align="flex-start" justify="space-between" wrap="nowrap">
                {/* Drag handle */}
                <ActionIcon
                    variant="subtle"
                    color={color}
                    size="lg"
                    {...attributes}
                    {...listeners}
                    style={{
                        cursor: "grab",
                        alignSelf: "center",
                        touchAction: "none",
                    }}
                    aria-label={`Drag to reorder field ${field.name}`}
                >
                    <MdDragHandle size={16} />
                </ActionIcon>

                {/* Field info section */}
                <Stack gap={"xs"} flex={1}>
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
                        <Badge variant="light" color="blue" size="sm">
                            {field.type}
                        </Badge>
                    </Group>
                </Stack>

                {/* Action buttons */}
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
            </Group>
        </Card>
    );
}

export default function FieldsList(props: FieldsListProps) {
    const [selectedField, setSelectedField] = useState<FieldDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [sortedFields, setSortedFields] = useState<FieldDto[]>([]);

    const { fields, DeleteField, refreshFieldsIfDirty, UpdateFieldOrder } =
        useTracker();

    const sensors = useSensors(
        useSensor(PointerSensor, {
            activationConstraint: {
                distance: 8,
            },
        }),
        useSensor(TouchSensor, {
            activationConstraint: {
                delay: 200,
                tolerance: 5,
            },
        }),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates,
        })
    );

    useEffect(() => {
        refreshFieldsIfDirty();
    }, []);

    useEffect(() => {
        setSortedFields([...fields]);
    }, [fields]);

    const handleDragEnd = async (event: DragEndEvent) => {
        const { active, over } = event;

        if (over && active.id !== over.id) {
            const oldIndex = sortedFields.findIndex(
                (field) => field.id === active.id
            );
            const newIndex = sortedFields.findIndex(
                (field) => field.id === over.id
            );

            const newSortedFields = arrayMove(sortedFields, oldIndex, newIndex);
            setSortedFields(newSortedFields);
            try {
                await UpdateFieldOrder(
                    props.tracker.id,
                    newSortedFields.map((f) => f.id)
                );
            } catch (error) {
                setSortedFields([...fields]);
                console.error("Failed to update field order:", error);
            }
        }
    };

    const handleEdit = (field: FieldDto) => {
        setSelectedField(field);
        setOpenDialogType(OpenDialogType.EditField);
    };

    const handleDelete = (field: FieldDto) => {
        setSelectedField(field);
        setOpenDialogType(OpenDialogType.DeleteField);
    };

    return (
        <>
            <Stack gap="md">
                <Group justify="space-between" w="100%" h={36}>
                    <Button
                        color={props.tracker.color}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateField)
                        }
                        leftSection={<FiPlus size={18} />}
                    >
                        Create
                    </Button>
                </Group>

                {sortedFields.length > 0 ? (
                    <DndContext
                        sensors={sensors}
                        collisionDetection={closestCenter}
                        onDragEnd={handleDragEnd}
                        modifiers={[restrictToParentElement]}
                    >
                        <SortableContext
                            items={sortedFields.map((field) => field.id)}
                            strategy={verticalListSortingStrategy}
                        >
                            <Stack gap="md">
                                {sortedFields.map((field) => (
                                    <SortableFieldCard
                                        key={field.id}
                                        color={props.tracker.color}
                                        field={field}
                                        onEdit={handleEdit}
                                        onDelete={handleDelete}
                                    />
                                ))}
                            </Stack>
                        </SortableContext>
                    </DndContext>
                ) : (
                    <Paper withBorder p="xl" radius="md">
                        <Stack gap="md" align="center">
                            <Text size="lg" fw={500} c="dimmed">
                                No Fields Available
                            </Text>
                            <Text ta="center" c="dimmed">
                                Fields will appear here once you create them.
                            </Text>
                        </Stack>
                    </Paper>
                )}
            </Stack>

            {selectedField && openDialogType === OpenDialogType.DeleteField && (
                <ConfirmationDialog
                    isOpen={selectedField !== undefined}
                    onClose={() => {
                        setSelectedField(undefined);
                        setOpenDialogType(undefined);
                    }}
                    onConfirm={async () => {
                        await DeleteField(props.tracker.id, selectedField.id);
                        setSelectedField(undefined);
                        setOpenDialogType(undefined);
                    }}
                    severity="important"
                    message="Deleting a field will delete all the data stored in it."
                />
            )}
            {openDialogType === OpenDialogType.CreateField && (
                <FieldFormDialog
                    tracker={props.tracker}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
            {openDialogType === OpenDialogType.EditField && selectedField && (
                <FieldFormDialog
                    tracker={props.tracker}
                    fieldId={selectedField.id}
                    initialValues={{ ...selectedField } as FieldUpsertDto}
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedField(undefined);
                    }}
                />
            )}
        </>
    );
}
