import {
    ActionIcon,
    Button,
    Checkbox,
    Group,
    Paper,
    Select,
    Stack,
    Textarea,
    TextInput,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { MdAdd, MdDelete } from "react-icons/md";
import { analyticsController } from "../../analytics/api/analyticsController";
import { AnalyticResultTypeEnum } from "../../analytics/enums/AnalyticResultTypeEnum";
import {
    AnalyticConfigDto,
    CodeDto,
    PurposeDto,
    ResultTypeDto,
} from "../../analytics/types/AnalyticConfigDto";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateWidgetDto } from "../types/WidgetDto";

interface Props {
    onCancel: () => void;
    onSubmit: (dto: CreateWidgetDto) => Promise<void>;
}

// One tracker's contribution to the widget. The chart type and calculation are picked once
// for the whole widget, so a row only carries the tracker and its own field mapping.
interface TrackerRow {
    trackerId: string | null;
    fieldMappings: Record<string, string>;
    fields: FieldDto[];
}

// Only line/bar charts can be combined into one widget across trackers — everything else
// (scatter/single-value/donut/calendar) has no shared points shape to merge.
const COMBINABLE_TYPES: string[] = [
    AnalyticResultTypeEnum.LineChart,
    AnalyticResultTypeEnum.BarChart,
];

// Mirrors DataLimits.MaxDashboardItemSourceCount on the backend.
const MAX_TRACKERS = 5;

// The purpose whose field ends up on the shared x-axis of a combined chart, per chart
// type. Only the combinable types need one.
const X_AXIS_PURPOSE: Record<string, string> = {
    [AnalyticResultTypeEnum.LineChart]: "X-axis",
    [AnalyticResultTypeEnum.BarChart]: "Name",
};

const makeEmptyRow = (): TrackerRow => ({
    trackerId: null,
    fieldMappings: {},
    fields: [],
});

/**
 * Defines a new, reusable Widget Library chart over one or more trackers. This is the
 * definition alone — no dashboard filter, label or layout, since those are placement-only
 * settings a Widget doesn't carry (see PlaceWidgetDto). Dashboard's own "New chart" form
 * (CustomAnalyticForm) builds the same shape plus those placement fields in one step.
 */
