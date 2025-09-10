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
import { TrackerDto } from "../model/TrackerDto";
import { ViewDto } from "../model/ViewDto";
import { renderValue } from "./EntriesList";

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
                <Group justify="space-between" wrap="nowrap" mr="xs">
                    <Title order={4} className="wrapped-text" lineClamp={3}>
                        {view.name}
                    </Title>
                    <Badge color={props.tracker.color} variant="filled">
                        View
                    </Badge>
                </Group>
            }
            size={"md"}
        >
            <Stack>
                {view.sorts.length > 0 && (
                    <>
                        <Divider label="Sorts" labelPosition="center" />
                        {view.sorts.map((sort, index) => (
                            <Group
                                key={index}
                                justify="space-between"
                                wrap="nowrap"
                            >
                                <Text fw={500}>{sort.field.name}</Text>
                                <Group gap="xs">
                                    <Badge
                                        color={props.tracker.color}
                                        variant="outline"
                                    >
                                        {sort.order + 1}
                                    </Badge>
                                    <Badge color="gray" variant="light">
                                        {sort.descending
                                            ? "Descending"
                                            : "Ascending"}
                                    </Badge>
                                </Group>
                            </Group>
                        ))}
                    </>
                )}

                {view.filters.length > 0 && (
                    <>
                        <Divider label="Filters" labelPosition="center" />
                        {view.filters.map((filter, index) => (
                            <Group
                                key={index}
                                justify="space-between"
                                wrap="nowrap"
                            >
                                <Text fw={500}>{filter.field.name}</Text>
                                <Group gap="xs">
                                    <Badge color="blue" variant="outline">
                                        {filter.operator}
                                    </Badge>
                                    <Badge color="teal" variant="light">
                                        {renderValue(
                                            filter.field.type,
                                            filter.value
                                        )}
                                    </Badge>
                                </Group>
                            </Group>
                        ))}
                    </>
                )}
            </Stack>
        </Modal>
    );
}
