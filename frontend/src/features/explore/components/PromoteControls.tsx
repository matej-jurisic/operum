import {
    Alert,
    Button,
    Group,
    Modal,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { notifications } from "@mantine/notifications";
import { useEffect, useState } from "react";
import { TbLayoutDashboard, TbBookmark } from "react-icons/tb";
import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";
import { dashboardController } from "../../dashboard/api/dashboardController";
import { DashboardDto } from "../../dashboard/types/DashboardDto";
import { trackersController } from "../../trackers/api/trackersController";
import { viewsController } from "../../views/api/viewsController";
import { widgetsController } from "../../widgets/api/widgetsController";
import { EvaluateFilterClauseDto } from "../types/EvaluateWidgetDto";

/** One source of the current exploration, ready to be turned into a widget source. */
export interface PromoteSource {
    trackerId: string;
    /** purpose -> fieldId, already complete. */
    fields: CreateAnalyticFieldDto[];
    /** A pre-existing saved view supplying the base filter/sort, if one was chosen. */
    viewId: string | null;
    /** Inline ad-hoc clauses. Promoting turns these into a saved view. */
    filters: EvaluateFilterClauseDto[];
}

interface Props {
    resultType: string;
    code: string;
    matchedValuesOnly: boolean;
    sources: PromoteSource[];
    /** Calculation label, used as the default name. */
    defaultName: string;
}

type OpenModal = "dashboard" | "widget" | null;

/** Turns the current exploration into something permanent: a Widget Library entry, or a
    widget placed on a dashboard. Inline filters can't live on a widget definition, so the
    dashboard path first saves each source's inline filters as a view on its tracker. */
export function PromoteControls({
    resultType,
    code,
    matchedValuesOnly,
    sources,
    defaultName,
}: Props) {
    const [open, setOpen] = useState<OpenModal>(null);
    const [dashboards, setDashboards] = useState<DashboardDto[]>([]);
    const [dashboardId, setDashboardId] = useState<string | null>(null);
    const [trackerNames, setTrackerNames] = useState<Record<string, string>>({});
    const [name, setName] = useState("");
    const [viewNames, setViewNames] = useState<Record<number, string>>({});
    const [busy, setBusy] = useState(false);

    const filteredSourceIndexes = sources
        .map((s, i) => (s.filters.length > 0 ? i : -1))
        .filter((i) => i >= 0);
    const hasAnyView = sources.some((s) => s.viewId || s.filters.length > 0);

    useEffect(() => {
        if (open !== "dashboard" || dashboards.length > 0) return;
        dashboardController.getDashboards().then((res) => {
            setDashboards(res.data ?? []);
        });
    }, [open, dashboards.length]);

    useEffect(() => {
        if (open !== "dashboard" || filteredSourceIndexes.length < 2) return;
        trackersController.getTrackerList("Accessible").then((res) => {
            const map: Record<string, string> = {};
            (res.data ?? []).forEach((t) => (map[t.id] = t.name));
            setTrackerNames(map);
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [open]);

    const reset = () => {
        setOpen(null);
        setDashboardId(null);
        setName("");
        setViewNames({});
    };

    // Persists each source's inline clauses as a new view on its tracker, returning the
    // view id to place that source with (its pre-existing view when it has no inline
    // clauses of its own).
    const ensureViewIds = async (): Promise<(string | null)[]> =>
        Promise.all(
            sources.map(async (source, i) => {
                if (source.filters.length === 0) return source.viewId;
                const res = await viewsController.createView(source.trackerId, {
                    name: viewNames[i]?.trim() || defaultName,
                    queries: source.filters.map((f) => ({
                        kind: "filter" as const,
                        fieldId: f.fieldId,
                        operator: f.operator,
                        value: f.value,
                    })),
                    columnFieldIds: [],
                });
                return res.data.id;
            }),
        );

    const addToDashboard = async () => {
        if (!dashboardId) return;
        setBusy(true);
        try {
            const viewIds = await ensureViewIds();
            await dashboardController.createAndPlaceWidget(dashboardId, {
                name: name.trim() || undefined,
                resultType,
                code,
                matchedValuesOnly,
                sources: sources.map((s, i) => ({
                    trackerId: s.trackerId,
                    analyticFields: s.fields,
                    viewId: viewIds[i],
                })),
            });
            notifications.show({
                title: "Added to dashboard",
                message: "The widget is on the board.",
                color: "teal",
                withBorder: true,
            });
            reset();
        } finally {
            setBusy(false);
        }
    };

    const saveAsWidget = async () => {
        setBusy(true);
        try {
            await widgetsController.createWidget({
                name: name.trim() || undefined,
                resultType,
                code,
                matchedValuesOnly,
                sources: sources.map((s) => ({
                    trackerId: s.trackerId,
                    fields: s.fields,
                })),
            });
            notifications.show({
                title: "Saved to Widget Library",
                message: "Add it to a dashboard from the board menu.",
                color: "teal",
                withBorder: true,
            });
            reset();
        } finally {
            setBusy(false);
        }
    };

    const dashboardSubmitDisabled =
        !dashboardId ||
        filteredSourceIndexes.some((i) => !viewNames[i]?.trim());

    const viewNameLabel = (index: number) =>
        filteredSourceIndexes.length > 1
            ? `Save ${trackerNames[sources[index].trackerId] ?? `tracker ${index + 1}`} filters as a view`
            : "Save filters as a view";

    return (
        <>
            <Group gap="sm">
                <Button
                    variant="light"
                    leftSection={<TbLayoutDashboard size={16} />}
                    onClick={() => setOpen("dashboard")}
                >
                    Add to dashboard
                </Button>
                <Button
                    variant="default"
                    leftSection={<TbBookmark size={16} />}
                    onClick={() => setOpen("widget")}
                >
                    Save as widget
                </Button>
            </Group>

            <Modal
                opened={open === "dashboard"}
                onClose={reset}
                title="Add to dashboard"
                centered
            >
                <Stack gap="md">
                    <Select
                        label="Dashboard"
                        placeholder="Select a dashboard"
                        data={dashboards.map((d) => ({
                            value: d.id,
                            label: d.name,
                        }))}
                        value={dashboardId}
                        onChange={setDashboardId}
                        searchable
                    />
                    <TextInput
                        label="Name"
                        placeholder={defaultName}
                        maxLength={100}
                        value={name}
                        onChange={(e) => setName(e.currentTarget.value)}
                    />
                    {filteredSourceIndexes.map((i) => (
                        <TextInput
                            key={i}
                            label={viewNameLabel(i)}
                            description="Your filters become a view on that tracker so the widget keeps them applied."
                            placeholder="e.g. Remote, last month"
                            maxLength={100}
                            value={viewNames[i] ?? ""}
                            onChange={(e) =>
                                setViewNames((prev) => ({
                                    ...prev,
                                    [i]: e.currentTarget.value,
                                }))
                            }
                        />
                    ))}
                    <Group justify="flex-end">
                        <Button variant="default" onClick={reset}>
                            Cancel
                        </Button>
                        <Button
                            loading={busy}
                            disabled={dashboardSubmitDisabled}
                            onClick={addToDashboard}
                        >
                            Add
                        </Button>
                    </Group>
                </Stack>
            </Modal>

            <Modal
                opened={open === "widget"}
                onClose={reset}
                title="Save as widget"
                centered
            >
                <Stack gap="md">
                    <TextInput
                        label="Name"
                        placeholder={defaultName}
                        maxLength={100}
                        value={name}
                        onChange={(e) => setName(e.currentTarget.value)}
                    />
                    {hasAnyView && (
                        <Alert color="gray" variant="light">
                            <Text size="sm">
                                Filters aren't stored on a library widget. Use
                                "Add to dashboard" to keep them applied.
                            </Text>
                        </Alert>
                    )}
                    <Group justify="flex-end">
                        <Button variant="default" onClick={reset}>
                            Cancel
                        </Button>
                        <Button loading={busy} onClick={saveAsWidget}>
                            Save
                        </Button>
                    </Group>
                </Stack>
            </Modal>
        </>
    );
}
