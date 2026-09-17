import {
    ActionIcon,
    Button,
    Checkbox,
    Group,
    NumberInput,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { TimePicker } from "@mantine/dates";
import { useEffect, useMemo, useState } from "react";
import { MdAdd, MdDelete } from "react-icons/md";
import { analyticsController } from "../../analytics/api/analyticsController";
import {
    AnalyticPurposeEnum,
    codeSpansTrackers,
    purposeHint,
} from "../../analytics/enums/AnalyticPurposeEnum";
import { AnalyticResultTypeEnum } from "../../analytics/enums/AnalyticResultTypeEnum";
import {
    GoalDirection,
    GoalDirections,
} from "../../analytics/types/AnalyticDto";
import {
    AnalyticConfigDto,
    CodeDto,
    GroupingDto,
    PurposeDto,
    ResultTypeDto,
    effectivePurposes,
    usesGrouping,
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
    GoalConditionalTargetDto,
} from "../types/DashboardDto";
import { FilterFollowChecklist } from "./FilterFollowChecklist";
import { GoalConditionalTargetsEditor } from "./GoalConditionalTargetsEditor";
import {
    FilterFollowLinks,
    connectedClausesFromLinks,
    filterCandidatesFor,
    followLinksComplete,
} from "./filterLinkUtils";
import { WidgetDisplayModeFields } from "./WidgetDisplayModeFields";
import { SourceViewSelect } from "./SourceViewSelect";
import { YAxisScaleOption } from "./YAxisScaleOption";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (
        dto: CreateAndPlaceWidgetDto,
        followFilters?: FilterFollowLinks[],
        goalConditionalTargets?: GoalConditionalTargetDto[],
    ) => Promise<void>;
}

// The chart type and calculation are picked once for the whole item, so a row only
// carries the tracker and its own field mapping.
interface TrackerRow {
    trackerId: string | null;
    fieldMappings: Record<string, string>;
    // The fixed tracker view this source reads through, if any.
    viewId: string | null;
    fields: FieldDto[];
    views: ViewDto[];
    // filterItemId -> (that filter's clause id -> field of this tracker it maps to).
    filterLinks: Record<string, Record<string, string>>;
}

// Line/bar merge onto a shared axis; a calendar just unions its dated events. A scatter
// Correlation also spans trackers but is handled separately (isPairedCode).
const COMBINABLE_TYPES: string[] = [
    AnalyticResultTypeEnum.LineChart,
    AnalyticResultTypeEnum.BarChart,
    AnalyticResultTypeEnum.Calendar,
];

// Mirrors DataLimits.MaxDashboardItemSourceCount on the backend.
const MAX_TRACKERS = 5;

// The purpose on the shared x-axis of a combined chart, per chart type; a calendar has none.
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

/** The definition this produces is a first-class Widget Library entry: placeable on other
 *  boards afterwards, and editing it there updates every placement, this one included. */
