import { Badge, Button, Card, Group, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { FieldDto } from "../model/FieldDto";
import { FieldUpsertDto } from "../model/requests/FieldUpsertDto";
import { TrackerDto } from "../model/TrackerDto";
import ConfirmationDialog from "./ConfirmationDialog";
import { FieldFormDialog } from "./CreateFieldDialog";

interface FieldsListProps {
    tracker: TrackerDto;
}

const DeleteField = async (trackerId: string, fieldId: string) => {
    await api.delete(`/trackers/${trackerId}/fields/${fieldId}`);
};

enum OpenDialogType {
    CreateField,
    DeleteField,
    EditField,
}

export default function FieldsList(props: FieldsListProps) {
    const [selectedField, setSelectedField] = useState<FieldDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    const {
        fields,
        refreshFields,
        markEntriesDirty,
        markAnalyticsDirty,
        refreshFieldsIfDirty,
    } = useTracker();

    useEffect(() => {
        refreshFieldsIfDirty();
    }, []);

    return (
        <>
            <Stack gap="md">
                <Group justify="space-between" w="100%">
                    <Button
                        color={props.tracker.color}
                        leftSection={<FiPlus size={18} />}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateField)
                        }
                    >
                        Create Field
                    </Button>
                </Group>
                {fields.map((field) => (
                    <Card key={field.id} p="md" radius="md" withBorder>
                        <Group p="apart" align="center" justify="flex-end">
                            <Stack gap={2} style={{ flex: 1 }} miw={"150px"}>
                                <Text fw={600} size="md" lineClamp={1}>
                                    {field.name}
                                </Text>
                                <Text
                                    c="dimmed"
                                    size="sm"
                                    style={{
                                        whiteSpace: "normal",
                                        wordBreak: "break-word",
                                    }}
                                    lineClamp={3}
                                >
                                    {field.description || "No description"}
                                </Text>
                            </Stack>
                            <Group justify="flex-end" w={250}>
                                <Group justify="flex-end">
                                    {field.required && (
                                        <Badge variant="light" color="red">
                                            Required
                                        </Badge>
                                    )}
                                    <Badge variant="light" color="blue">
                                        {field.type}
                                    </Badge>
                                </Group>
                                <Button
                                    variant="outline"
                                    color="red"
                                    onClick={() => {
                                        setSelectedField(field);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteField
                                        );
                                    }}
                                    aria-label={`Delete field ${field.name}`}
                                >
                                    <MdDelete size={18} />
                                </Button>
                                <Button
                                    variant="outline"
                                    color="green"
                                    onClick={() => {
                                        setSelectedField(field);
                                        setOpenDialogType(
                                            OpenDialogType.EditField
                                        );
                                    }}
                                    aria-label={`Edit field ${field.name}`}
                                >
                                    <MdEdit size={18} />
                                </Button>
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
                        markEntriesDirty();
                        markAnalyticsDirty();
                        refreshFields();
                    }}
                    severity="important"
                    message="Deleting a field will delete all the data stored in it."
                />
            )}
            {openDialogType === OpenDialogType.CreateField && (
                <FieldFormDialog
                    tracker={props.tracker}
                    onClose={() => setOpenDialogType(undefined)}
                    onFieldSaved={async () => {
                        refreshFields();
                    }}
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
                    onFieldSaved={async () => {
                        setSelectedField(undefined);
                        setOpenDialogType(undefined);
                        markAnalyticsDirty();
                        markEntriesDirty();
                        refreshFields();
                    }}
                />
            )}
        </>
    );
}
