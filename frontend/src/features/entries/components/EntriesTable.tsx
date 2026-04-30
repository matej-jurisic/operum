import { ActionIcon, Badge, Checkbox, Group, Table, Text } from "@mantine/core";
import { useMemo } from "react";
import { CiBoxList } from "react-icons/ci";
import { IoDuplicateOutline } from "react-icons/io5";
import { MdDelete, MdEdit } from "react-icons/md";
import { formatDateTimeFromDate } from "../../../shared/utils/formatters/TypeFormatter";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useEntries } from "../context/EntriesContext";
import { EntryDto } from "../types/EntryDto";

const gridColumMinWidth = {
    string: "100px",
    number: "100px",
    date: "80px",
    datetime: "160px",
    timespan: "80px",
    bool: "80px",
};

interface EntriesTableProps {
    entries: EntryDto[];
    onViewDetails: (entry: EntryDto) => void;
    onEdit: (entry: EntryDto) => void;
    onDuplicate: (entry: EntryDto) => void;
    onDelete: (entry: EntryDto) => void;
}

export function EntriesTable({
    entries,
    onViewDetails,
    onEdit,
    onDuplicate,
    onDelete,
}: EntriesTableProps) {
    const {
        isSelectMode,
        allEntriesSelected,
        someEntriesSelected,
        toggleSelectAll,
        selectedEntryIds,
        toggleEntrySelection,
    } = useEntries();
    const { tracker, canEditData } = useTracker();
    const { visibleFields, visibleColumns } = useFields();

    // Create table headers from visible fields
    const tableHeaders = useMemo(() => {
        const headers = [];

        if (isSelectMode) {
            headers.push({
                id: "selection",
                label: (
                    <Group>
                        <Checkbox
                            checked={allEntriesSelected}
                            indeterminate={someEntriesSelected}
                            onChange={toggleSelectAll}
                        />
                        <Badge color={tracker.color} variant="filled">
                            {selectedEntryIds.size}
                        </Badge>
                    </Group>
                ),
                minWidth: "100px",
                width: "100px",
            });
        }

        headers.push(
            ...visibleFields.map((field) => ({
                id: field.id,
                label: field.name,
                minWidth: field.type
                    ? gridColumMinWidth[
                          field.type as keyof typeof gridColumMinWidth
                      ]
                    : "auto",
                width: "auto",
            })),
        );

        if (visibleColumns["createdAt"]) {
            headers.push({
                id: "createdAt",
                label: "Created At",
                minWidth: gridColumMinWidth.datetime,
                width: "auto",
            });
        }

        if (visibleColumns["actions"]) {
            headers.push({
                id: "actions",
                label: "Actions",
                minWidth: "125px",
                width: "167px",
            });
        }

        return headers;
    }, [
        visibleFields,
        visibleColumns,
        isSelectMode,
        allEntriesSelected,
        someEntriesSelected,
        selectedEntryIds.size,
        tracker.color,
        toggleSelectAll,
    ]);

    // Create table rows from entries
    const tableRows = useMemo(() => {
        return entries.map((entry) => {
            const fieldCells = visibleFields.map((field) => {
                const fieldValue = entry.fieldValues.find(
                    (fv) => fv.fieldId === field.id,
                );
                return renderValue(fieldValue?.fieldType, fieldValue?.value);
            });

            return (
                <Table.Tr key={entry.id} h={53}>
                    {/* Selection checkbox */}
                    {isSelectMode && (
                        <Table.Td>
                            <Checkbox
                                checked={selectedEntryIds.has(entry.id)}
                                onChange={() => toggleEntrySelection(entry.id)}
                                size="sm"
                            />
                        </Table.Td>
                    )}

                    {fieldCells.map((cellValue, index) => (
                        <Table.Td key={visibleFields[index].id} maw={300}>
                            <Text size="sm" truncate="end">
                                {cellValue}
                            </Text>
                        </Table.Td>
                    ))}
                    {visibleColumns["createdAt"] && (
                        <Table.Td>
                            <Text size="sm" c="dimmed">
                                {formatDateTimeFromDate(
                                    new Date(entry.createdAt),
                                )}
                            </Text>
                        </Table.Td>
                    )}
                    {visibleColumns["actions"] && (
                        <Table.Td>
                            <Group gap="xs">
                                <ActionIcon
                                    variant="outline"
                                    color={tracker.color}
                                    onClick={() => onViewDetails(entry)}
                                >
                                    <CiBoxList size={16} />
                                </ActionIcon>
                                {canEditData && (
                                    <>
                                        <ActionIcon
                                            variant="outline"
                                            color="green"
                                            onClick={() => onEdit(entry)}
                                        >
                                            <MdEdit size={16} />
                                        </ActionIcon>
                                        <ActionIcon
                                            variant="outline"
                                            color="gray"
                                            onClick={() => onDuplicate(entry)}
                                        >
                                            <IoDuplicateOutline size={16} />
                                        </ActionIcon>
                                        <ActionIcon
                                            variant="outline"
                                            color="red"
                                            onClick={() => onDelete(entry)}
                                        >
                                            <MdDelete size={16} />
                                        </ActionIcon>
                                    </>
                                )}
                            </Group>
                        </Table.Td>
                    )}
                </Table.Tr>
            );
        });
    }, [
        entries,
        visibleFields,
        visibleColumns,
        isSelectMode,
        selectedEntryIds,
        tracker.color,
        toggleEntrySelection,
        onViewDetails,
        onEdit,
        onDelete,
    ]);

    return (
        <Table.ScrollContainer
            minWidth={0}
            style={{ backgroundColor: "var(--mantine-color-body)" }}
        >
            <Table
                striped
                highlightOnHover
                withColumnBorders
                withTableBorder
                verticalSpacing={"sm"}
            >
                <Table.Thead>
                    <Table.Tr>
                        {tableHeaders.map((header) => (
                            <Table.Th
                                key={header.id}
                                miw={header.minWidth}
                                w={header.width}
                            >
                                {typeof header.label === "string" ? (
                                    <Text fw={600} size="sm">
                                        {header.label}
                                    </Text>
                                ) : (
                                    header.label
                                )}
                            </Table.Th>
                        ))}
                    </Table.Tr>
                </Table.Thead>
                <Table.Tbody>{tableRows}</Table.Tbody>
            </Table>
        </Table.ScrollContainer>
    );
}
