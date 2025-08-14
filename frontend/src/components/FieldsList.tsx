import { Badge, Button, Card, Group, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { FieldDto } from "../model/FieldDto";
import { TrackerDto } from "../model/TrackerDto";
import ConfirmationDialog from "./ConfirmationDialog";
import { CreateFieldDialog } from "./CreateFieldDialog";

interface FieldsListProps {
    tracker: TrackerDto;
    refreshTracker: () => void;
}

const DeleteField = async (trackerId: string, fieldId: string) => {
    await api.delete(`/trackers/${trackerId}/fields/${fieldId}`);
};

const GetFields = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/fields`);
    return response.data.data;
};

enum OpenDialogType {
    CreateField,
    DeleteField,
}

export default function FieldsList(props: FieldsListProps) {
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [selectedField, setSelectedField] = useState<FieldDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    useEffect(() => {
        const GetData = async () => {
            setFields(await GetFields(props.tracker.id));
        };

        GetData();
    }, [props.tracker.id]);

    return (
        <>
            <Stack gap="md">
                <Group justify="space-between" w="100%">
                    <Button
                        color={props.tracker.color}
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
                        setFields(await GetFields(props.tracker.id));
                        props.refreshTracker();
                    }}
                    severity="important"
                    message="Deleting a field will delete all the data stored in it."
                />
            )}
            {openDialogType === OpenDialogType.CreateField && (
                <CreateFieldDialog
                    tracker={props.tracker}
                    onClose={() => setOpenDialogType(undefined)}
                    onFieldAdded={async () => {
                        setFields(await GetFields(props.tracker.id));
                        props.refreshTracker();
                    }}
                />
            )}
        </>
    );
}
