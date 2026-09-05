import {
    ActionIcon,
    Button,
    Checkbox,
    Group,
    Paper,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { MdAdd, MdDelete } from "react-icons/md";
import { analyticsController } from "../../analytics/api/analyticsController";
import {
    AnalyticPurposeEnum,
    codeSpansTrackers,
} from "../../analytics/enums/AnalyticPurposeEnum";
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
import { useDashboard } from "../context/DashboardContext";
import {
    CreateAndPlaceWidgetDto,
    DashboardItemDisplayMode,
} from "../types/DashboardDto";
import { FilterFollowChecklist } from "./FilterFollowChecklist";
import {
    FilterFollowLinks,
    filterCandidatesFor,
    followLinksComplete,
} from "./filterLinkUtils";
import { WidgetDisplayModeFields } from "./WidgetDisplayModeFields";
import { SourceViewSelect } from "./SourceViewSelect";
import { YAxisScaleOption } from "./YAxisScaleOption";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: CreateAndPlaceWidgetDto, followFilters?: FilterFollowLinks[]) => Promise<void>;
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
    // Which of the board's existing filter widgets this source should follow, and which
    // field of this tracker maps to each of that filter's clauses.
    filterLinks: Record<string, Record<string, string>>;
}

// Result types that can read from any number of trackers. Line/bar merge onto a shared
// axis; a calendar just unions its dated events. A scatter chart's Correlation
// calculation also spans trackers but is handled separately (isPairedCode): it pairs
// exactly two, one per axis.
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
    filterLinks: {},
});

/**
 * Builds a chart from scratch over one or more trackers and places it on this board in
 * one step. The definition it produces is a first-class Widget Library entry, not owned by
 * this dashboard item or any tracker -- it can be placed on other boards afterwards from
 * the Library, and editing it there updates every placement, this one included.
 */
export function CustomAnalyticForm({ onBack, onAdd }: Props) {
    const { widgets } = useDashboard();
    const filterCandidates = useMemo(() => filterCandidatesFor(widgets), [widgets]);
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [resultType, setResultType] = useState<string | null>(null);
    const [code, setCode] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [rows, setRows] = useState<TrackerRow[]>([makeEmptyRow()]);
    const [matchedValuesOnly, setMatchedValuesOnly] = useState(false);
    const [yAxisFromZero, setYAxisFromZero] = useState(true);
    const [displayMode, setDisplayMode] = useState(DashboardItemDisplayMode.Full);
    const [mobileDisplayMode, setMobileDisplayMode] = useState(
        DashboardItemDisplayMode.Full,
    );
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

    // A scatter "Correlation": two trackers, each mapping the join field and a value, one
    // becoming the x-axis and the other the y-axis of a single point cloud.
    const isPairedCode = !!selectedCode && codeSpansTrackers(selectedCode);
    const isCombinable =
        isPairedCode ||
        (!!resultType && COMBINABLE_TYPES.includes(resultType));
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

        const codeDef = value
            ? resultTypesByName[resultType!]?.codes.find((c) => c.code === value)
            : undefined;
        const paired = !!codeDef && codeSpansTrackers(codeDef);

        if (paired) {
            // A Correlation pairs exactly two trackers, one per axis.
            setRows((prev) => [
                prev[0] ?? makeEmptyRow(),
                prev[1] ?? makeEmptyRow(),
            ]);
            setMatchedValuesOnly(false);
        } else if (!resultType || !COMBINABLE_TYPES.includes(resultType)) {
            setRows((prev) => prev.slice(0, 1));
            setMatchedValuesOnly(false);
        }
    };

    const handleTrackerChange = async (index: number, trackerId: string | null) => {
        updateRow(index, {
            trackerId,
            fieldMappings: {},
            viewId: null,
            fields: [],
            views: [],
            filterLinks: {},
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

    // The purpose that lands on the shared x-axis of a combined line/bar chart. Only these
    // types offer the "matched values only" option.
    const xAxisPurpose = resultType ? X_AXIS_PURPOSE[resultType] : undefined;

    // The purpose whose field type the later rows are pinned to the first row's: the shared
    // x-axis for a combined line/bar, or the join field for a Correlation (two trackers
    // only line up if they match on the same kind of value).
    const narrowPurpose = isPairedCode
        ? AnalyticPurposeEnum.Match
        : xAxisPurpose;

    // Sharing one definition leaves that field's type as the last thing rows can disagree
    // on, and the chart can't reconcile a mismatch. Rather than let it through and warn
    // afterwards (the backend still does, defensively), the first row's choice narrows what
    // the later rows are offered.
    const narrowType = useMemo(() => {
        if (!narrowPurpose) return undefined;
        const first = rows[0];
        return first?.fields.find((f) => f.id === first.fieldMappings[narrowPurpose])?.type;
    }, [rows, narrowPurpose]);

    // Fields of `row` that may fill `purpose`: the data types the analytic allows for it,
    // narrowed to the first row's type once the shared/join field is being picked.
    const fieldOptionsFor = (row: TrackerRow, purpose: PurposeDto, index: number) =>
        row.fields
            .filter((f) => purpose.allowedDataTypes.includes(f.type))
            .filter(
                (f) =>
                    index === 0 ||
                    purpose.name !== narrowPurpose ||
                    !narrowType ||
                    f.type === narrowType
            )
            .map((f) => ({ value: f.id, label: f.name }));

    const handleSubmit = async () => {
        if (!canSubmit) return;
        setIsSubmitting(true);
        await onAdd(
            {
                name: name.trim() || undefined,
                resultType: resultType!,
                code: code!,
                matchedValuesOnly:
                    rows.length > 1 && !!xAxisPurpose && matchedValuesOnly,
                yAxisFromZero: isLineChart ? yAxisFromZero : undefined,
                displayMode,
                mobileDisplayMode,
                sources: rows.map((row) => ({
                    trackerId: row.trackerId!,
                    analyticFields: Object.entries(row.fieldMappings)
                        .filter(([, fieldId]) => !!fieldId)
                        .map(([purpose, fieldId]) => ({ purpose, fieldId })),
                    viewId: row.viewId,
                })),
            },
            rows.map((row) => ({ trackerId: row.trackerId!, links: row.filterLinks })),
        );
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

    const canAddAnotherTracker =
        isCombinable && !isPairedCode && rows.length < MAX_TRACKERS;
    const canSubmit =
        !!selectedCode &&
        rows.every(isRowComplete) &&
        rows.every((row) => followLinksComplete(row.filterLinks, filterCandidates, row.fields)) &&
        (!isPairedCode || rows.length === 2);

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
                            {isPairedCode && (
                                <Text size="sm" fw={600}>
                                    {index === 0 ? "X axis" : "Y axis"}
                                </Text>
                            )}
                            {index > 0 && !isPairedCode && (
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
                                        purpose.name === narrowPurpose &&
                                        narrowType
                                            ? isPairedCode
                                                ? `Limited to ${narrowType} fields so the two trackers match up.`
                                                : `Limited to ${narrowType} fields so both trackers share one axis.`
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

                            {row.trackerId && (
                                <FilterFollowChecklist
                                    fields={row.fields}
                                    filters={filterCandidates}
                                    links={row.filterLinks}
                                    onLinksChange={(filterLinks) =>
                                        updateRow(index, { filterLinks })
                                    }
                                />
                            )}
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

            <WidgetDisplayModeFields
                displayMode={displayMode}
                mobileDisplayMode={mobileDisplayMode}
                onDisplayModeChange={setDisplayMode}
                onMobileDisplayModeChange={setMobileDisplayMode}
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
