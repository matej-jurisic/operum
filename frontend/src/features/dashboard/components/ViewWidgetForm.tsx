import { Button, Group, Select, Stack } from "@mantine/core";
import { useEffect, useState } from "react";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { AddDashboardViewItemDto } from "../types/DashboardDto";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: AddDashboardViewItemDto) => Promise<void>;
}

/**
 * Picks the tracker a view selector on the board lists views for, and what it starts on.
 * Other widgets' sources link to it afterwards, from their own form (see
 * CustomAnalyticForm), so this one has nothing to say about who follows it.
 */
export function ViewWidgetForm({ onBack, onAdd }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerId, setTrackerId] = useState<string | null>(null);
    const [views, setViews] = useState<ViewDto[]>([]);
    const [viewId, setViewId] = useState<string | null>(null);
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
        await onAdd({ trackerId, viewId });
        setIsSubmitting(false);
    };

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
                label="Starting view (optional)"
                placeholder={isLoadingTracker ? "Loading..." : "All entries"}
                data={views.map((v) => ({ value: v.id, label: v.name }))}
                value={viewId}
                onChange={setViewId}
                disabled={!trackerId || isLoadingTracker}
                clearable
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
