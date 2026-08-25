import { Alert, Button, Group, Select, Stack } from "@mantine/core";
import { useEffect, useState } from "react";
import { analyticsController } from "../../analytics/api/analyticsController";
import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { useDashboard } from "../context/DashboardContext";
import { AddDashboardItemFromAnalyticDto, WidgetTypes } from "../types/DashboardDto";

// Prefixes a "Filter by view" option's value so the same Select can offer both a fixed
// view and a live link to a View widget without the two id spaces colliding.
const LINK_PREFIX = "link:";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: AddDashboardItemFromAnalyticDto) => Promise<void>;
}

/**
 * Puts an analytic a tracker already has onto the board. The definition is copied when it
 * is added, so the widget keeps rendering the way it did here even if the tracker's own
 * analytic is later changed or deleted.
 */
export function ExistingAnalyticForm({ onBack, onAdd }: Props) {
    const { widgets } = useDashboard();
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerId, setTrackerId] = useState<string | null>(null);
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const [analyticId, setAnalyticId] = useState<string | null>(null);
    const [views, setViews] = useState<ViewDto[]>([]);
    const [viewId, setViewId] = useState<string | null>(null);
    const [linkedViewWidgetId, setLinkedViewWidgetId] = useState<string | null>(null);
    const [isLoadingTracker, setIsLoadingTracker] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
    }, []);

    const handleTrackerChange = async (value: string | null) => {
        setTrackerId(value);
        setAnalyticId(null);
        setAnalytics([]);
        setViewId(null);
        setLinkedViewWidgetId(null);
        setViews([]);
        if (!value) return;

        setIsLoadingTracker(true);
        const [analyticsRes, viewsRes] = await Promise.all([
            analyticsController.getTrackerAnalytics(value),
            viewsController.getViewList(value),
        ]);
        setAnalytics(analyticsRes.data ?? []);
        setViews(viewsRes.data ?? []);
        setIsLoadingTracker(false);
    };

    const handleSubmit = async () => {
        if (!analyticId) return;
        setIsSubmitting(true);
        await onAdd({
            analyticId,
            viewId: linkedViewWidgetId ? null : viewId,
            linkedViewWidgetId,
        });
        setIsSubmitting(false);
    };

    // The board's own View widgets built for this tracker — the only ones this can link
    // to. Numbered rather than named since a View widget carries no label of its own.
    const linkableViewWidgets = trackerId
        ? widgets
              .filter((w) => w.type === WidgetTypes.View && w.viewWidget?.trackerId === trackerId)
              .map((w, index) => ({ id: w.id, label: `View selector ${index + 1}` }))
        : [];

    const viewSelectData = [
        { group: "Fixed view", items: views.map((v) => ({ value: v.id, label: v.name })) },
        ...(linkableViewWidgets.length > 0
            ? [
                  {
                      group: "Follow a view widget",
                      items: linkableViewWidgets.map((w) => ({
                          value: `${LINK_PREFIX}${w.id}`,
                          label: w.label,
                      })),
                  },
              ]
            : []),
    ];

    const viewSelectValue = linkedViewWidgetId
        ? `${LINK_PREFIX}${linkedViewWidgetId}`
        : viewId;

    const handleViewSelectChange = (value: string | null) => {
        if (value?.startsWith(LINK_PREFIX)) {
            setViewId(null);
            setLinkedViewWidgetId(value.slice(LINK_PREFIX.length));
        } else {
            setViewId(value);
            setLinkedViewWidgetId(null);
        }
    };

    // A name is optional when an analytic is built, so two of a tracker's analytics can
    // still end up sharing one — either typed the same or both left to fall back to the
    // same calculation label. Numbering the repeats keeps the options tellable apart
    // without inventing a name the tracker never showed.
    const analyticOptions = analytics.map((analytic, index) => {
        const sameName = analytics.filter((a) => a.name === analytic.name);
        const position = sameName.indexOf(analytic) + 1;

        return {
            value: analytic.id,
            label:
                sameName.length > 1
                    ? `${analytic.name} (${position})`
                    : analytic.name || `Analytic ${index + 1}`,
        };
    });

    const hasNoAnalytics =
        !!trackerId && !isLoadingTracker && analytics.length === 0;

    return (
        <Stack gap="md">
            <Select
                label="Tracker"
                placeholder="Select a tracker"
                data={trackers.map((t) => ({ value: t.id, label: t.name }))}
                value={trackerId}
                onChange={handleTrackerChange}
                searchable
            />

            <Select
                label="Analytic"
                placeholder={
                    isLoadingTracker ? "Loading..." : "Select an analytic"
                }
                data={analyticOptions}
                value={analyticId}
                onChange={setAnalyticId}
                disabled={!trackerId || isLoadingTracker}
                searchable
            />

            {hasNoAnalytics && (
                <Alert color="gray" variant="light">
                    This tracker has no analytics yet. Build one on the tracker
                    first, or add a chart to the board directly.
                </Alert>
            )}

            <Select
                label="Filter by view (optional)"
                placeholder="All entries"
                data={viewSelectData}
                value={viewSelectValue}
                onChange={handleViewSelectChange}
                disabled={!trackerId || (views.length === 0 && linkableViewWidgets.length === 0)}
                clearable
            />

            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onBack}>
                    Back
                </Button>
                <Button
                    disabled={!analyticId}
                    loading={isSubmitting}
                    onClick={handleSubmit}
                >
                    Add
                </Button>
            </Group>
        </Stack>
    );
}
