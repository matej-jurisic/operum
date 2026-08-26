import { Center, Loader, Paper, ScrollArea, Stack, Table, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";
import { entriesController } from "../../entries/api/entriesController";
import { EntryDto } from "../../entries/types/EntryDto";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { EntriesWidgetDto } from "../types/DashboardDto";

interface Props {
    widgetId: string;
    /** The tracker/view/columns this table reads from, resolved by the board itself — the
        card never fetches its own tracker or view, only the entries themselves. */
    entriesWidget: EntriesWidgetDto | undefined;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the widget's own edit dialog — which view it reads through, and whether it's
        expandable. The rows it shows stay read-only regardless. */
    onEdit?: (itemId: string) => void;
}

// A board widget is a window onto the tracker, not the tracker's own paginated table — this
// is plenty to show the most recent activity without the card growing its own pagination.
const ENTRIES_LIMIT = 25;

/**
 * A read-only table of one tracker's entries. Everything the tracker page's own entries
 * table does — editing, selecting, deleting — is deliberately left out: this card is a
 * window onto the data, not another place to change it. onEdit is the widget's own
 * settings instead (its view, its expandable flags) — see EditEntriesWidgetModal.
 *
 * Which columns to show comes from the board (entriesWidget.columns, following the view
 * the same way its filter does); only the rows themselves are fetched here, through the
 * same paged endpoint the tracker page uses.
 */
export function EntriesWidgetCard({
    widgetId,
    entriesWidget,
    color,
    isConfiguring,
    onRemove,
    onEdit,
}: Props) {
    const layout = useCardLayout(true);
    const [entries, setEntries] = useState<EntryDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);

    const trackerId = entriesWidget?.trackerId;
    const viewId = entriesWidget?.viewId;

    useEffect(() => {
        if (!trackerId) {
            setEntries([]);
            return;
        }

        let cancelled = false;
        setIsLoading(true);
        entriesController
            .getEntries(trackerId, viewId, 1, ENTRIES_LIMIT)
            .then((res) => {
                if (!cancelled) setEntries(res.data?.items ?? []);
            })
            .finally(() => {
                if (!cancelled) setIsLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [trackerId, viewId]);

    const columns = entriesWidget?.columns ?? [];
    const trackerColor = entriesWidget?.color ?? color;

    return (
        <Paper
            ref={layout.ref}
            withBorder
            p={layout.padding}
            radius="md"
            {...cardShellProps(true)}
        >
            <Stack gap="xs" {...cardBodyProps(true)}>
                <AnalyticCardHeader
                    title={entriesWidget?.trackerName ?? "Entries"}
                    layout={layout}
                    color={trackerColor}
                    isConfiguring={isConfiguring}
                    analyticId={widgetId}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />

                {!entriesWidget ? (
                    <Center style={{ flex: 1, minHeight: 0 }}>
                        <Text size="sm" c="dimmed" ta="center">
                            This tracker is no longer available.
                        </Text>
                    </Center>
                ) : (
                    <ScrollArea
                        style={{
                            ...cardBodyProps(true).style,
                            // Matches the View widget's dropdown: not a control the board
                            // can drag by, but arranging the board still takes over every
                            // pointer gesture inside it.
                            pointerEvents: isConfiguring ? "none" : "auto",
                        }}
                    >
                        {isLoading ? (
                            <Center h="100%">
                                <Loader size="sm" color={trackerColor} />
                            </Center>
                        ) : entries.length === 0 ? (
                            <Center h="100%">
                                <Text size="sm" c="dimmed">
                                    No entries
                                </Text>
                            </Center>
                        ) : (
                            <Table striped highlightOnHover verticalSpacing="xs">
                                <Table.Thead>
                                    <Table.Tr>
                                        {columns.map((field) => (
                                            <Table.Th key={field.id}>
                                                <Text fw={600} size="xs" truncate="end">
                                                    {field.name}
                                                </Text>
                                            </Table.Th>
                                        ))}
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {entries.map((entry) => (
                                        <Table.Tr key={entry.id}>
                                            {columns.map((field) => {
                                                const fieldValue = entry.fieldValues.find(
                                                    (fv) => fv.fieldId === field.id,
                                                );
                                                return (
                                                    <Table.Td key={field.id} maw={200}>
                                                        <Text size="xs" truncate="end">
                                                            {renderValue(
                                                                fieldValue?.fieldType,
                                                                fieldValue?.value,
                                                            )}
                                                        </Text>
                                                    </Table.Td>
                                                );
                                            })}
                                        </Table.Tr>
                                    ))}
                                </Table.Tbody>
                            </Table>
                        )}
                    </ScrollArea>
                )}
            </Stack>
        </Paper>
    );
}
