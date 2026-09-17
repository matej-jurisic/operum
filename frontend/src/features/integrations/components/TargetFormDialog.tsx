import {
    ActionIcon,
    Alert,
    Badge,
    Button,
    Group,
    Modal,
    Paper,
    Select,
    SimpleGrid,
    Stack,
    Table,
    Text,
    Tooltip,
    useMantineTheme,
} from "@mantine/core";
import { DateInput } from "@mantine/dates";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useMemo, useState } from "react";
import { MdAdd, MdDelete, MdInfoOutline } from "react-icons/md";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import navigationStore from "../../../shared/stores/NavigationStore";
import {
    fieldTypesCompatible,
    FieldTypes,
} from "../../../shared/constants/DataTypes";
import {
    FieldMappingDto,
    IntegrationTargetDto,
    ProviderDto,
    SourceFieldDto,
} from "../types/IntegrationDto";
import { SaveIntegrationTargetDto } from "../types/requests/SaveIntegrationTargetDto";

/** Mirrors server-side MappingValidator; client-side only, server still enforces it. */
function acceptsField(source: SourceFieldDto, field: FieldDto): boolean {
    if (source.type === FieldTypes.TimeSpan) {
        // A duration may also be mapped to a plain Number field (raw seconds).
        return (
            field.type === FieldTypes.TimeSpan ||
            field.type === FieldTypes.Number
        );
    }
    return fieldTypesCompatible(source.type, field.type);
}

interface TargetFormDialogProps {
    opened: boolean;
    onClose: () => void;
    provider: ProviderDto;
    /** Set when editing; the tracker and resource are then fixed. */
    target?: IntegrationTargetDto;
    onSave: (dto: SaveIntegrationTargetDto) => Promise<boolean>;
}

