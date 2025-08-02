import { Badge, Card, Group, Modal, Stack, Text } from "@mantine/core";
import { Dispatch, SetStateAction, useEffect, useState } from "react";
import api from "../api/api";
import { FieldDto } from "../model/FieldDto";
import { TrackerDto } from "../model/TrackerDto";

interface ViewFieldsDialogProps {
    tracker: TrackerDto;
    onClose: () => void;
}

const GetFields = async (
    trackerId: string,
    setFields: Dispatch<SetStateAction<FieldDto[]>>
) => {
    const response = await api.get(`/trackers/${trackerId}/fields`);
    setFields(response.data.data);
};

export default function ViewFieldsDialog({
    tracker,
    onClose,
}: ViewFieldsDialogProps) {
    const [fields, setFields] = useState<FieldDto[]>([]);

    useEffect(() => {
        GetFields(tracker.id, setFields);
    }, [tracker.id]);

    if (fields.length === 0) {
        return <></>;
    }

    return (
        <Modal
            opened
            onClose={onClose}
            title={`${tracker.name} fields`}
            centered
            size="lg"
        >
            <Stack>
                {fields.map((field) => (
                    <Card
                        key={field.id}
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                    >
                        <Group justify="space-between">
                            <div>
                                <Text fw={500} size="md">
                                    {field.name}
                                </Text>
                                <Text size="sm" c="dimmed">
                                    {field.description || "No description"}
                                </Text>
                            </div>

                            <Badge variant="light" color="blue">
                                {field.type}
                            </Badge>
                        </Group>
                    </Card>
                ))}
            </Stack>
        </Modal>
    );
}
