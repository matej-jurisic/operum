import {
    ActionIcon,
    Card,
    Checkbox,
    Grid,
    Group,
    Stack,
    Text,
} from "@mantine/core";
import { MdDelete, MdEdit } from "react-icons/md";
import { IoDuplicateOutline } from "react-icons/io5";
import { RiFileListFill } from "react-icons/ri";
import { formatDateTimeFromDate } from "../../../shared/utils/formatters/TypeFormatter";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useEntries } from "../context/EntriesContext";
import { EntryDto } from "../types/EntryDto";

interface EntriesCardsProps {
    entries: EntryDto[];
    onViewDetails: (entry: EntryDto) => void;
    onEdit: (entry: EntryDto) => void;
    onDuplicate: (entry: EntryDto) => void;
    onDelete: (entry: EntryDto) => void;
}

export function EntriesCards({
    entries,
    onViewDetails,
    onEdit,
    onDuplicate,
    onDelete,
}: EntriesCardsProps) {
    const { tracker, canEditData } = useTracker();
    const { fields, visibleFields, visibleColumns } = useFields();
    const { selectedEntryIds, isSelectMode, toggleEntrySelection } =
        useEntries();

    const getFieldName = (fieldId: string) => {
        return fields.find((f) => f.id === fieldId)?.name || fieldId;
    };

    const getDisplayFields = (entry: EntryDto) => {
        return visibleFields.map((f) => {
            const fieldValue = entry.fieldValues.find(
                (fv) => fv.fieldId === f.id
            );
            return {
                fieldId: f.id,
                fieldType: f.type,
                value: fieldValue ? fieldValue.value : "",
            };
        });
    };

    return (
        <Stack gap="md">
            {entries.map((entry) => {
                const displayFields = getDisplayFields(entry);
                const isSelected = selectedEntryIds.has(entry.id);

                return (
                    <Card
                        key={entry.id}
                        shadow="sm"
                        padding="md"
                        radius="md"
                        withBorder
                        style={{
                            borderColor: isSelected
                                ? `var(--mantine-color-${tracker.color}-6)`
                                : undefined,
                            borderWidth: 1,
                        }}
                    >
                        <Stack gap="sm">
                            {isSelectMode && (
                                <Checkbox
                                    checked={isSelected}
                                    onChange={() =>
                                        toggleEntrySelection(entry.id)
                                    }
                                    color={tracker.color}
                                />
                            )}

                            {/* Field values in a responsive grid */}
                            <Grid gutter="xs">
                                {displayFields.map((fieldValue) => (
                                    <Grid.Col
                                        key={fieldValue?.fieldId}
                                        span={{ base: 6, sm: 6, md: 4, lg: 3 }}
                                    >
                                        <Stack gap={2}>
                                            <Text
                                                size="xs"
                                                c="dimmed"
                                                fw={500}
                                                tt="uppercase"
                                                truncate
                                            >
                                                {getFieldName(
                                                    fieldValue.fieldId
                                                )}
                                            </Text>
                                            <Text w={"100%"} size="sm" truncate>
                                                {renderValue(
                                                    fieldValue.fieldType,
                                                    fieldValue.value
                                                )}
                                            </Text>
                                        </Stack>
                                    </Grid.Col>
                                ))}
                            </Grid>

                            {/* Created at timestamp and actions */}
                            {(visibleColumns["createdAt"] ||
                                visibleColumns["actions"]) && (
                                <Group justify="flex-end">
                                    {visibleColumns["createdAt"] && (
                                        <Text size="xs" c="dimmed">
                                            Created:{" "}
                                            {formatDateTimeFromDate(
                                                new Date(entry.createdAt)
                                            )}
                                        </Text>
                                    )}

                                    {visibleColumns["actions"] && (
                                        <Group gap="xs">
                                            <ActionIcon
                                                variant="outline"
                                                color={tracker.color}
                                                size="lg"
                                                onClick={() =>
                                                    onViewDetails(entry)
                                                }
                                            >
                                                <RiFileListFill size={16} />
                                            </ActionIcon>
                                            {canEditData && <>
                                            <ActionIcon
                                                variant="outline"
                                                color="green"
                                                size="lg"
                                                onClick={() => onEdit(entry)}
                                            >
                                                <MdEdit size={16} />
                                            </ActionIcon>
                                            <ActionIcon
                                                variant="outline"
                                                color="gray"
                                                size="lg"
                                                onClick={() => onDuplicate(entry)}
                                            >
                                                <IoDuplicateOutline size={16} />
                                            </ActionIcon>
                                            <ActionIcon
                                                variant="outline"
                                                color="red"
                                                size="lg"
                                                onClick={() => onDelete(entry)}
                                            >
                                                <MdDelete size={16} />
                                            </ActionIcon>
                                            </>}
                                        </Group>
                                    )}
                                </Group>
                            )}
                        </Stack>
                    </Card>
                );
            })}
        </Stack>
    );
}
