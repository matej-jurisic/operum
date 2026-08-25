import { Badge, Group, Modal, Paper, Stack, Text, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import {
    QueryKindColor,
    QueryKindLabel,
} from "../../../shared/constants/QueryKinds";
import { describeQuery } from "../../../shared/utils/formatters/QueryFormatter";
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
                {view.description && (
                    <Text c="dimmed" size="sm" className="wrapped-text">
                        {view.description}
                    </Text>
                )}

                {view.queries.length === 0 ? (
                    <Text c="dimmed" ta="center" size="sm">
                        This view has no queries, so it matches every entry.
                    </Text>
                ) : (
                    view.queries.map((query, index) => (
                        <Paper key={query.id} p="sm" withBorder>
                            <Group justify="space-between" wrap="nowrap">
                                <Group gap="xs" wrap="nowrap">
                                    <Badge
                                        variant="light"
                                        color={QueryKindColor[query.kind]}
                                        size="sm"
                                    >
                                        {QueryKindLabel[query.kind]}
                                    </Badge>
                                    <Text
                                        fw={500}
                                        size="sm"
                                        className="wrapped-text"
                                    >
                                        {describeQuery(query)}
                                    </Text>
                                </Group>
                                <Badge variant="outline" color="gray" size="xs">
                                    precedence {index + 1}
                                </Badge>
                            </Group>
                        </Paper>
                    ))
                )}
            </Stack>
        </Modal>
    );
}
