import {
    ActionIcon,
    Box,
    Button,
    Group,
    Modal,
    MultiSelect,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Text,
    Tooltip,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { MdAdd, MdDelete, MdWarningAmber } from "react-icons/md";
import { analyticsController } from "../../analytics/api/analyticsController";
import { AnalyticResultTypeEnum } from "../../analytics/enums/AnalyticResultTypeEnum";
import {
    AnalyticConfigDto,
    CodeDto,
    ResultTypeDto,
} from "../../analytics/types/AnalyticConfigDto";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { AddDashboardItemDto, AnalyticSummaryDto } from "../types/DashboardDto";

interface Props {
    onClose: () => void;
    onAdd: (dto: AddDashboardItemDto) => Promise<void>;
}

type SourceMode = "saved" | "new";

interface SourceRow {
    mode: SourceMode;
    trackerId: string | null;
    // "saved" mode
    analyticId: string | null;
    // "new" mode: an ad hoc definition that lives on this dashboard only
    resultType: string | null;
    code: string | null;
    fieldMappings: Record<string, string>;
    viewIds: string[];
    // Loaded per tracker
    analytics: AnalyticSummaryDto[];
    fields: FieldDto[];
    views: ViewDto[];
}

// Only line/bar analytics can be combined into one chart across trackers — everything
// else (scatter/single-value/donut/calendar) has no shared points shape to merge.
const COMBINABLE_TYPES: string[] = [
    AnalyticResultTypeEnum.LineChart,
    AnalyticResultTypeEnum.BarChart,
];

// The result type + code a row resolves to, whichever mode it is in. Both the combine
// constraint and the mismatch warning care only about this pair, not about where it
// came from.
interface RowDefinition {
    resultType: string;
    code: string;
}

// Mirrors DashboardService.BuildComposedResult's warning logic on the backend, so the
// user sees this before adding the item rather than only after, on the dashboard.
const getMismatchWarning = (
    base: RowDefinition | undefined,
    other: RowDefinition | undefined
): string | null => {
    if (!base || !other) return null;
    if (base.resultType !== other.resultType)
        return "This chart mixes line and bar sources, axes may not align as expected.";
    if (base.code !== other.code)
        return "Sources use different time buckets or aggregations, axis alignment may be misleading.";
    return null;
};

const makeEmptyRow = (): SourceRow => ({
    mode: "saved",
    trackerId: null,
    analyticId: null,
    resultType: null,
    code: null,
    fieldMappings: {},
    viewIds: [],
    analytics: [],
    fields: [],
    views: [],
});

export function AddDashboardItemModal({ onClose, onAdd }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [rows, setRows] = useState<SourceRow[]>([makeEmptyRow()]);
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

    const getSelectedCode = (row: SourceRow): CodeDto | undefined => {
        if (!row.resultType || !row.code) return undefined;
        return resultTypesByName[row.resultType]?.codes.find(
            (c) => c.code === row.code
        );
    };

    const getRowDefinition = (row: SourceRow): RowDefinition | undefined => {
        if (row.mode === "saved") {
            const analytic = row.analytics.find((a) => a.id === row.analyticId);
            return analytic
                ? { resultType: analytic.resultType, code: analytic.code }
                : undefined;
        }
        return row.resultType && row.code
            ? { resultType: row.resultType, code: row.code }
            : undefined;
    };

    const isRowComplete = (row: SourceRow): boolean => {
        if (!row.trackerId) return false;
        if (row.mode === "saved") return !!row.analyticId;

        const code = getSelectedCode(row);
        if (!code) return false;
        return code.purposes.every((p) => !!row.fieldMappings[p.name]);
    };

    const updateRow = (index: number, patch: Partial<SourceRow>) => {
        setRows((prev) => prev.map((row, i) => (i === index ? { ...row, ...patch } : row)));
    };

    // Any change that can make the first row non-combinable also invalidates the extra
    // rows that were only there to merge into it, so they get dropped.
    const updateRowAndPrune = (index: number, patch: Partial<SourceRow>) => {
        setRows((prev) => {
            const next = prev.map((row, i) => (i === index ? { ...row, ...patch } : row));
            if (index > 0) return next;

            const definition = getRowDefinition(next[0]);
            const combinable =
                !!definition && COMBINABLE_TYPES.includes(definition.resultType);
            return combinable ? next : next.slice(0, 1);
        });
    };

    const handleTrackerChange = async (index: number, trackerId: string | null) => {
        updateRowAndPrune(index, {
            trackerId,
            analyticId: null,
            resultType: null,
            code: null,
            fieldMappings: {},
            viewIds: [],
            analytics: [],
            fields: [],
            views: [],
        });
        if (!trackerId) return;

        const [analyticsRes, fieldsRes, viewsRes] = await Promise.all([
            trackersController.getTrackerAnalyticsSummary(trackerId),
            fieldsController.getFields(trackerId),
            viewsController.getViewList(trackerId),
        ]);
        updateRow(index, {
            analytics: analyticsRes.data ?? [],
            fields: fieldsRes.data ?? [],
            views: viewsRes.data ?? [],
        });
    };

    const addRow = () => setRows((prev) => [...prev, makeEmptyRow()]);

    const removeRow = (index: number) =>
        setRows((prev) => prev.filter((_, i) => i !== index));

    const handleSubmit = async () => {
        if (!canSubmit) return;
        setIsSubmitting(true);
        await onAdd({
            sources: rows.map((row) =>
                row.mode === "saved"
                    ? {
                          trackerId: row.trackerId!,
                          analyticId: row.analyticId!,
                          viewIds: row.viewIds,
                      }
                    : {
                          trackerId: row.trackerId!,
                          resultType: row.resultType!,
                          code: row.code!,
                          analyticFields: Object.entries(row.fieldMappings)
                              .filter(([, fieldId]) => !!fieldId)
                              .map(([purpose, fieldId]) => ({ purpose, fieldId })),
                          viewIds: row.viewIds,
                      }
            ),
        });
        setIsSubmitting(false);
        onClose();
    };

    const trackerOptions = trackers.map((t) => ({ value: t.id, label: t.name }));

    const firstDefinition = rows[0] ? getRowDefinition(rows[0]) : undefined;
    const canAddAnotherTracker =
        !!firstDefinition && COMBINABLE_TYPES.includes(firstDefinition.resultType);
    const canSubmit = rows.every(isRowComplete);

    return (
        <Modal opened onClose={onClose} title="Add analytic to dashboard" size="md" centered>
            <Stack gap="md">
                {rows.map((row, index) => {
                    // Extra rows can only hold their own against the first one if they
                    // render as a line or bar, so both the saved list and the ad hoc
                    // chart types are narrowed to those.
                    const combinableOnly = index > 0;

                    const analyticOptions = (
                        combinableOnly
                            ? row.analytics.filter((a) =>
                                  COMBINABLE_TYPES.includes(a.resultType)
                              )
                            : row.analytics
                    ).map((a) => ({ value: a.id, label: a.name }));

                    const resultTypeOptions = (config?.resultTypes ?? [])
                        .filter(
                            (rt) => !combinableOnly || COMBINABLE_TYPES.includes(rt.name)
                        )
                        .map((rt) => ({ value: rt.name, label: rt.name }));

                    const codeOptions = (
                        row.resultType
                            ? resultTypesByName[row.resultType]?.codes ?? []
                            : []
                    ).map((c) => ({ value: c.code, label: c.name }));

                    const selectedCode = getSelectedCode(row);
                    const viewOptions = row.views.map((v) => ({
                        value: v.id,
                        label: v.name,
                    }));
                    const mismatchWarning = combinableOnly
                        ? getMismatchWarning(firstDefinition, getRowDefinition(row))
                        : null;

                    return (
                        <Paper key={index} withBorder p="sm" radius="md">
                            <Stack gap="sm">
                                {index > 0 && (
                                    <Group justify="space-between">
                                        <Group gap="xs">
                                            <Text size="xs" c="dimmed">
                                                Combined with the chart above
                                            </Text>
                                            {mismatchWarning && (
                                                <Tooltip label={mismatchWarning} multiline maw={260}>
                                                    <Box style={{ cursor: "default", display: "flex" }}>
                                                        <MdWarningAmber
                                                            size={14}
                                                            color="var(--mantine-color-yellow-6)"
                                                        />
                                                    </Box>
                                                </Tooltip>
                                            )}
                                        </Group>
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

                                <SegmentedControl
                                    fullWidth
                                    size="xs"
                                    value={row.mode}
                                    onChange={(value) =>
                                        updateRowAndPrune(index, {
                                            mode: value as SourceMode,
                                            analyticId: null,
                                            resultType: null,
                                            code: null,
                                            fieldMappings: {},
                                        })
                                    }
                                    data={[
                                        { value: "saved", label: "Saved analytic" },
                                        { value: "new", label: "Build a new chart" },
                                    ]}
                                />

                                {row.mode === "saved" ? (
                                    <Select
                                        label="Analytic"
                                        placeholder="Select an analytic"
                                        data={analyticOptions}
                                        value={row.analyticId}
                                        onChange={(value) =>
                                            updateRowAndPrune(index, { analyticId: value })
                                        }
                                        searchable
                                        disabled={!row.trackerId}
                                    />
                                ) : (
                                    <>
                                        <Select
                                            label="Chart type"
                                            placeholder="Select a chart type"
                                            data={resultTypeOptions}
                                            value={row.resultType}
                                            onChange={(value) =>
                                                updateRowAndPrune(index, {
                                                    resultType: value,
                                                    code: null,
                                                    fieldMappings: {},
                                                })
                                            }
                                            disabled={!row.trackerId}
                                        />
                                        <Select
                                            label="Calculation"
                                            placeholder="Select a calculation"
                                            data={codeOptions}
                                            value={row.code}
                                            onChange={(value) =>
                                                updateRowAndPrune(index, {
                                                    code: value,
                                                    fieldMappings: {},
                                                })
                                            }
                                            disabled={!row.resultType}
                                        />
                                        {selectedCode?.purposes.map((purpose) => (
                                            <Select
                                                key={purpose.name}
                                                label={purpose.name}
                                                placeholder={`Select field (${purpose.allowedDataTypes.join(
                                                    ", "
                                                )})`}
                                                data={row.fields
                                                    .filter((f) =>
                                                        purpose.allowedDataTypes.includes(
                                                            f.type
                                                        )
                                                    )
                                                    .map((f) => ({
                                                        value: f.id,
                                                        label: f.name,
                                                    }))}
                                                value={
                                                    row.fieldMappings[purpose.name] || null
                                                }
                                                onChange={(value) =>
                                                    updateRow(index, {
                                                        fieldMappings: {
                                                            ...row.fieldMappings,
                                                            [purpose.name]: value ?? "",
                                                        },
                                                    })
                                                }
                                                clearable
                                            />
                                        ))}
                                    </>
                                )}

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
                    <Button variant="default" onClick={onClose}>
                        Cancel
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
        </Modal>
    );
}
