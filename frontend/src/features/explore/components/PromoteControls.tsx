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
import { viewsController } from "../../views/api/viewsController";
import { widgetsController } from "../../widgets/api/widgetsController";
import { EvaluateFilterClauseDto } from "../types/EvaluateWidgetDto";

interface Props {
    trackerId: string;
    resultType: string;
    code: string;
    /** purpose -> fieldId, already complete. */
    fields: CreateAnalyticFieldDto[];
    /** A pre-existing saved view supplying the base filter/sort, if one was chosen. */
    viewId: string | null;
    /** Inline ad-hoc clauses. Promoting turns these into a saved view. */
    filters: EvaluateFilterClauseDto[];
    /** Calculation label, used as the default name. */
    defaultName: string;
}

type OpenModal = "dashboard" | "widget" | null;

/** Turns the current exploration into something permanent: a Widget Library entry, or a
    widget placed on a dashboard. Inline filters can't live on a widget definition, so
    both paths first save them as a view on the tracker. */
export function PromoteControls({
    trackerId,
    resultType,
    code,
    fields,
    viewId,
    filters,
    defaultName,
}: Props) {
    const [open, setOpen] = useState<OpenModal>(null);
    const [dashboards, setDashboards] = useState<DashboardDto[]>([]);
    const [dashboardId, setDashboardId] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [viewName, setViewName] = useState("");
    const [busy, setBusy] = useState(false);

    const hasInlineFilters = filters.length > 0;

    useEffect(() => {
        if (open !== "dashboard" || dashboards.length > 0) return;
        dashboardController.getDashboards().then((res) => {
            setDashboards(res.data ?? []);
        });
    }, [open, dashboards.length]);

    const reset = () => {
        setOpen(null);
        setDashboardId(null);
        setName("");
        setViewName("");
    };

    // Persists the inline clauses as a new view on the tracker and returns its id, or the
    // pre-existing view id when there are no inline clauses to save.
    const ensureViewId = async (): Promise<string | null> => {
        if (!hasInlineFilters) return viewId;
        const res = await viewsController.createView(trackerId, {
            name: viewName.trim(),
            queries: filters.map((f) => ({
                kind: "filter" as const,
                fieldId: f.fieldId,
                operator: f.operator,
                value: f.value,
            })),
            columnFieldIds: [],
        });
        return res.data.id;
    };

    const addToDashboard = async () => {
        if (!dashboardId) return;
        setBusy(true);
        try {
            const resolvedViewId = await ensureViewId();
            await dashboardController.createAndPlaceWidget(dashboardId, {
                name: name.trim() || undefined,
                resultType,
                code,
                sources: [
                    {
                        trackerId,
                        analyticFields: fields,
                        viewId: resolvedViewId,
                    },
                ],
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
                sources: [{ trackerId, fields }],
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
        !dashboardId || (hasInlineFilters && !viewName.trim());

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
                    {hasInlineFilters && (
                        <TextInput
                            label="Save filters as a view"
                            description="Your filters become a view on this tracker so the widget keeps them applied."
                            placeholder="e.g. Remote, last month"
                            maxLength={100}
                            value={viewName}
                            onChange={(e) =>
                                setViewName(e.currentTarget.value)
                            }
                        />
                    )}
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
                    {(hasInlineFilters || viewId) && (
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
