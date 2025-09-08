import { Badge, Divider, Group, Modal, Text, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import api from "../api/api";
import { TrackerDto } from "../model/TrackerDto";
import { ViewDto } from "../model/ViewDto";

interface Props {
    viewId: string;
    tracker: TrackerDto;
    onClose: () => void;
}

const GetViewDetails = async (trackerId: string, viewId: string) => {
    const response = await api.get(`trackers/${trackerId}/views/${viewId}`);
    return response.data.data;
};

export default function ViewDetailsDialog(props: Props) {
    const [view, setView] = useState<ViewDto>();

    useEffect(() => {
        const GetData = async () => {
            const data = await GetViewDetails(props.tracker.id, props.viewId);
            setView(data);
        };

        GetData();
    }, [props.tracker.id, props.viewId]);

    if (!view) return null;

    return (
        <Modal
            opened
            centered
            onClose={props.onClose}
            title={
                <Group justify="space-between">
                    <Title order={4}>{view.name}</Title>
                    <Badge color={props.tracker.color} variant="filled">
                        View
                    </Badge>
                </Group>
            }
            size="lg"
        >
            <Divider label="Sort" />
            {view.sorts.map((sort) => (
                <>
                    <Group key={sort.fieldId} justify="space-between">
                        <Text fw={500}>{sort.field.name}</Text>
                        <Text>
                            {sort.descending ? "Descending" : "Ascending"}
                        </Text>
                    </Group>
                </>
            ))}
        </Modal>
    );
}
