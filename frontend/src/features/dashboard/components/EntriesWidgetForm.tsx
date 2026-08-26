import { Button, Group, Select, Stack } from "@mantine/core";
import { useEffect, useState } from "react";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { useDashboard } from "../context/DashboardContext";
import { AddDashboardEntriesItemDto } from "../types/DashboardDto";
import { linkableViewWidgets, SourceViewSelect } from "./SourceViewSelect";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: AddDashboardEntriesItemDto) => Promise<void>;
}

/**
 * Picks the tracker an Entries widget's table reads from, and how it's filtered: one of
 * the tracker's own views, or a View widget already on the board whose dropdown it
 * follows instead — the same choice a chart's source gets from CustomAnalyticForm.
 */
export function EntriesWidgetForm({ onBack, onAdd }: Props) {
    const { widgets } = useDashboard();
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerId, setTrackerId] = useState<string | null>(null);
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
        setViewId(null);
        setLinkedViewWidgetId(null);
        setViews([]);
        if (!value) return;

        setIsLoadingTracker(true);
        const res = await viewsController.getViewList(value);
        setViews(res.data ?? []);
        setIsLoadingTracker(false);
    };

    const handleSubmit = async () => {
        if (!trackerId) return;
        setIsSubmitting(true);
        await onAdd({ trackerId, viewId, linkedViewWidgetId });
        setIsSubmitting(false);
    };

    const linkableWidgets = linkableViewWidgets(widgets, trackerId);

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

            <SourceViewSelect
                views={views}
                linkableWidgets={linkableWidgets}
                value={{ viewId, linkedViewWidgetId }}
                onChange={(selection) => {
                    setViewId(selection.viewId);
                    setLinkedViewWidgetId(selection.linkedViewWidgetId);
                }}
                disabled={!trackerId || isLoadingTracker}
                placeholder={isLoadingTracker ? "Loading..." : "All entries"}
            />

            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onBack}>
                    Back
                </Button>
                <Button
                    disabled={!trackerId}
                    loading={isSubmitting}
                    onClick={handleSubmit}
                >
                    Add
                </Button>
            </Group>
        </Stack>
    );
}
