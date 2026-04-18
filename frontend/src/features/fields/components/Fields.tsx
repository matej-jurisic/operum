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
    Paper,
    ScrollArea,
    Stack,
    Text,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { RiListOrdered2 } from "react-icons/ri";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useFields } from "../context/FieldsContext";
import { FieldDto } from "../types/FieldDto";
import { FieldFormDialog } from "./FieldFormDialog";
import SortableFieldCard from "./SortableFieldCard";

interface FieldsProps {
    tracker: TrackerDto;
}

enum OpenDialogType {
    CreateField,
    DeleteField,
    EditField,
}

export default function Fields(props: FieldsProps) {
    const [selectedField, setSelectedField] = useState<FieldDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [sortedFields, setSortedFields] = useState<FieldDto[]>([]);
    const [isReordering, setIsReordering] = useState(false);

    const { deleteField, updateFieldOrder } = useTrackerOperations();
    const { fields, refreshFieldsIfDirty } = useFields();
    const { canEditSchema } = useTracker();

    const sensors = useSensors(
        useSensor(PointerSensor, {
            activationConstraint: {
                distance: 4,
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
                await updateFieldOrder(newSortedFields.map((f) => f.id));
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
            <Stack gap="md" h={"100%"}>
                {canEditSchema && (
                    <Group justify="space-between" w="100%">
                        <Button
                            color={props.tracker.color}
                            variant="outline"
                            onClick={() =>
                                setOpenDialogType(OpenDialogType.CreateField)
                            }
                            leftSection={<FiPlus size={18} />}
                        >
                            Create
                        </Button>
                        <Group>
                            <ActionIcon
                                size={"lg"}
                                variant={isReordering ? "filled" : "outline"}
                                onClick={() => setIsReordering((prev) => !prev)}
                                color={props.tracker.color}
                            >
                                <RiListOrdered2 size={18} />
                            </ActionIcon>
                        </Group>
                    </Group>
                )}

                <ScrollArea flex={1} mih={0}>
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
                                            isReordering={isReordering}
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
                                    Fields will appear here once you create
                                    them.
                                </Text>
                            </Stack>
                        </Paper>
                    )}
                </ScrollArea>
            </Stack>

            {selectedField && openDialogType === OpenDialogType.DeleteField && (
                <ConfirmationDialog
                    isOpen={selectedField !== undefined}
                    onClose={() => {
                        setSelectedField(undefined);
                        setOpenDialogType(undefined);
                    }}
                    onConfirm={async () => {
                        await deleteField(selectedField.id);
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
                    initialValues={{ ...selectedField }}
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedField(undefined);
                    }}
                />
            )}
        </>
    );
}