export function CreateWidgetForm({ onCancel, onSubmit }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [resultType, setResultType] = useState<string | null>(null);
    const [code, setCode] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [rows, setRows] = useState<TrackerRow[]>([makeEmptyRow()]);
    const [matchedValuesOnly, setMatchedValuesOnly] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
        analyticsController.getAnalyticsConfig().then((res) => {
            setConfig(res.data);
        });
    }, []);

    const resultTypesByName = useMemo(() => {
        const map: Record<string, ResultTypeDto> = {};
        config?.resultTypes.forEach((rt) => {
            map[rt.name] = rt;
        });
        return map;
    }, [config]);

    const selectedCode: CodeDto | undefined =
        resultType && code
            ? resultTypesByName[resultType]?.codes.find((c) => c.code === code)
            : undefined;

    const isCombinable = !!resultType && COMBINABLE_TYPES.includes(resultType);

    const updateRow = (index: number, patch: Partial<TrackerRow>) => {
        setRows((prev) =>
            prev.map((row, i) => (i === index ? { ...row, ...patch } : row))
        );
    };

    const clearFieldMappings = () =>
        setRows((prev) => prev.map((row) => ({ ...row, fieldMappings: {} })));

    const handleResultTypeChange = (value: string | null) => {
        setResultType(value);
        setCode(null);
        clearFieldMappings();
        if (!value || !COMBINABLE_TYPES.includes(value)) {
            setRows((prev) => prev.slice(0, 1));
            setMatchedValuesOnly(false);
        }
    };

    const handleCodeChange = (value: string | null) => {
        setCode(value);
        clearFieldMappings();
    };

    const handleTrackerChange = async (index: number, trackerId: string | null) => {
        updateRow(index, { trackerId, fieldMappings: {}, fields: [] });
        if (!trackerId) return;

        const fieldsRes = await fieldsController.getFields(trackerId);
        updateRow(index, { fields: fieldsRes.data ?? [] });
    };

    const addRow = () => setRows((prev) => [...prev, makeEmptyRow()]);

    const removeRow = (index: number) =>
        setRows((prev) => prev.filter((_, i) => i !== index));

    const isRowComplete = (row: TrackerRow): boolean =>
        !!row.trackerId &&
        !!selectedCode &&
        selectedCode.purposes.every((p) => !!row.fieldMappings[p.name]);

    const xAxisPurpose = resultType ? X_AXIS_PURPOSE[resultType] : undefined;

    const xAxisType = useMemo(() => {
        if (!xAxisPurpose) return undefined;
        const first = rows[0];
        return first?.fields.find((f) => f.id === first.fieldMappings[xAxisPurpose])?.type;
    }, [rows, xAxisPurpose]);

    const fieldOptionsFor = (row: TrackerRow, purpose: PurposeDto, index: number) =>
        row.fields
            .filter((f) => purpose.allowedDataTypes.includes(f.type))
            .filter(
                (f) =>
                    index === 0 ||
                    purpose.name !== xAxisPurpose ||
                    !xAxisType ||
                    f.type === xAxisType
            )
            .map((f) => ({ value: f.id, label: f.name }));

    const handleSubmit = async () => {
        if (!canSubmit) return;
        setIsSubmitting(true);
        try {
            await onSubmit({
                name: name.trim() || undefined,
                description: description.trim() || undefined,
                resultType: resultType!,
                code: code!,
                matchedValuesOnly: rows.length > 1 && matchedValuesOnly,
                sources: rows.map((row) => ({
                    trackerId: row.trackerId!,
                    fields: Object.entries(row.fieldMappings)
                        .filter(([, fieldId]) => !!fieldId)
                        .map(([purpose, fieldId]) => ({ purpose, fieldId })),
                })),
            });
        } finally {
            setIsSubmitting(false);
        }
    };

    const trackerOptions = trackers.map((t) => ({ value: t.id, label: t.name }));
    const resultTypeOptions = (config?.resultTypes ?? []).map((rt) => ({
        value: rt.name,
        label: rt.name,
    }));
    const codeOptions = (
        resultType ? resultTypesByName[resultType]?.codes ?? [] : []
    ).map((c) => ({ value: c.code, label: c.name }));

    const canAddAnotherTracker = isCombinable && rows.length < MAX_TRACKERS;
    const canSubmit = !!selectedCode && rows.every(isRowComplete);

    return (
        <Stack gap="md">
            <Select
                label="Chart type"
                placeholder="Select a chart type"
                data={resultTypeOptions}
                value={resultType}
                onChange={handleResultTypeChange}
            />
            <Select
                label="Calculation"
                placeholder="Select a calculation"
                data={codeOptions}
                value={code}
                onChange={handleCodeChange}
                disabled={!resultType}
            />

            <TextInput
                label="Name"
                placeholder={selectedCode?.name}
                maxLength={100}
                value={name}
                onChange={(event) => setName(event.currentTarget.value)}
            />
            <Textarea
                label="Description"
                placeholder="Optional"
                maxLength={500}
                autosize
                minRows={2}
                value={description}
                onChange={(event) => setDescription(event.currentTarget.value)}
            />

            {rows.map((row, index) => (
                <Paper key={index} withBorder p="sm" radius="md">
                    <Stack gap="sm">
                        {index > 0 && (
                            <Group justify="flex-end">
                                <ActionIcon
                                    size="sm"
                                    variant="subtle"
                                    color="red"
                                    onClick={() => removeRow(index)}
                                >
                                    <MdDelete size={14} />
                                </ActionIcon>
                            </Group>
                        )}
                        <Select
                            label="Tracker"
                            placeholder="Select a tracker"
                            data={trackerOptions}
                            value={row.trackerId}
                            onChange={(value) => handleTrackerChange(index, value)}
                            searchable
                        />

                        {selectedCode?.purposes.map((purpose) => (
                            <Select
                                key={purpose.name}
                                label={purpose.name}
                                placeholder={`Select field (${purpose.allowedDataTypes.join(
                                    ", "
                                )})`}
                                data={fieldOptionsFor(row, purpose, index)}
                                value={row.fieldMappings[purpose.name] || null}
                                onChange={(value) =>
                                    updateRow(index, {
                                        fieldMappings: {
                                            ...row.fieldMappings,
                                            [purpose.name]: value ?? "",
                                        },
                                    })
                                }
                                disabled={!row.trackerId}
                                clearable
                                description={
                                    index > 0 &&
                                    purpose.name === xAxisPurpose &&
                                    xAxisType
                                        ? `Limited to ${xAxisType} fields so both trackers share one axis.`
                                        : undefined
                                }
                            />
                        ))}
                    </Stack>
                </Paper>
            ))}

            {rows.length > 1 && (
                <Checkbox
                    label="Show only matched values"
                    description="Plot only the x-axis values every tracker has data for, so the series cover the same range."
                    checked={matchedValuesOnly}
                    onChange={(event) => setMatchedValuesOnly(event.currentTarget.checked)}
                />
            )}

            {canAddAnotherTracker && (
                <Button variant="light" leftSection={<MdAdd size={16} />} onClick={addRow}>
                    Add another tracker
                </Button>
            )}

            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onCancel}>
                    Cancel
                </Button>
                <Button disabled={!canSubmit} loading={isSubmitting} onClick={handleSubmit}>
                    Create
                </Button>
            </Group>
        </Stack>
    );
}
