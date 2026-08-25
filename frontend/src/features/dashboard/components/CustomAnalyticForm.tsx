import {
    ActionIcon,
    Button,
    Checkbox,
    Group,
    MultiSelect,
    Paper,
    Select,
    Stack,
    Text,
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
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { AddDashboardItemDto } from "../types/DashboardDto";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: AddDashboardItemDto) => Promise<void>;
}

// One tracker's contribution to the item. The chart type and calculation are picked once
// for the whole item, so a row only carries the tracker and its own field mapping.
interface TrackerRow {
    trackerId: string | null;
    fieldMappings: Record<string, string>;
    viewIds: string[];
    // Loaded per tracker
    fields: FieldDto[];
    views: ViewDto[];
}

// Only line/bar analytics can be combined into one chart across trackers — everything
// else (scatter/single-value/donut/calendar) has no shared points shape to merge.
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
    viewIds: [],
    fields: [],
    views: [],
});

/**
 * Builds a chart from scratch over one or more trackers. The definition it produces is
 * owned by the dashboard item, not by any tracker.
 */
export function CustomAnalyticForm({ onBack, onAdd }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [resultType, setResultType] = useState<string | null>(null);
    const [code, setCode] = useState<string | null>(null);
    const [name, setName] = useState("");
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

    // A different definition needs a different field mapping, so switching either one
    // clears what every row had mapped.
    const clearFieldMappings = () =>
        setRows((prev) => prev.map((row) => ({ ...row, fieldMappings: {} })));

    const handleResultTypeChange = (value: string | null) => {
        setResultType(value);
        setCode(null);
        clearFieldMappings();
        // Extra trackers only exist to be merged into one chart, which the new type may
        // not support.
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
        updateRow(index, {
            trackerId,
            fieldMappings: {},
            viewIds: [],
            fields: [],
            views: [],
        });
        if (!trackerId) return;

        const [fieldsRes, viewsRes] = await Promise.all([
            fieldsController.getFields(trackerId),
            viewsController.getViewList(trackerId),
        ]);
        updateRow(index, {
            fields: fieldsRes.data ?? [],
            views: viewsRes.data ?? [],
        });
    };

    const addRow = () => setRows((prev) => [...prev, makeEmptyRow()]);

    const removeRow = (index: number) =>
        setRows((prev) => prev.filter((_, i) => i !== index));

    const isRowComplete = (row: TrackerRow): boolean =>
        !!row.trackerId &&
        !!selectedCode &&
        selectedCode.purposes.every((p) => !!row.fieldMappings[p.name]);

    // The purpose that lands on the shared x-axis, for the type currently selected.
    const xAxisPurpose = resultType ? X_AXIS_PURPOSE[resultType] : undefined;

    // Sharing one definition leaves the x-axis field type as the last thing rows can
    // disagree on, and a combined chart draws them all on a single axis formatted from the
    // first series. Rather than let a mismatch through and warn about it afterwards
    // (DashboardService.BuildComposedResult still does, defensively), the first row's choice
    // narrows what the later rows are offered.
    const xAxisType = useMemo(() => {
        if (!xAxisPurpose) return undefined;
        const first = rows[0];
        return first?.fields.find((f) => f.id === first.fieldMappings[xAxisPurpose])?.type;
    }, [rows, xAxisPurpose]);

    // Fields of `row` that may fill `purpose`: the data types the analytic allows for it,
    // narrowed to the first row's x-axis type once that is what is being picked.
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
        await onAdd({
            resultType: resultType!,
            code: code!,
            matchedValuesOnly: rows.length > 1 && matchedValuesOnly,
            sources: rows.map((row, index) => ({
                trackerId: row.trackerId!,
                analyticFields: Object.entries(row.fieldMappings)
                    .filter(([, fieldId]) => !!fieldId)
                    .map(([purpose, fieldId]) => ({ purpose, fieldId })),
                viewIds: row.viewIds,
                // Only a single-source item has one calculated result to name; a combined
                // chart names itself from its series instead, so the label is dropped there.
                label:
                    index === 0 && rows.length === 1 && name.trim()
                        ? name.trim()
                        : undefined,
            })),
        });
        setIsSubmitting(false);
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

            {rows.length === 1 ? (
                <TextInput
                    label="Name"
                    description="Shown on the card instead of the calculation's default label"
                    placeholder={selectedCode?.name}
                    maxLength={100}
                    value={name}
                    onChange={(event) => setName(event.currentTarget.value)}
                />
            ) : (
                <Text size="xs" c="dimmed">
                    Combining trackers names the chart from its series instead of a name
                    typed here.
                </Text>
            )}

            {rows.map((row, index) => {
                const viewOptions = row.views.map((v) => ({
                    value: v.id,
                    label: v.name,
                }));

                return (
                    <Paper key={index} withBorder p="sm" radius="md">
                        <Stack gap="sm">
                            {index > 0 && (
                                <Group justify="space-between">
                                    <Text size="xs" c="dimmed">
                                        Combined with the tracker above
                                    </Text>
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

                            <MultiSelect
                                label="Filter by views (optional)"
                                placeholder="All entries"
                                data={viewOptions}
                                value={row.viewIds}
                                onChange={(value) => updateRow(index, { viewIds: value })}
                                disabled={!row.trackerId}
                            />
                        </Stack>
                    </Paper>
                );
            })}

            {rows.length > 1 && (
                <Checkbox
                    label="Show only matched values"
                    description="Plot only the x-axis values every tracker has data for, so the series cover the same range."
                    checked={matchedValuesOnly}
                    onChange={(event) =>
                        setMatchedValuesOnly(event.currentTarget.checked)
                    }
                />
            )}

            {canAddAnotherTracker && (
                <Button
                    variant="light"
                    leftSection={<MdAdd size={16} />}
                    onClick={addRow}
                >
                    Add another tracker
                </Button>
            )}

            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onBack}>
                    Back
                </Button>
                <Button
                    disabled={!canSubmit}
                    loading={isSubmitting}
                    onClick={handleSubmit}
                >
                    Add
                </Button>
            </Group>
        </Stack>
    );
}
