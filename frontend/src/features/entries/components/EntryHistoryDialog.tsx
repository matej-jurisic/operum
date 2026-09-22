import {
    Badge,
    Group,
    Loader,
    Modal,
    ScrollArea,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { relativeTime } from "../../../shared/utils/relativeTime";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { entriesController } from "../api/entriesController";
import { EntryRevisionDto } from "../types/EntryRevisionDto";

interface EntryHistoryDialogProps {
    onClose: () => void;
    entryId: string;
    tracker: TrackerDto;
}

export default function EntryHistoryDialog({
    onClose,
    entryId,
    tracker,
}: EntryHistoryDialogProps) {
    const [history, setHistory] = useState<EntryRevisionDto[]>();

    useEffect(() => {
        const fetchHistory = async () => {
            const response = await entriesController.getEntryHistory(
                tracker.id,
                entryId
            );
            setHistory(response.data);
        };

        fetchHistory();
    }, [entryId, tracker.id]);

    return (
        <Modal
            opened
            centered
            size="lg"
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
                        Entry history
                    </Badge>
                </Group>
            }
        >
            {history === undefined && (
                <Group justify="center" py="lg">
                    <Loader size="sm" />
                </Group>
            )}
            {history?.length === 0 && (
                <Text size="sm" c="dimmed">
                    No changes recorded yet.
                </Text>
            )}
            {history !== undefined && history.length > 0 && (
                <ScrollArea.Autosize mah="60vh" offsetScrollbars>
                    <Stack gap="lg">
                        {history.map((revision) => (
                            <Stack key={revision.id} gap={4}>
                                <Group justify="space-between" wrap="nowrap">
                                    <Text size="sm" fw={500}>
                                        {revision.changeType === "create"
                                            ? "Created"
                                            : "Updated"}{" "}
                                        by{" "}
                                        {revision.changedByUserName ||
                                            "Unknown user"}
                                    </Text>
                                    <Text
                                        size="xs"
                                        c="dimmed"
                                        miw="max-content"
                                    >
                                        {relativeTime(
                                            revision.changedAt as unknown as string
                                        )}
                                    </Text>
                                </Group>
                                {revision.changes.map((change) => (
                                    <Group
                                        key={change.fieldId}
                                        gap={6}
                                        wrap="wrap"
                                    >
                                        <Text
                                            size="xs"
                                            c="dimmed"
                                            miw={100}
                                            className="wrapped-text"
                                        >
                                            {change.fieldName}
                                        </Text>
                                        {change.oldValue != null && (
                                            <Text
                                                size="xs"
                                                c="red"
                                                td="line-through"
                                                className="wrapped-text"
                                            >
                                                {renderValue(
                                                    change.fieldType,
                                                    change.oldValue
                                                )}
                                            </Text>
                                        )}
                                        <Text
                                            size="xs"
                                            c={
                                                change.newValue != null
                                                    ? "green"
                                                    : "dimmed"
                                            }
                                            className="wrapped-text"
                                        >
                                            {change.newValue != null
                                                ? renderValue(
                                                      change.fieldType,
                                                      change.newValue
                                                  )
                                                : "Cleared"}
                                        </Text>
                                    </Group>
                                ))}
                            </Stack>
                        ))}
                    </Stack>
                </ScrollArea.Autosize>
            )}
        </Modal>
    );
}
