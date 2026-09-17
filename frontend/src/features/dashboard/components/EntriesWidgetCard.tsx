import { Center, ScrollArea, Table, Text } from "@mantine/core";
import {
    cardBodyProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";
import { WidgetShell } from "../../analytics/components/WidgetShell";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { EntriesWidgetDto } from "../types/DashboardDto";

interface Props {
    widgetId: string;
    /** Resolved by the board itself; the card never fetches anything on its own. */
    entriesWidget: EntriesWidgetDto | undefined;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    onEdit?: (itemId: string) => void;
}

/** Read-only: no editing, selecting or deleting rows here, unlike the tracker page's own table. */
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
        <WidgetShell
            layout={layout}
            fillHeight
            isConfiguring={isConfiguring}
            color={trackerColor}
            itemId={widgetId}
            onRemove={onRemove}
            onEdit={onEdit}
            title={entriesWidget?.trackerName ?? "Entries"}
        >
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
                        // Not a drag control, but arrange mode still takes over pointer gestures inside it.
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
                                            <Text
                                                fw={600}
                                                size="xs"
                                                truncate="end"
                                            >
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
                                            const fieldValue =
                                                entry.fieldValues.find(
                                                    (fv) =>
                                                        fv.fieldId === field.id,
                                                );
                                            return (
                                                <Table.Td
                                                    key={field.id}
                                                    maw={200}
                                                >
                                                    <Text
                                                        size="xs"
                                                        truncate="end"
                                                    >
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
        </WidgetShell>
    );
}
