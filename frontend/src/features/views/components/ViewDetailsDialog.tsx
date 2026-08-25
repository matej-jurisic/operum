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
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
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
                {view.queries.length === 0 && (
                    <Text c="dimmed" ta="center" size="sm">
                        This view has no queries — it matches every entry.
                    </Text>
                )}

                {view.queries.map((query, queryIndex) => (
                    <Stack key={query.id} gap="xs">
                        <Divider
                            label={
                                <Group gap={6}>
                                    <Text fw={500} size="sm">
                                        {query.name}
                                    </Text>
                                    <Badge
                                        variant="light"
                                        color="gray"
                                        size="xs"
                                    >
                                        precedence {queryIndex + 1}
                                    </Badge>
                                </Group>
                            }
                            labelPosition="center"
                        />

                        {query.sorts.length > 0 && (
                            <Stack gap={4}>
                                <Text size="xs" fw={500} c="dimmed">
                                    Sorts
                                </Text>
                                {query.sorts.map((sort, index) => (
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
                            </Stack>
                        )}

                        {query.filters.length > 0 && (
                            <Stack gap={4}>
                                <Text size="xs" fw={500} c="dimmed">
                                    Filters
                                </Text>
                                {query.filters.map((filter, index) => (
                                    <Group
                                        key={index}
                                        justify="space-between"
                                        wrap="nowrap"
                                    >
                                        <Text fw={500}>
                                            {filter.field.name}
                                        </Text>
                                        <Group gap="xs">
                                            <Badge
                                                color={props.tracker.color}
                                                variant="light"
                                            >
                                                {formatOperator(
                                                    filter.operator,
                                                )}
                                            </Badge>
                                            <Badge
                                                color={props.tracker.color}
                                                variant="outline"
                                            >
                                                {filter.value
                                                    ? renderValue(
                                                          filter.field.type,
                                                          filter.value,
                                                      )
                                                    : "Empty"}
                                            </Badge>
                                        </Group>
                                    </Group>
                                ))}
                            </Stack>
                        )}

                        {query.sorts.length === 0 &&
                            query.filters.length === 0 && (
                                <Text size="xs" c="dimmed" ta="center">
                                    No filters or sorts
                                </Text>
                            )}
                    </Stack>
                ))}
            </Stack>
        </Modal>
    );
}
