import {
    Badge,
    Button,
    Card,
    Group,
    Modal,
    ScrollArea,
    Stack,
    Text,
} from "@mantine/core";
import { Dispatch, SetStateAction, useEffect, useState } from "react";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { FieldDto } from "../model/FieldDto";
import { TrackerDto } from "../model/TrackerDto";
import ConfirmationDialog from "./ConfirmationDialog";

interface ViewFieldsDialogProps {
    tracker: TrackerDto;
    onClose: (withReload: boolean) => void;
}

const GetFields = async (
    trackerId: string,
    setFields: Dispatch<SetStateAction<FieldDto[]>>
) => {
    const response = await api.get(`/trackers/${trackerId}/fields`);
    setFields(response.data.data);
};

const DeleteField = async (fieldId: string) => {
    await api.delete(`/trackers/fields/${fieldId}`);
};

export default function ViewFieldsDialog({
    tracker,
    onClose,
}: ViewFieldsDialogProps) {
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [fieldToDelete, setFieldToDelete] = useState<FieldDto>();
    const [shouldReloadEntries, setShouldReloadEntries] = useState(false);

    useEffect(() => {
        GetFields(tracker.id, setFields);
    }, [tracker.id]);

    if (fields.length === 0) {
        return <></>;
    }

    return (
        <>
            <Modal
                opened
                onClose={() => onClose(shouldReloadEntries)}
                title={`${tracker.name} Fields`}
                centered
                size="lg"
            >
                <ScrollArea style={{ maxHeight: "60vh" }} scrollbarSize={6}>
                    <Stack gap="md" p={"sm"}>
                        {fields.map((field) => (
                            <Card
                                key={field.id}
                                shadow="xs"
                                padding="sm"
                                radius="md"
                                withBorder
                            >
                                <Group p="apart" align="center" wrap="nowrap">
                                    <Stack
                                        gap={2}
                                        style={{ flex: 1, minWidth: 0 }}
                                    >
                                        <Text fw={600} size="md" lineClamp={1}>
                                            {field.name}
                                        </Text>
                                        <Text
                                            size="sm"
                                            c="dimmed"
                                            lineClamp={2}
                                        >
                                            {field.description ||
                                                "No description"}
                                        </Text>
                                    </Stack>
                                    <Group gap="xs" wrap="nowrap">
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
                                        <Button
                                            size="xs"
                                            variant="outline"
                                            color="red"
                                            onClick={() =>
                                                setFieldToDelete(field)
                                            }
                                            aria-label={`Delete field ${field.name}`}
                                        >
                                            <MdDelete size={18} />
                                        </Button>
                                    </Group>
                                </Group>
                            </Card>
                        ))}
                    </Stack>
                </ScrollArea>
            </Modal>

            {fieldToDelete && (
                <ConfirmationDialog
                    isOpen={fieldToDelete !== undefined}
                    onClose={() => setFieldToDelete(undefined)}
                    onConfirm={async () => {
                        await DeleteField(fieldToDelete.id);
                        setFieldToDelete(undefined);
                        await GetFields(tracker.id, setFields);
                        setShouldReloadEntries(true);
                    }}
                    severity="important"
                    message="Deleting a field will delete all the data stored in it."
                />
            )}
        </>
    );
}
