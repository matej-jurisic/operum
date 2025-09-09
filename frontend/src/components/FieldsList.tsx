import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Group,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
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

export default function FieldsList(props: FieldsListProps) {
    const [selectedField, setSelectedField] = useState<FieldDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    const { fields, DeleteField, refreshFieldsIfDirty } = useTracker();

    useEffect(() => {
        refreshFieldsIfDirty();
    }, []);

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
                {fields.map((field) => (
                    <Card key={field.id} p="md" radius="md" withBorder>
                        {/* Option 1: Stacked layout for mobile */}
                        <Group
                            align="flex-start"
                            justify="space-between"
                            wrap="nowrap"
                        >
                            {/* Field info section */}
                            <Stack gap={"sm"} flex={1}>
                                <Title
                                    order={4}
                                    lineClamp={1}
                                    className="wrapped-text"
                                >
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
                                        <Badge
                                            variant="light"
                                            color="red"
                                            size="sm"
                                        >
                                            Required
                                        </Badge>
                                    )}
                                    <Badge
                                        variant="light"
                                        color="blue"
                                        size="sm"
                                    >
                                        {field.type}
                                    </Badge>
                                </Group>
                            </Stack>

                            {/* Badges and actions section - always horizontal */}

                            <Group gap="xs" wrap="nowrap">
                                <ActionIcon
                                    variant="outline"
                                    color="green"
                                    size="lg"
                                    onClick={() => {
                                        setSelectedField(field);
                                        setOpenDialogType(
                                            OpenDialogType.EditField
                                        );
                                    }}
                                    aria-label={`Edit field ${field.name}`}
                                >
                                    <MdEdit size={16} />
                                </ActionIcon>
                                <ActionIcon
                                    variant="outline"
                                    color="red"
                                    size="lg"
                                    onClick={() => {
                                        setSelectedField(field);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteField
                                        );
                                    }}
                                    aria-label={`Delete field ${field.name}`}
                                >
                                    <MdDelete size={16} />
                                </ActionIcon>
                            </Group>
                        </Group>
                    </Card>
                ))}
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
