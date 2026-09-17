import {
    Box,
    Button,
    Card,
    Checkbox,
    Divider,
    Flex,
    Group,
    Paper,
    ScrollArea,
    Select,
    Stack,
    Text,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { MdAdd } from "react-icons/md";
import { useSearchParams } from "react-router-dom";
import EmptyState from "../../../shared/components/EmptyState";
import SidebarBurger from "../../../shared/components/navigation/SidebarBurger";
import { analyticsController } from "../../analytics/api/analyticsController";
import { AnalyticCard } from "../../analytics/components/AnalyticCard";
import {
    AnalyticPurposeEnum,
    codeSpansTrackers,
} from "../../analytics/enums/AnalyticPurposeEnum";
import { AnalyticResultTypeEnum } from "../../analytics/enums/AnalyticResultTypeEnum";
import {
    AnalyticConfigDto,
    CodeDto,
    GroupingDto,
    PurposeDto,
    ResultTypeDto,
    effectivePurposes,
    usesGrouping,
} from "../../analytics/types/AnalyticConfigDto";
import { resolveLegacyCalculation } from "../../analytics/legacyLineBarCodes";
import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { exploreController } from "../api/exploreController";
import { PromoteControls } from "../components/PromoteControls";
import { SourcePanel } from "../components/SourcePanel";
import { EvaluateFilterClauseDto } from "../types/EvaluateWidgetDto";

interface FilterRow {
    fieldId: string;
    operator: string;
    value?: string;
}

// Chart type and calculation are picked once for the whole run, not per source.
interface SourceInput {
    trackerId: string | null;
    fieldByPurpose: Record<string, string>;
    viewId: string | null;
    filters: FilterRow[];
}

// Fetched per source, keyed by the same index as the form's sources.
interface SourceData {
    fields: FieldDto[];
    views: ViewDto[];
}

interface ExploreState {
    resultType: string | null;
    grouping: string | null;
    code: string | null;
    matchedValuesOnly: boolean;
    sources: SourceInput[];
}

const emptySource = (): SourceInput => ({
    trackerId: null,
    fieldByPurpose: {},
    viewId: null,
    filters: [],
});

const EMPTY_STATE: ExploreState = {
    resultType: null,
    grouping: null,
    code: null,
    matchedValuesOnly: false,
    sources: [emptySource()],
};

// Result types that read from any number of trackers (Correlation also spans trackers but
// pairs exactly two, so it's handled separately via isPairedCode).
const COMBINABLE_TYPES: string[] = [
    AnalyticResultTypeEnum.LineChart,
    AnalyticResultTypeEnum.BarChart,
    AnalyticResultTypeEnum.Calendar,
];

// Mirrors DataLimits.MaxDashboardItemSourceCount on the backend.
const MAX_TRACKERS = 5;

// Only chart types drawn on one shared axis have an entry here.
const X_AXIS_PURPOSE: Record<string, string> = {
    [AnalyticResultTypeEnum.LineChart]: "X-axis",
    [AnalyticResultTypeEnum.BarChart]: "Name",
};

function readStateFromUrl(raw: string | null): ExploreState {
    if (!raw) return EMPTY_STATE;
    try {
        const parsed = { ...EMPTY_STATE, ...JSON.parse(raw) } as ExploreState;
        const sources = Array.isArray(parsed.sources) && parsed.sources.length > 0
            ? parsed.sources.map((s) => ({ ...emptySource(), ...s }))
            : [emptySource()];
        // A URL bookmarked before grouping and aggregation were split carries one fused
        // Line/Bar code and no grouping.
        const { grouping, code } = resolveLegacyCalculation(
            parsed.grouping,
            parsed.code,
        );
        return { ...parsed, grouping, code, sources };
    } catch {
        return EMPTY_STATE;
    }
}

export default function ExplorePage() {
    const theme = useMantineTheme();
    const [searchParams, setSearchParams] = useSearchParams();

    const initial = useMemo(
        () => readStateFromUrl(searchParams.get("q")),
        // Read once on mount; later URL changes are ours.
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [],
    );

    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [config, setConfig] = useState<AnalyticConfigDto>();

    const [resultType, setResultType] = useState(initial.resultType);
    const [grouping, setGrouping] = useState(initial.grouping);
    const [code, setCode] = useState(initial.code);
    const [matchedValuesOnly, setMatchedValuesOnly] = useState(
        initial.matchedValuesOnly,
    );

    const form = useForm<{ sources: SourceInput[] }>({
        initialValues: { sources: initial.sources },
    });

    const [sourceData, setSourceData] = useState<SourceData[]>(
        initial.sources.map(() => ({ fields: [], views: [] })),
    );

    const [result, setResult] = useState<AnalyticDto>();
    const [isEvaluating, setIsEvaluating] = useState(false);
    const autoRanRef = useRef(false);

    const loadSourceData = useCallback(async (index: number, trackerId: string) => {
        const [fieldsRes, viewsRes] = await Promise.all([
            fieldsController.getFields(trackerId),
            viewsController.getViewList(trackerId),
        ]);
        setSourceData((prev) =>
            prev.map((d, i) =>
                i === index
                    ? { fields: fieldsRes.data ?? [], views: viewsRes.data ?? [] }
                    : d,
            ),
        );
    }, []);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
        analyticsController.getAnalyticsConfig().then((res) => {
            setConfig(res.data);
        });
        // Pick up the fields/views for any source the URL arrived with.
        initial.sources.forEach((s, i) => {
            if (s.trackerId) loadSourceData(i, s.trackerId);
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const resultTypesByName = useMemo(() => {
        const map: Record<string, ResultTypeDto> = {};
        config?.resultTypes.forEach((rt) => (map[rt.name] = rt));
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

    const isPairedCode = !!selectedCode && codeSpansTrackers(selectedCode);
    const isCombinable =
        isPairedCode ||
        (!!resultType && COMBINABLE_TYPES.includes(resultType));

    const sources = form.values.sources;

    // One write so callers that both clear and resize don't fight a stale form value.
    const reshapeSources = (count: number, clearMappings: boolean) => {
        const next = form.values.sources.map((s) =>
            clearMappings ? { ...s, fieldByPurpose: {} } : s,
        );
        while (next.length < count) next.push(emptySource());
        form.setFieldValue("sources", next.slice(0, count));
        setSourceData((prev) => {
            const nd = [...prev];
            while (nd.length < count) nd.push({ fields: [], views: [] });
            return nd.slice(0, count);
        });
    };

    const handleResultTypeChange = (value: string | null) => {
        setResultType(value);
        setGrouping(null);
        setCode(null);
        setResult(undefined);
        const keepCount = !!value && COMBINABLE_TYPES.includes(value);
        reshapeSources(keepCount ? sources.length : 1, true);
        if (!keepCount) setMatchedValuesOnly(false);
    };

    const handleGroupingChange = (value: string | null) => {
        setGrouping(value);
        setResult(undefined);
        const stillValid =
            !!code &&
            !!value &&
            (resultTypesByName[resultType ?? ""]?.groupings
                .find((g) => g.grouping === value)
                ?.allowedCodes.includes(code) ??
                false);
        if (!stillValid) setCode(null);
        reshapeSources(sources.length, true);
    };

    const handleCodeChange = (value: string | null) => {
        setCode(value);
        setResult(undefined);

        const codeDef =
            value && resultType
                ? resultTypesByName[resultType]?.codes.find((c) => c.code === value)
                : undefined;
        const paired = !!codeDef && codeSpansTrackers(codeDef);
        const combinable =
            !!resultType && COMBINABLE_TYPES.includes(resultType);

        // A Correlation pairs exactly two trackers; a non-combinable type is single-source.
        const count = paired ? 2 : combinable ? sources.length : 1;
        reshapeSources(count, true);
        if (!combinable) setMatchedValuesOnly(false);
    };

    const handleTrackerChange = (index: number, trackerId: string | null) => {
        form.setFieldValue(`sources.${index}`, {
            ...emptySource(),
            trackerId,
        });
        setSourceData((prev) =>
            prev.map((d, i) => (i === index ? { fields: [], views: [] } : d)),
        );
        setResult(undefined);
        if (trackerId) loadSourceData(index, trackerId);
    };

    const addRow = () => reshapeSources(sources.length + 1, false);

    const removeRow = (index: number) => {
        form.removeListItem("sources", index);
        setSourceData((prev) => prev.filter((_, i) => i !== index));
        setResult(undefined);
    };

    const xAxisPurpose = resultType ? X_AXIS_PURPOSE[resultType] : undefined;

    // Shared x-axis for a combined line/bar, or the join field for a Correlation.
    const narrowPurpose = isPairedCode
        ? AnalyticPurposeEnum.Match
        : xAxisPurpose;

    const narrowType = useMemo(() => {
        if (!narrowPurpose) return undefined;
        const first = sources[0];
        const firstFields = sourceData[0]?.fields ?? [];
        return firstFields.find(
            (f) => f.id === first?.fieldByPurpose[narrowPurpose],
        )?.type;
    }, [sources, sourceData, narrowPurpose]);

    const fieldOptionsFor = (
        data: SourceData,
        purpose: PurposeDto,
        index: number,
    ) =>
        data.fields
            .filter((f) => purpose.allowedDataTypes.includes(f.type))
            .filter(
                (f) =>
                    index === 0 ||
                    purpose.name !== narrowPurpose ||
                    !narrowType ||
                    f.type === narrowType,
            )
            .map((f) => ({ value: f.id, label: f.name }));

    const isRowComplete = (row: SourceInput): boolean =>
        !!row.trackerId &&
        calculationChosen &&
        purposes.every((p) => p.optional || !!row.fieldByPurpose[p.name]);

    const mappedFields = (row: SourceInput) =>
        Object.entries(row.fieldByPurpose)
            .filter(([, fieldId]) => !!fieldId)
            .map(([purpose, fieldId]) => ({ purpose, fieldId }));

    const completeFilters = (row: SourceInput): EvaluateFilterClauseDto[] =>
        row.filters
            .filter((f) => f.fieldId && f.operator)
            .map((f) => ({
                fieldId: f.fieldId,
                operator: f.operator,
                value: f.value ?? undefined,
            }));

    const canRun =
        calculationChosen &&
        sources.every(isRowComplete) &&
        (!isPairedCode || sources.length === 2);

    const canAddTracker =
        isCombinable && !isPairedCode && sources.length < MAX_TRACKERS;

    const sendMatchedValuesOnly =
        sources.length > 1 && !!xAxisPurpose && matchedValuesOnly;

    const run = useCallback(async () => {
        if (!canRun) return;

        setSearchParams(
            {
                q: JSON.stringify({
                    resultType,
                    grouping,
                    code,
                    matchedValuesOnly,
                    sources,
                } satisfies ExploreState),
            },
            { replace: true },
        );

        setIsEvaluating(true);
        try {
            const res = await exploreController.evaluate({
                resultType: resultType!,
                grouping: grouping ?? undefined,
                code: code!,
                matchedValuesOnly: sendMatchedValuesOnly,
                sources: sources.map((s) => ({
                    trackerId: s.trackerId!,
                    fields: mappedFields(s),
                    viewId: s.viewId ?? undefined,
                    filters: completeFilters(s),
                })),
            });
            setResult(res.data);
        } finally {
            setIsEvaluating(false);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [canRun, resultType, grouping, code, matchedValuesOnly, sources, sendMatchedValuesOnly]);

    // Evaluate once, immediately, when the page opened on a shared/bookmarked exploration.
    useEffect(() => {
        if (autoRanRef.current) return;
        if (!config) return;
        const ready =
            initial.sources.some((s) => s.trackerId) &&
            initial.sources.every(
                (s, i) => s.trackerId && (sourceData[i]?.fields.length ?? 0) > 0,
            );
        if (ready) {
            autoRanRef.current = true;
            run();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [config, sourceData]);

    const distinctTrackerIds = [
        ...new Set(sources.map((s) => s.trackerId).filter(Boolean)),
    ];
    const resultTrackerColor =
        distinctTrackerIds.length === 1
            ? trackers.find((t) => t.id === distinctTrackerIds[0])?.color
            : undefined;

    const isSingleValueResult =
        result?.resultType === AnalyticResultTypeEnum.SingleValue;

    const trackerOptions = trackers.map((t) => ({ value: t.id, label: t.name }));
    const resultTypeOptions = (config?.resultTypes ?? [])
        .filter((rt) => !rt.widgetOnly)
        .map((rt) => ({
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

    return (
        <Stack gap="md" h="100%">
            <Group gap="sm" wrap="nowrap">
                <SidebarBurger />
                <Title order={2} c={theme.primaryColor}>
                    Explore
                </Title>
            </Group>
            <Text size="sm" c="dimmed">
                Run a calculation over one or more trackers without saving it to a
                dashboard.
            </Text>

            <ScrollArea flex={1} mih={0}>
                <Flex
                    direction={{ base: "column", md: "row" }}
                    gap="md"
                    align="stretch"
                >
                    <Box
                        w={{ base: "100%", md: 340, lg: 380 }}
                        style={{ flexShrink: 0 }}
                    >
                        <Card withBorder radius="md" p="lg">
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
                                    disabled={
                                        !resultType ||
                                        (typeUsesGrouping && !grouping)
                                    }
                                />

                                {sources.map((row, index) => {
                                    const data = sourceData[index] ?? {
                                        fields: [],
                                        views: [],
                                    };
                                    return (
                                        <SourcePanel
                                            key={index}
                                            index={index}
                                            source={row}
                                            fields={data.fields}
                                            views={data.views}
                                            purposes={purposes}
                                            heading={
                                                isPairedCode
                                                    ? index === 0
                                                        ? "X axis"
                                                        : "Y axis"
                                                    : sources.length > 1
                                                      ? `Tracker ${index + 1}`
                                                      : undefined
                                            }
                                            canRemove={index > 0 && !isPairedCode}
                                            onRemove={() => removeRow(index)}
                                            trackerOptions={trackerOptions}
                                            trackerColor={
                                                trackers.find(
                                                    (t) => t.id === row.trackerId,
                                                )?.color
                                            }
                                            onTrackerChange={(value) =>
                                                handleTrackerChange(index, value)
                                            }
                                            fieldOptionsFor={(purpose) =>
                                                fieldOptionsFor(
                                                    data,
                                                    purpose,
                                                    index,
                                                )
                                            }
                                            narrowPurpose={narrowPurpose}
                                            narrowType={narrowType}
                                            onFieldChange={(purposeName, value) =>
                                                form.setFieldValue(
                                                    `sources.${index}.fieldByPurpose.${purposeName}`,
                                                    value,
                                                )
                                            }
                                            onViewChange={(value) =>
                                                form.setFieldValue(
                                                    `sources.${index}.viewId`,
                                                    value,
                                                )
                                            }
                                            form={form}
                                            filtersPath={`sources.${index}.filters`}
                                        />
                                    );
                                })}

                                {sources.length > 1 && xAxisPurpose && (
                                    <Checkbox
                                        label="Show only matched values"
                                        description="Plot only the x-axis values every tracker has data for."
                                        checked={matchedValuesOnly}
                                        onChange={(event) =>
                                            setMatchedValuesOnly(
                                                event.currentTarget.checked,
                                            )
                                        }
                                    />
                                )}

                                {canAddTracker && (
                                    <Button
                                        variant="light"
                                        leftSection={<MdAdd size={16} />}
                                        onClick={addRow}
                                    >
                                        Add another tracker
                                    </Button>
                                )}

                                <Button
                                    onClick={run}
                                    disabled={!canRun}
                                    loading={isEvaluating}
                                >
                                    Run
                                </Button>
                            </Stack>
                        </Card>
                    </Box>

                    <Box flex={1} miw={0}>
                        <Paper
                            withBorder
                            radius="md"
                            p="md"
                            h={460}
                            style={{
                                display: "flex",
                                flexDirection: "column",
                            }}
                        >
                            {result ? (
                                <>
                                    <div
                                        style={{
                                            // A single number sits at its natural size; a
                                            // chart stretches to fill the preview.
                                            flex: isSingleValueResult
                                                ? undefined
                                                : 1,
                                            minHeight: 0,
                                            minWidth: 0,
                                            display: "flex",
                                            overflow: "hidden",
                                        }}
                                    >
                                        <AnalyticCard
                                            analytic={result}
                                            color={resultTrackerColor}
                                            isConfiguring={false}
                                            fillHeight={!isSingleValueResult}
                                        />
                                    </div>
                                    <Divider my="sm" />
                                    <PromoteControls
                                        resultType={resultType!}
                                        grouping={grouping ?? undefined}
                                        code={code!}
                                        matchedValuesOnly={sendMatchedValuesOnly}
                                        sources={sources.map((s) => ({
                                            trackerId: s.trackerId!,
                                            fields: mappedFields(s),
                                            viewId: s.viewId,
                                            filters: completeFilters(s),
                                        }))}
                                        defaultName={
                                            selectedCode?.name ?? "Result"
                                        }
                                    />
                                </>
                            ) : (
                                <div
                                    style={{
                                        flex: 1,
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                    }}
                                >
                                    <EmptyState
                                        title="Nothing to show yet"
                                        hint="Pick a tracker and a calculation, then Run."
                                    />
                                </div>
                            )}
                        </Paper>
                    </Box>
                </Flex>
            </ScrollArea>
        </Stack>
    );
}
