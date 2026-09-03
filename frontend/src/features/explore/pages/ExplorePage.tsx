import {
    Box,
    Button,
    Card,
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
import { useEffect, useMemo, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import EmptyState from "../../../shared/components/EmptyState";
import SidebarBurger from "../../../shared/components/navigation/SidebarBurger";
import { analyticsController } from "../../analytics/api/analyticsController";
import { AnalyticCard } from "../../analytics/components/AnalyticCard";
import { codeSpansTrackers } from "../../analytics/enums/AnalyticPurposeEnum";
import {
    AnalyticConfigDto,
    CodeDto,
    ResultTypeDto,
} from "../../analytics/types/AnalyticConfigDto";
import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import EntryFilterListEditor from "../../views/components/EntryFilterListEditor";
import { exploreController } from "../api/exploreController";
import { PromoteControls } from "../components/PromoteControls";
import { EvaluateFilterClauseDto } from "../types/EvaluateWidgetDto";

interface FilterRow {
    fieldId: string;
    operator: string;
    value?: string;
}

interface ExploreState {
    trackerId: string | null;
    resultType: string | null;
    code: string | null;
    fieldByPurpose: Record<string, string>;
    viewId: string | null;
    filters: FilterRow[];
}

const EMPTY_STATE: ExploreState = {
    trackerId: null,
    resultType: null,
    code: null,
    fieldByPurpose: {},
    viewId: null,
    filters: [],
};

function readStateFromUrl(raw: string | null): ExploreState {
    if (!raw) return EMPTY_STATE;
    try {
        return { ...EMPTY_STATE, ...JSON.parse(raw) };
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
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [views, setViews] = useState<ViewDto[]>([]);

    const [trackerId, setTrackerId] = useState(initial.trackerId);
    const [resultType, setResultType] = useState(initial.resultType);
    const [code, setCode] = useState(initial.code);
    const [fieldByPurpose, setFieldByPurpose] = useState(initial.fieldByPurpose);
    const [viewId, setViewId] = useState(initial.viewId);

    const filterForm = useForm<{ filters: FilterRow[] }>({
        initialValues: { filters: initial.filters },
    });

    const [result, setResult] = useState<AnalyticDto>();
    const [isEvaluating, setIsEvaluating] = useState(false);

    const skipNextTrackerReset = useRef(!!initial.trackerId);
    const autoRanRef = useRef(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
        analyticsController.getAnalyticsConfig().then((res) => {
            setConfig(res.data);
        });
    }, []);

    // Load the picked tracker's fields and views. The mapping/filters below belong to
    // whatever tracker was picked before, so they reset -- except on the first run when
    // the state came from the URL.
    useEffect(() => {
        if (!trackerId) {
            setFields([]);
            setViews([]);
            return;
        }
        fieldsController.getFields(trackerId).then((res) => setFields(res.data ?? []));
        viewsController.getViewList(trackerId).then((res) => setViews(res.data ?? []));

        if (skipNextTrackerReset.current) {
            skipNextTrackerReset.current = false;
            return;
        }
        setFieldByPurpose({});
        setViewId(null);
        filterForm.setValues({ filters: [] });
        setResult(undefined);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [trackerId]);

    const resultTypesByName = useMemo(() => {
        const map: Record<string, ResultTypeDto> = {};
        config?.resultTypes.forEach((rt) => (map[rt.name] = rt));
        return map;
    }, [config]);

    const selectedCode: CodeDto | undefined = (() => {
        if (!resultType || !code) return undefined;
        const found = resultTypesByName[resultType]?.codes.find(
            (c) => c.code === code,
        );
        // A tracker-spanning code (correlation) can't be evaluated here even if a
        // bookmarked URL names one.
        return found && !codeSpansTrackers(found) ? found : undefined;
    })();

    const handleResultTypeChange = (value: string | null) => {
        setResultType(value);
        setCode(null);
        setFieldByPurpose({});
        setResult(undefined);
    };

    const handleCodeChange = (value: string | null) => {
        setCode(value);
        setFieldByPurpose({});
        setResult(undefined);
    };

    const filterRows = filterForm.values.filters;
    const completeFilters: EvaluateFilterClauseDto[] = useMemo(
        () =>
            filterRows
                .filter((f) => f.fieldId && f.operator)
                .map((f) => ({
                    fieldId: f.fieldId,
                    operator: f.operator,
                    value: f.value ?? undefined,
                })),
        [filterRows],
    );

    const mappedFields = useMemo(
        () =>
            Object.entries(fieldByPurpose)
                .filter(([, fieldId]) => !!fieldId)
                .map(([purpose, fieldId]) => ({ purpose, fieldId })),
        [fieldByPurpose],
    );

    const canRun =
        !!trackerId &&
        !!selectedCode &&
        selectedCode.purposes.every((p) => !!fieldByPurpose[p.name]);

    const run = async () => {
        if (!canRun) return;

        setSearchParams(
            {
                q: JSON.stringify({
                    trackerId,
                    resultType,
                    code,
                    fieldByPurpose,
                    viewId,
                    filters: filterRows,
                } satisfies ExploreState),
            },
            { replace: true },
        );

        setIsEvaluating(true);
        try {
            const res = await exploreController.evaluate({
                resultType: resultType!,
                code: code!,
                trackerId: trackerId!,
                fields: mappedFields,
                viewId: viewId ?? undefined,
                filters: completeFilters,
            });
            setResult(res.data);
        } finally {
            setIsEvaluating(false);
        }
    };

    // Evaluate once, immediately, when the page opened on a shared/bookmarked exploration.
    useEffect(() => {
        if (autoRanRef.current) return;
        if (initial.trackerId && config && fields.length > 0) {
            autoRanRef.current = true;
            run();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [config, fields.length]);

    const trackerColor = trackers.find((t) => t.id === trackerId)?.color;

    const resultTypeOptions = (config?.resultTypes ?? []).map((rt) => ({
        value: rt.name,
        label: rt.name,
    }));
    const codeOptions = (
        resultType ? resultTypesByName[resultType]?.codes ?? [] : []
    )
        // Explore runs one calculation over one tracker; a correlation pairs two, so it
        // has nothing to do here.
        .filter((c) => !codeSpansTrackers(c))
        .map((c) => ({ value: c.code, label: c.name }));

    return (
        <Stack gap="md" h="100%">
            <Group gap="sm" wrap="nowrap">
                <SidebarBurger />
                <Title order={2} c={theme.primaryColor}>
                    Explore
                </Title>
            </Group>
            <Text size="sm" c="dimmed">
                Run a calculation over a tracker without saving it to a dashboard.
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
                                label="Tracker"
                                placeholder="Select a tracker"
                                data={trackers.map((t) => ({
                                    value: t.id,
                                    label: t.name,
                                }))}
                                value={trackerId}
                                onChange={setTrackerId}
                                searchable
                            />
                            <Select
                                label="Chart type"
                                placeholder="Select a chart type"
                                data={resultTypeOptions}
                                value={resultType}
                                onChange={handleResultTypeChange}
                                disabled={!trackerId}
                            />
                            <Select
                                label="Calculation"
                                placeholder="Select a calculation"
                                data={codeOptions}
                                value={code}
                                onChange={handleCodeChange}
                                disabled={!resultType}
                            />

                            {selectedCode?.purposes.map((purpose) => (
                                <Select
                                    key={purpose.name}
                                    label={purpose.name}
                                    placeholder={`Select field (${purpose.allowedDataTypes.join(
                                        ", ",
                                    )})`}
                                    data={fields
                                        .filter((f) =>
                                            purpose.allowedDataTypes.includes(
                                                f.type,
                                            ),
                                        )
                                        .map((f) => ({
                                            value: f.id,
                                            label: f.name,
                                        }))}
                                    value={fieldByPurpose[purpose.name] || null}
                                    onChange={(value) =>
                                        setFieldByPurpose((prev) => ({
                                            ...prev,
                                            [purpose.name]: value ?? "",
                                        }))
                                    }
                                    clearable
                                />
                            ))}

                            {views.length > 0 && (
                                <Select
                                    label="Start from view"
                                    placeholder="No view"
                                    data={views.map((v) => ({
                                        value: v.id,
                                        label: v.name,
                                    }))}
                                    value={viewId}
                                    onChange={setViewId}
                                    clearable
                                />
                            )}

                            {trackerId && fields.length > 0 && (
                                <>
                                    <Divider />
                                    <EntryFilterListEditor
                                        fields={fields}
                                        form={filterForm}
                                        filtersPath="filters"
                                        color={trackerColor}
                                    />
                                </>
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
                                        flex: 1,
                                        minHeight: 0,
                                        minWidth: 0,
                                        display: "flex",
                                        overflow: "hidden",
                                    }}
                                >
                                    <AnalyticCard
                                        analytic={result}
                                        color={trackerColor}
                                        isConfiguring={false}
                                        fillHeight
                                    />
                                </div>
                                <Divider my="sm" />
                                <PromoteControls
                                    trackerId={trackerId!}
                                    resultType={resultType!}
                                    code={code!}
                                    fields={mappedFields}
                                    viewId={viewId}
                                    filters={completeFilters}
                                    defaultName={selectedCode?.name ?? "Result"}
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