export default function TargetFormDialog({
    opened,
    onClose,
    provider,
    target,
    onSave,
}: TargetFormDialogProps) {
    const isMobile = useMediaQuery("(max-width: 48em)");
    const theme = useMantineTheme();
    const isEdit = !!target;

    const [trackerId, setTrackerId] = useState(target?.trackerId ?? "");
    const [resourceType, setResourceType] = useState(
        target?.resourceType ?? provider.resources[0]?.resourceType ?? "",
    );
    const [backfillFrom, setBackfillFrom] = useState<Date | null>(
        target?.backfillFrom ? new Date(target.backfillFrom) : oneYearAgo(),
    );
    const [mappings, setMappings] = useState<FieldMappingDto[]>(
        target?.mappings ?? [],
    );
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [loadingFields, setLoadingFields] = useState(false);
    const [saving, setSaving] = useState(false);
    const [creatingKey, setCreatingKey] = useState<string | null>(null);

    useEffect(() => {
        if (!opened) return;
        setTrackerId(target?.trackerId ?? "");
        setResourceType(
            target?.resourceType ?? provider.resources[0]?.resourceType ?? "",
        );
        setBackfillFrom(
            target?.backfillFrom ? new Date(target.backfillFrom) : oneYearAgo(),
        );
        setMappings(target?.mappings ?? []);
    }, [opened, target, provider]);

    useEffect(() => {
        if (!trackerId) {
            setFields([]);
            return;
        }
        let cancelled = false;
        setLoadingFields(true);
        fieldsController
            .getFields(trackerId)
            .then((response) => {
                if (!cancelled) setFields(response.data ?? []);
            })
            .finally(() => {
                if (!cancelled) setLoadingFields(false);
            });
        return () => {
            cancelled = true;
        };
    }, [trackerId]);

    const sourceFields = useMemo(
        () =>
            provider.resources.find((r) => r.resourceType === resourceType)
                ?.fields ?? [],
        [provider, resourceType],
    );

    const writableFields = useMemo(
        () => fields.filter((f) => !f.isCalculated),
        [fields],
    );

    const setMapping = (sourceKey: string, patch: Partial<FieldMappingDto>) =>
        setMappings((current) =>
            current.map((m) =>
                m.sourceKey === sourceKey ? { ...m, ...patch } : m,
            ),
        );

    const addMapping = (source: SourceFieldDto, fieldId: string) =>
        setMappings((current) => [
            ...current.filter((m) => m.sourceKey !== source.key),
            // Required fields can't skip nulls; the server rejects that pairing.
            {
                sourceKey: source.key,
                fieldId,
                skipWhenNull: !writableFields.find((f) => f.id === fieldId)
                    ?.required,
            },
        ]);

    const removeMapping = (sourceKey: string) =>
        setMappings((current) =>
            current.filter((m) => m.sourceKey !== sourceKey),
        );

    const createFieldForSource = async (source: SourceFieldDto) => {
        setCreatingKey(source.key);
        try {
            const response = await fieldsController.createField(trackerId, {
                // The field editor caps names at 30 characters.
                name: source.label.trim().slice(0, 30),
                type: source.type,
                required: false,
                isCalculated: false,
            });
            const created = response.data;
            if (!created) return;
            setFields((current) => [...current, created]);
            setMappings((current) => [
                ...current.filter((m) => m.sourceKey !== source.key),
                { sourceKey: source.key, fieldId: created.id, skipWhenNull: true },
            ]);
        } catch {
            // API layer already surfaces the error.
        } finally {
            setCreatingKey(null);
        }
    };

    // Shared between the mobile and desktop layouts below.
    const rows = useMemo(() => {
        const mappedFieldIds = new Set(mappings.map((m) => m.fieldId));

        return sourceFields.map((source) => {
            const mapping = mappings.find((m) => m.sourceKey === source.key);
            const field = writableFields.find((f) => f.id === mapping?.fieldId);

            const options = writableFields
                .filter(
                    (f) =>
                        acceptsField(source, f) &&
                        (!mappedFieldIds.has(f.id) || mapping?.fieldId === f.id),
                )
                .map((f) => ({
                    value: f.id,
                    label: f.required ? `${f.name} (required)` : f.name,
                }));

            return { source, mapping, field, options };
        });
    }, [sourceFields, mappings, writableFields]);

    const trackerOptions = navigationStore.trackers.map((t) => ({
        value: t.id,
        label: t.name,
    }));

    const canSave =
        !!trackerId && !!resourceType && mappings.length > 0 && !saving;

    const save = async () => {
        setSaving(true);
        const ok = await onSave({
            trackerId,
            resourceType,
            isEnabled: target?.isEnabled ?? true,
            backfillFrom: backfillFrom
                ? toDateOnly(backfillFrom)
                : undefined,
            mappings,
        });
        setSaving(false);
        if (ok) onClose();
    };

    /** Whether a re-sync with no value for this field keeps or clears the current value. */
    const reSyncControl = (
        mapping: FieldMappingDto,
        field: FieldDto | undefined,
        size: "xs" | "sm",
        withLabel = false,
    ) => {
        // The server refuses "keep" for a required field.
        if (field?.required) {
            return (
                <Text size={size} c="dimmed">
                    Always written
                </Text>
            );
        }
        return (
            <Select
                size={size}
                label={withLabel ? "On re-sync" : undefined}
                data={[
                    { value: "keep", label: "Keep value" },
                    { value: "clear", label: "Clear field" },
                ]}
                value={mapping.skipWhenNull ? "keep" : "clear"}
                onChange={(value) =>
                    setMapping(mapping.sourceKey, {
                        skipWhenNull: value !== "clear",
                    })
                }
                allowDeselect={false}
                comboboxProps={{ withinPortal: true }}
            />
        );
    };

    const sourceLabel = (source: SourceFieldDto) => {
        const content = (
            <Group gap={6} wrap="wrap" w="fit-content">
                <Text size="sm" className="wrapped-text">
                    {source.label}
                </Text>
                <Badge size="xs" variant="light">
                    {source.type}
                </Badge>
            </Group>
        );

        if (!source.description) return content;

        return (
            <Tooltip label={source.description} withArrow multiline w={240}>
                {content}
            </Tooltip>
        );
    };

    const fieldSelect = (
        source: SourceFieldDto,
        mapping: FieldMappingDto | undefined,
        options: { value: string; label: string }[],
        size: "xs" | "sm",
    ) => (
        <Select
            size={size}
            placeholder={
                options.length ? "Not imported" : "No matching field"
            }
            data={options}
            value={mapping?.fieldId ?? null}
            disabled={options.length === 0}
            onChange={(value) =>
                value ? addMapping(source, value) : removeMapping(source.key)
            }
            searchable
            clearable
            comboboxProps={{ withinPortal: true }}
        />
    );

    const createButton = (source: SourceFieldDto) => (
        <Tooltip label="Create a matching field" withArrow>
            <ActionIcon
                variant="outline"
                color={theme.primaryColor}
                loading={creatingKey === source.key}
                disabled={!!creatingKey || loadingFields}
                onClick={() => createFieldForSource(source)}
                aria-label={`Create a field matching ${source.label}`}
            >
                <MdAdd size={16} />
            </ActionIcon>
        </Tooltip>
    );

    return (
        <Modal
            opened={opened}
            onClose={onClose}
            title={isEdit ? "Edit mapping" : `Import from ${provider.displayName}`}
            size="lg"
            centered
            fullScreen={isMobile}
        >
            <Stack gap="lg">
                <Stack gap="md">
                    <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
                        <Select
                            label="Tracker"
                            placeholder="Select tracker"
                            data={trackerOptions}
                            value={trackerId}
                            onChange={(value) => {
                                setTrackerId(value ?? "");
                                // Field ids belong to the tracker they came from.
                                setMappings([]);
                            }}
                            // The server refuses to move a target to a different tracker.
                            disabled={isEdit}
                            searchable
                        />
                        <Select
                            label="Data"
                            data={provider.resources.map((r) => ({
                                value: r.resourceType,
                                label: titleCase(r.resourceType),
                            }))}
                            value={resourceType}
                            onChange={(value) => {
                                setResourceType(value ?? "");
                                setMappings([]);
                            }}
                            disabled={isEdit || provider.resources.length <= 1}
                        />
                    </SimpleGrid>

                    {provider.supportsPull && (
                        <DateInput
                            label="Import history from"
                            description="How far back the first sync reaches."
                            value={backfillFrom}
                            onChange={(value) =>
                                setBackfillFrom(
                                    value
                                        ? new Date(value as string | Date)
                                        : null,
                                )
                            }
                            maxDate={new Date()}
                            clearable={false}
                        />
                    )}
                </Stack>

                {!trackerId ? (
                    <Alert
                        icon={<MdInfoOutline size={18} />}
                        variant="light"
                        color="blue"
                    >
                        Select a tracker to map its fields.
                    </Alert>
                ) : loadingFields ? null : (
                    <Stack gap="xs">
                        <Group
                            justify="space-between"
                            align="center"
                            wrap="wrap"
                            gap="xs"
                        >
                            <Text fw={500} size="sm">
                                Field mapping
                            </Text>
                            <Badge
                                variant="light"
                                color={mappings.length ? "teal" : "gray"}
                            >
                                {mappings.length}/{writableFields.length} mapped
                            </Badge>
                        </Group>

                        <Text size="xs" c="dimmed">
                            {provider.displayName} keeps syncing after setup. "On
                            re-sync" sets what happens when it later re-sends a
                            record with no value for a field.
                        </Text>

                        {isMobile ? (
                            <Stack gap="xs">
                                {rows.map(
                                    ({ source, mapping, field, options }) => (
                                        <Paper
                                            key={source.key}
                                            withBorder
                                            radius="sm"
                                            p="sm"
                                        >
                                            <Stack gap="xs">
                                                {sourceLabel(source)}
                                                {fieldSelect(
                                                    source,
                                                    mapping,
                                                    options,
                                                    "sm",
                                                )}
                                                {mapping ? (
                                                    <Group
                                                        justify="space-between"
                                                        align="flex-end"
                                                        wrap="nowrap"
                                                    >
                                                        {reSyncControl(
                                                            mapping,
                                                            field,
                                                            "sm",
                                                            true,
                                                        )}
                                                        <ActionIcon
                                                            variant="outline"
                                                            color="red"
                                                            size="lg"
                                                            onClick={() =>
                                                                removeMapping(
                                                                    source.key,
                                                                )
                                                            }
                                                            aria-label={`Stop importing ${source.label}`}
                                                        >
                                                            <MdDelete
                                                                size={16}
                                                            />
                                                        </ActionIcon>
                                                    </Group>
                                                ) : (
                                                    <Button
                                                        variant="light"
                                                        color={
                                                            theme.primaryColor
                                                        }
                                                        size="xs"
                                                        leftSection={
                                                            <MdAdd size={14} />
                                                        }
                                                        loading={
                                                            creatingKey ===
                                                            source.key
                                                        }
                                                        disabled={!!creatingKey}
                                                        onClick={() =>
                                                            createFieldForSource(
                                                                source,
                                                            )
                                                        }
                                                    >
                                                        Create matching field
                                                    </Button>
                                                )}
                                            </Stack>
                                        </Paper>
                                    ),
                                )}
                            </Stack>
                        ) : (
                            <Table.ScrollContainer minWidth={480}>
                                <Table verticalSpacing="xs">
                                    <Table.Thead>
                                        <Table.Tr>
                                            <Table.Th>
                                                {provider.displayName} value
                                            </Table.Th>
                                            <Table.Th>Tracker field</Table.Th>
                                            <Table.Th w={130}>
                                                <Group gap={4} wrap="nowrap">
                                                    On re-sync
                                                    <Tooltip
                                                        label="When a later sync has no value for this field: keep the value that's already there, or clear it."
                                                        withArrow
                                                        multiline
                                                        w={240}
                                                    >
                                                        <Text
                                                            component="span"
                                                            c="dimmed"
                                                            style={{
                                                                display:
                                                                    "inline-flex",
                                                            }}
                                                        >
                                                            <MdInfoOutline
                                                                size={14}
                                                            />
                                                        </Text>
                                                    </Tooltip>
                                                </Group>
                                            </Table.Th>
                                            <Table.Th w={48} />
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {rows.map(
                                            ({
                                                source,
                                                mapping,
                                                field,
                                                options,
                                            }) => (
                                                <Table.Tr key={source.key}>
                                                    <Table.Td>
                                                        {sourceLabel(source)}
                                                    </Table.Td>
                                                    <Table.Td>
                                                        {fieldSelect(
                                                            source,
                                                            mapping,
                                                            options,
                                                            "xs",
                                                        )}
                                                    </Table.Td>
                                                    <Table.Td>
                                                        {mapping &&
                                                            reSyncControl(
                                                                mapping,
                                                                field,
                                                                "xs",
                                                            )}
                                                    </Table.Td>
                                                    <Table.Td>
                                                        {mapping ? (
                                                            <Tooltip
                                                                label="Remove"
                                                                withArrow
                                                            >
                                                                <ActionIcon
                                                                    variant="outline"
                                                                    color="red"
                                                                    onClick={() =>
                                                                        removeMapping(
                                                                            source.key,
                                                                        )
                                                                    }
                                                                    aria-label={`Stop importing ${source.label}`}
                                                                >
                                                                    <MdDelete
                                                                        size={
                                                                            16
                                                                        }
                                                                    />
                                                                </ActionIcon>
                                                            </Tooltip>
                                                        ) : (
                                                            createButton(source)
                                                        )}
                                                    </Table.Td>
                                                </Table.Tr>
                                            ),
                                        )}
                                    </Table.Tbody>
                                </Table>
                            </Table.ScrollContainer>
                        )}
                    </Stack>
                )}

                <Button
                    size="md"
                    onClick={save}
                    disabled={!canSave}
                    loading={saving}
                >
                    {isEdit ? "Save mapping" : "Start importing"}
                </Button>
            </Stack>
        </Modal>
    );
}

function oneYearAgo() {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 1);
    return date;
}

/** The API takes a plain date; the local parts avoid a timezone shifting the day. */
function toDateOnly(date: Date) {
    const month = `${date.getMonth() + 1}`.padStart(2, "0");
    const day = `${date.getDate()}`.padStart(2, "0");
    return `${date.getFullYear()}-${month}-${day}`;
}

function titleCase(value: string) {
    return value.charAt(0).toUpperCase() + value.slice(1);
}
