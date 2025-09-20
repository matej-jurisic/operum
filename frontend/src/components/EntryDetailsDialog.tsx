import {
    Badge,
    Divider,
    Group,
    Modal,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import api from "../api/api";
import { EntryDto } from "../model/EntryDto";
import { TrackerDto } from "../model/TrackerDto";
import { formatDateTimeFromDate } from "../util/TypeFormatter";
import { renderValue } from "../util/ValueRenderer";

interface EntryDetailsDialogProps {
    onClose: () => void;
    entryId: string;
    tracker: TrackerDto;
}

const GetEntry = async (trackerId: string, entryId: string) => {
    const response = await api.get(`/trackers/${trackerId}/entries/${entryId}`);
    return response.data.data;
};

export default function EntryDetailsDialog({
    onClose,
    entryId,
    tracker,
}: EntryDetailsDialogProps) {
    const [entry, setEntry] = useState<EntryDto>();

    useEffect(() => {
        const fetchEntry = async () => {
            setEntry(await GetEntry(tracker.id, entryId));
        };

        if (entryId) {
            fetchEntry();
        }
    }, [entryId, tracker.id]);

    if (!entry) return null;

    return (
        <Modal
            opened
            centered
            onClose={onClose}
            title={
                <Group justify="space-between" wrap="nowrap" mr={"xs"}>
                    <Title order={4} className="wrapped-text" lineClamp={3}>
                        {tracker.name}
                    </Title>
                    <Badge
                        color={tracker.color}
                        variant="filled"
                        miw="max-content"
                    >
                        Entry Details
                    </Badge>
                </Group>
            }
        >
            <Stack gap="sm">
                <Divider label="Data" />
                {entry.fieldValues.map((fieldValue) => (
                    <Group
                        key={fieldValue.fieldId}
                        justify="space-between"
                        wrap="nowrap"
                    >
                        <Text fw={500} maw={"50%"} className="wrapped-text">
                            {fieldValue.fieldName}
                        </Text>
                        <Text w={"50%"} className="wrapped-text">
                            {renderValue(
                                fieldValue.fieldType,
                                fieldValue.value
                            )}
                        </Text>
                    </Group>
                ))}
                <Divider label="Information" />
                <Group justify="space-between">
                    <Text maw={"50%"} className="wrapped-text">
                        Created At
                    </Text>
                    <Text className="wrapped-text">
                        {formatDateTimeFromDate(new Date(entry.createdAt))}
                    </Text>
                </Group>
            </Stack>
        </Modal>
    );
}
