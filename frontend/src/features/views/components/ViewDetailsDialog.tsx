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
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../api/viewsController";
import { ViewDto } from "../types/ViewDto";

interface Props {
    viewId: string;
    tracker: TrackerDto;
    onClose: () => void;
}

export default function ViewDetailsDialog(props: Props) {
    const [view, setView] = useState<ViewDto>();

    useEffect(() => {
        const GetData = async () => {
            const response = await viewsController.getView(
                props.tracker.id,
                props.viewId
            );
            setView(response.data);
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
                                        variant="light"
                                    >
                                        Order: {sort.order + 1}
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
                                <Text fw={500} truncate="end" style={{ flex: 1, minWidth: 0 }}>
                                    {filter.field.name}
                                </Text>
                                <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
                                    <Badge
                                        color={props.tracker.color}
                                        variant="light"
                                    >
                                        {filter.operator}
                                    </Badge>
                                    <Badge
                                        color={props.tracker.color}
                                        variant="outline"
                                    >
                                        {filter.value
                                            ? renderValue(
                                                  filter.field.type,
                                                  filter.value
                                              )
                                            : "Empty"}
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
