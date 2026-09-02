import {
    ActionIcon,
    Button,
    Checkbox,
    Group,
    Paper,
    Select,
    Stack,
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
import { CreateAndPlaceWidgetDto } from "../types/DashboardDto";
import { ExpandableOptionFields } from "./ExpandableOptionFields";
import { SourceViewSelect } from "./SourceViewSelect";
import { YAxisScaleOption } from "./YAxisScaleOption";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: CreateAndPlaceWidgetDto) => Promise<void>;
}

// One tracker's contribution to the item. The chart type and calculation are picked once
// for the whole item, so a row only carries the tracker and its own field mapping.
interface TrackerRow {
    trackerId: string | null;
    fieldMappings: Record<string, string>;
    // The fixed tracker view this source reads through, if any.
    viewId: string | null;
    // Loaded per tracker
    fields: FieldDto[];
    views: ViewDto[];
}

// Result types that can read from more than one tracker. Line/bar merge onto a shared
// axis; a calendar just unions its dated events. Scatter/single-value/donut have no
// merge path.
const COMBINABLE_TYPES: string[] = [
    AnalyticResultTypeEnum.LineChart,
    AnalyticResultTypeEnum.BarChart,
    AnalyticResultTypeEnum.Calendar,
];

// Mirrors DataLimits.MaxDashboardItemSourceCount on the backend.
const MAX_TRACKERS = 5;

// The purpose whose field ends up on the shared x-axis of a combined chart, per chart
// type. Only the types drawn on one shared axis have one; a combined calendar does not.
const X_AXIS_PURPOSE: Record<string, string> = {
    [AnalyticResultTypeEnum.LineChart]: "X-axis",
    [AnalyticResultTypeEnum.BarChart]: "Name",
};

const makeEmptyRow = (): TrackerRow => ({
    trackerId: null,
    fieldMappings: {},
    viewId: null,
    fields: [],
    views: [],
});

/**
 * Builds a chart from scratch over one or more trackers and places it on this board in
 * one step. The definition it produces is a first-class Widget Library entry, not owned by
 * this dashboard item or any tracker -- it can be placed on other boards afterwards from
 * the Library, and editing it there updates every placement, this one included.
 */
export function CustomAnalyticForm({ onBack, onAdd }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [resultType, setResultType] = useState<string | null>(null);
    const [code, setCode] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [rows, setRows] = useState<TrackerRow[]>([makeEmptyRow()]);
    const [matchedValuesOnly, setMatchedValuesOnly] = useState(false);
    const [yAxisFromZero, setYAxisFromZero] = useState(true);
    const [expandable, setExpandable] = useState(false);
    const [mobileExpandable, setMobileExpandable] = useState(false);
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
    const isLineChart = resultType === AnalyticResultTypeEnum.LineChart;

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
            viewId: null,
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
            name: name.trim() || undefined,
            resultType: resultType!,
            code: code!,
            matchedValuesOnly:
                rows.length > 1 && !!xAxisPurpose && matchedValuesOnly,
            yAxisFromZero: isLineChart ? yAxisFromZero : undefined,
            expandable,
            mobileExpandable,
            sources: rows.map((row) => ({
                trackerId: row.trackerId!,
                analyticFields: Object.entries(row.fieldMappings)
                    .filter(([, fieldId]) => !!fieldId)
                    .map(([purpose, fieldId]) => ({ purpose, fieldId })),
                viewId: row.viewId,
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

            <TextInput
                label="Name"
                placeholder={selectedCode?.name}
                maxLength={100}
                value={name}
                onChange={(event) => setName(event.currentTarget.value)}
            />

            {rows.map((row, index) => {
                return (
                    <Paper key={index} withBorder p="sm" radius="md">
                        <Stack gap="sm">
                            {index > 0 && (
                                <Group justify="flex-end">
                                    <ActionIcon
                                        size="sm"
                                        variant="outline"
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

                            <SourceViewSelect
                                views={row.views}
                                value={{ viewId: row.viewId }}
                                onChange={(selection) =>
                                    updateRow(index, selection)
                                }
                                disabled={!row.trackerId}
                            />
                        </Stack>
                    </Paper>
                );
            })}

            {rows.length > 1 && xAxisPurpose && (
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

            {isLineChart && (
                <YAxisScaleOption
                    yAxisFromZero={yAxisFromZero}
                    onChange={setYAxisFromZero}
                />
            )}

            <ExpandableOptionFields
                expandable={expandable}
                mobileExpandable={mobileExpandable}
                onExpandableChange={setExpandable}
                onMobileExpandableChange={setMobileExpandable}
            />

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
