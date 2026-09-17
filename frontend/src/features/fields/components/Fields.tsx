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
    ScrollArea,
    Stack,
    Tooltip,
} from "@mantine/core";
import { notifications } from "@mantine/notifications";
import { useEffect, useState } from "react";
import { CiBoxList } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { TbArrowsSplit } from "react-icons/tb";
import { useNavigate } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import EmptyState from "../../../shared/components/EmptyState";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useTracker } from "../../trackers/context/TrackerContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useFields } from "../context/FieldsContext";
import { FieldDto } from "../types/FieldDto";
import { ExtractFieldsDialog } from "./ExtractFieldsDialog";
import { FieldFormDialog } from "./FieldFormDialog";
import SortableFieldCard from "./SortableFieldCard";

interface FieldsProps {
    tracker: TrackerDto;
}

enum OpenDialogType {
    CreateField,
    DeleteField,
    EditField,
    ExtractFields,
}

function extractBlockReason(field: FieldDto): string | undefined {
    if (field.isCalculated) return "Calculated fields cannot be extracted.";
    if (field.type === "reference")
        return "Reference fields cannot be extracted.";
    return undefined;
}

export default function Fields(props: FieldsProps) {
    const [selectedField, setSelectedField] = useState<FieldDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [sortedFields, setSortedFields] = useState<FieldDto[]>([]);
    const [isReordering, setIsReordering] = useState(false);
    const [isSelecting, setIsSelecting] = useState(false);
    const [selectedIds, setSelectedIds] = useState<string[]>([]);

    const { deleteField, updateFieldOrder } = useTrackerOperations();
    const { fields, refreshFieldsIfDirty } = useFields();
    const { canEditSchema } = useTracker();
    const navigate = useNavigate();

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

    // A field can drop out of the tracker while it is ticked (deleted in another tab, say).
    useEffect(() => {
        setSelectedIds((prev) =>
            prev.filter((id) => fields.some((f) => f.id === id))
        );
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

    const toggleSelect = (field: FieldDto) => {
        setSelectedIds((prev) =>
            prev.includes(field.id)
                ? prev.filter((id) => id !== field.id)
                : [...prev, field.id]
        );
    };

    const startSelecting = () => {
        setIsReordering(false);
        setIsSelecting(true);
        setSelectedIds([]);
    };

    const stopSelecting = () => {
        setIsSelecting(false);
        setSelectedIds([]);
    };

    const selectedFields = selectedIds
        .map((id) => fields.find((f) => f.id === id))
        .filter((f): f is FieldDto => f !== undefined);

    return (
        <>
            <Stack gap="md" h={"100%"}>
                {canEditSchema && (
                    <Group justify="space-between" w="100%">
                        {isSelecting ? (
                            <Group>
                                <Button
                                    variant="default"
                                    onClick={stopSelecting}
                                >
                                    Cancel
                                </Button>
                                <Button
                                    color={props.tracker.color}
                                    onClick={() =>
                                        setOpenDialogType(
                                            OpenDialogType.ExtractFields
                                        )
                                    }
                                    disabled={selectedIds.length === 0}
                                >
                                    Extract
                                    {selectedIds.length > 0 &&
                                        ` (${selectedIds.length})`}
                                </Button>
                            </Group>
                        ) : (
                            <>
                                <Button
                                    color={props.tracker.color}
                                    variant="outline"
                                    onClick={() =>
                                        setOpenDialogType(
                                            OpenDialogType.CreateField
                                        )
                                    }
                                    leftSection={<FiPlus size={18} />}
                                >
                                    Create
                                </Button>
                                <Group>
                                    <Tooltip label="Extract fields to a new tracker">
                                        <ActionIcon
                                            size={"lg"}
                                            variant="outline"
                                            onClick={startSelecting}
                                            color={props.tracker.color}
                                            aria-label="Extract fields to a new tracker"
                                        >
                                            <TbArrowsSplit size={18} />
                                        </ActionIcon>
                                    </Tooltip>
                                    <ActionIcon
                                        size={"lg"}
                                        variant={
                                            isReordering ? "filled" : "outline"
                                        }
                                        onClick={() =>
                                            setIsReordering((prev) => !prev)
                                        }
                                        color={props.tracker.color}
                                        aria-label="Reorder fields"
                                    >
                                        <CiBoxList size={18} />
                                    </ActionIcon>
                                </Group>
                            </>
                        )}
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
                                            isReordering={
                                                isReordering && !isSelecting
                                            }
                                            isSelecting={isSelecting}
                                            selected={selectedIds.includes(
                                                field.id
                                            )}
                                            selectableReason={extractBlockReason(
                                                field
                                            )}
                                            onToggleSelect={toggleSelect}
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
                        <EmptyState
                            title="No fields yet"
                            hint="Fields define what each entry records. Add one to start capturing data."
                        />
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
            {openDialogType === OpenDialogType.ExtractFields &&
                selectedFields.length > 0 && (
                    <ExtractFieldsDialog
                        tracker={props.tracker}
                        fields={selectedFields}
                        onClose={() => setOpenDialogType(undefined)}
                        onExtracted={(result) => {
                            setOpenDialogType(undefined);
                            stopSelecting();
                            notifications.show({
                                title: "Fields extracted",
                                message: `${result.newTrackerName} now holds ${result.extractedEntryCount} rows.`,
                            });
                            navigate(
                                `/trackers/${result.newTrackerId}/fields`
                            );
                        }}
                    />
                )}
        </>
    );
}
