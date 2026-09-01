import { Center, Paper, ScrollArea, Stack, Table, Text } from "@mantine/core";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { EntriesWidgetDto } from "../types/DashboardDto";

interface Props {
    widgetId: string;
    /** The tracker, columns and rows this table renders — all resolved by the board itself.
        The card never fetches anything; a view selector change recomputes the whole board. */
    entriesWidget: EntriesWidgetDto | undefined;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the widget's own edit dialog — which columns it shows, and whether it's
        expandable. The rows it shows stay read-only regardless. */
    onEdit?: (itemId: string) => void;
}

/**
 * A read-only table of one tracker's entries. Everything the tracker page's own entries
 * table does — editing, selecting, deleting — is deliberately left out: this card is a
 * window onto the data, not another place to change it. onEdit is the widget's own
 * settings instead (its columns, its expandable flags) — see EditEntriesWidgetModal.
 *
 * Both the columns and the rows come from the board (entriesWidget), already filtered and
 * sorted by whatever view selectors this widget follows — the card renders them as-is.
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

    const columns = entriesWidget?.columns ?? [];
    const entries = entriesWidget?.entries ?? [];
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
                        {entries.length === 0 ? (
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