export function CustomAnalyticForm({ onBack, onAdd }: Props) {
    const { widgets } = useDashboard();
    const filterCandidates = useMemo(() => filterCandidatesFor(widgets), [widgets]);
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [resultType, setResultType] = useState<string | null>(null);
    const [grouping, setGrouping] = useState<string | null>(null);
    const [code, setCode] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [rows, setRows] = useState<TrackerRow[]>([makeEmptyRow()]);
    const [matchedValuesOnly, setMatchedValuesOnly] = useState(false);
    const [yAxisFromZero, setYAxisFromZero] = useState(true);
    // Goal widgets only: the target the value is shown as progress toward.
    const [goalTarget, setGoalTarget] = useState("");
    // Goal widgets only: whether more or less is the goal -- a cap/budget is LowerIsBetter.
    const [goalDirection, setGoalDirection] = useState<GoalDirection>(
        GoalDirections.HigherIsBetter,
    );
    // Goal widgets only: per-row targets overriding the default for a followed filter value.
    const [conditionalTargets, setConditionalTargets] = useState<
        GoalConditionalTargetDto[]
    >([]);
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

    const selectedResultType: ResultTypeDto | undefined = resultType
        ? resultTypesByName[resultType]
        : undefined;
    const typeUsesGrouping = usesGrouping(selectedResultType);
    const selectedGrouping: GroupingDto | undefined =
        grouping !== null
            ? selectedResultType?.groupings.find((g) => g.grouping === grouping)
            : undefined;

    const selectedCode: CodeDto | undefined =
        resultType && code
            ? resultTypesByName[resultType]?.codes.find((c) => c.code === code)
            : undefined;

    // For a grouping type the "Calculation" options depend on the chosen grouping.
    const availableCodes: CodeDto[] = !selectedResultType
        ? []
        : typeUsesGrouping
          ? selectedResultType.codes.filter((c) =>
                selectedGrouping?.allowedCodes.includes(c.code),
            )
          : selectedResultType.codes;
    const purposes: PurposeDto[] = effectivePurposes(
        selectedResultType,
        selectedGrouping,
        selectedCode,
    );
    const calculationChosen =
        !!selectedCode && (!typeUsesGrouping || !!selectedGrouping);

    // A scatter "Correlation": two trackers, each mapping a join field and a value, become
    // the x-axis and y-axis of a single point cloud.
    const isPairedCode = !!selectedCode && codeSpansTrackers(selectedCode);
    const isCombinable =
        isPairedCode ||
        (!!resultType && COMBINABLE_TYPES.includes(resultType));
    const isLineChart = resultType === AnalyticResultTypeEnum.LineChart;
    const isGoal = resultType === AnalyticResultTypeEnum.Goal;

    // Goal calculations that always come out as a plain number, whatever the field type.
    const GOAL_COUNTING_CODES = [
        "Count",
        "Count Distinct",
        "True Count",
        "False Count",
        "True Percentage",
    ];
    const goalValueField = isGoal
        ? rows[0]?.fields.find((f) => f.id === rows[0]?.fieldMappings["Value"])
        : undefined;
    const goalTargetIsDuration =
        !!goalValueField &&
        goalValueField.type === "timespan" &&
        !GOAL_COUNTING_CODES.includes(code ?? "");
    const goalTargetValid = goalTargetIsDuration
        ? /^\d+:[0-5]\d:[0-5]\d$/.test(goalTarget.trim())
        : goalTarget.trim() !== "" &&
          Number.isFinite(Number(goalTarget.trim()));

    // The target's format follows the calculation and the value field; either changing
    // invalidates whatever was typed.
    useEffect(() => {
        setGoalTarget("");
        setGoalDirection(GoalDirections.HigherIsBetter);
        setConditionalTargets([]);
    }, [resultType, code, goalValueField?.type]);

    // Goals aren't combinable, so the one row's followed filter clauses are the source.
    const goalConnectedClauses = useMemo(
        () =>
            isGoal
                ? connectedClausesFromLinks(
                      rows[0]?.filterLinks ?? {},
                      filterCandidates,
                      Object.fromEntries(
                          (rows[0]?.fields ?? []).map((f) => [f.id, f.name]),
                      ),
                  )
                : [],
        [isGoal, rows, filterCandidates],
    );

    const updateRow = (index: number, patch: Partial<TrackerRow>) => {
        setRows((prev) =>
            prev.map((row, i) => (i === index ? { ...row, ...patch } : row))
        );
    };

    const clearFieldMappings = () =>
        setRows((prev) => prev.map((row) => ({ ...row, fieldMappings: {} })));

    const handleResultTypeChange = (value: string | null) => {
        setResultType(value);
        setGrouping(null);
        setCode(null);
        clearFieldMappings();
        // Extra trackers only exist to be merged, which the new type may not support.
        if (!value || !COMBINABLE_TYPES.includes(value)) {
            setRows((prev) => prev.slice(0, 1));
            setMatchedValuesOnly(false);
        }
    };

    const handleGroupingChange = (value: string | null) => {
        setGrouping(value);
        const stillValid =
            !!code &&
            !!value &&
            (selectedResultType?.groupings
                .find((g) => g.grouping === value)
                ?.allowedCodes.includes(code) ??
                false);
        if (!stillValid) setCode(null);
        clearFieldMappings();
    };

    const handleCodeChange = (value: string | null) => {
        setCode(value);
        clearFieldMappings();

        const codeDef = value
            ? resultTypesByName[resultType!]?.codes.find((c) => c.code === value)
            : undefined;
        const paired = !!codeDef && codeSpansTrackers(codeDef);

        if (paired) {
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
        calculationChosen &&
        purposes.every((p) => p.optional || !!row.fieldMappings[p.name]);

    // Only line/bar types offer the "matched values only" option.
    const xAxisPurpose = resultType ? X_AXIS_PURPOSE[resultType] : undefined;

    // The purpose later rows' field type is pinned to the first row's: the shared x-axis
    // for combined line/bar, or the join field for a Correlation.
    const narrowPurpose = isPairedCode
        ? AnalyticPurposeEnum.Match
        : xAxisPurpose;

    // The first row's field type narrows what later rows are offered, so a mismatch the
    // chart can't reconcile is never picked (the backend still validates defensively).
    const narrowType = useMemo(() => {
        if (!narrowPurpose) return undefined;
        const first = rows[0];
        return first?.fields.find((f) => f.id === first.fieldMappings[narrowPurpose])?.type;
    }, [rows, narrowPurpose]);

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
                grouping: grouping ?? undefined,
                matchedValuesOnly:
                    rows.length > 1 && !!xAxisPurpose && matchedValuesOnly,
                goalTarget: isGoal ? goalTarget.trim() : undefined,
                goalDirection: isGoal ? goalDirection : undefined,
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
            isGoal && goalConnectedClauses.length > 0 ? conditionalTargets : undefined,
        );
        setIsSubmitting(false);
    };

    const trackerOptions = trackers.map((t) => ({ value: t.id, label: t.name }));
    const resultTypeOptions = (config?.resultTypes ?? []).map((rt) => ({
        value: rt.name,
        label: rt.name,
    }));
    const groupingOptions = (selectedResultType?.groupings ?? []).map((g) => ({
        value: g.grouping,
        label: g.name,
    }));
    const codeOptions = availableCodes.map((c) => ({
        value: c.code,
        label: c.name,
    }));

    const canAddAnotherTracker =
        isCombinable && !isPairedCode && rows.length < MAX_TRACKERS;
    const canSubmit =
        calculationChosen &&
        rows.every(isRowComplete) &&
        rows.every((row) => followLinksComplete(row.filterLinks, filterCandidates, row.fields)) &&
        (!isPairedCode || rows.length === 2) &&
        (!isGoal || goalTargetValid);

    return (
        <Stack gap="md">
            <Select
                label="Chart type"
                placeholder="Select a chart type"
                data={resultTypeOptions}
                value={resultType}
                onChange={handleResultTypeChange}
            />
            {typeUsesGrouping && (
                <Select
                    label="Group by"
                    placeholder="Select a grouping"
                    data={groupingOptions}
                    value={grouping}
                    onChange={handleGroupingChange}
                    disabled={!resultType}
                />
            )}
            <Select
                label="Calculation"
                placeholder="Select a calculation"
                data={codeOptions}
                value={code}
                onChange={handleCodeChange}
                disabled={!resultType || (typeUsesGrouping && !grouping)}
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

                            {calculationChosen && purposes.map((purpose) => (
                                <Select
                                    key={purpose.name}
                                    label={purpose.name}
                                    placeholder={
                                        purpose.optional
                                            ? "Optional"
                                            : `Select field (${purpose.allowedDataTypes.join(
                                                  ", "
                                              )})`
                                    }
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
                                            : purposeHint(purpose)
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

            {isGoal &&
                goalValueField &&
                (goalTargetIsDuration ? (
                    <TimePicker
                        label="Target (hh:mm:ss)"
                        withSeconds
                        format="24h"
                        value={goalTarget}
                        onChange={setGoalTarget}
                    />
                ) : (
                    <NumberInput
                        label="Target"
                        placeholder="Goal value"
                        value={goalTarget === "" ? "" : Number(goalTarget)}
                        onChange={(value) =>
                            setGoalTarget(value === "" ? "" : String(value))
                        }
                    />
                ))}

            {isGoal && goalValueField && (
                <SegmentedControl
                    value={goalDirection}
                    onChange={(value) => setGoalDirection(value as GoalDirection)}
                    data={[
                        { label: "Higher is better", value: GoalDirections.HigherIsBetter },
                        { label: "Lower is better", value: GoalDirections.LowerIsBetter },
                    ]}
                />
            )}

            {isGoal && goalValueField && goalConnectedClauses.length > 0 && (
                <GoalConditionalTargetsEditor
                    clauses={goalConnectedClauses}
                    value={conditionalTargets}
                    onChange={setConditionalTargets}
                />
            )}

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
