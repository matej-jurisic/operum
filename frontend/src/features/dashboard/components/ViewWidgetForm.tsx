import { Button, Checkbox, Group, Select, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import { AddDashboardViewItemDto, DashboardItemDto } from "../types/DashboardDto";
import { linkTargetsForViewWidget } from "./SourceViewSelect";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: AddDashboardViewItemDto) => Promise<void>;
}

/**
 * Picks the tracker a view selector on the board lists views for, and what it starts on.
 * Other widgets' sources normally link to it from their own form afterwards (see
 * CustomAnalyticForm) — this form also offers the reverse: tick the Analytic/Entries
 * widgets already on the board that read from the same tracker and they follow this
 * selector from the moment it's added, rather than being opened one by one.
 */
export function ViewWidgetForm({ onBack, onAdd }: Props) {
    const { dashboardId } = useDashboard();
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerId, setTrackerId] = useState<string | null>(null);
    const [views, setViews] = useState<ViewDto[]>([]);
    const [viewId, setViewId] = useState<string | null>(null);
    const [items, setItems] = useState<DashboardItemDto[]>([]);
    const [linkedItemIds, setLinkedItemIds] = useState<string[]>([]);
    const [isLoadingTracker, setIsLoadingTracker] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
        dashboardController.getDashboard(dashboardId).then((res) => {
            setItems(res.data?.items ?? []);
        });
    }, [dashboardId]);

    const handleTrackerChange = async (value: string | null) => {
        setTrackerId(value);
        setViewId(null);
        setViews([]);
        setLinkedItemIds([]);
        if (!value) return;

        setIsLoadingTracker(true);
        const res = await viewsController.getViewList(value);
        setViews(res.data ?? []);
        setIsLoadingTracker(false);
    };

    const toggleLinked = (itemId: string, checked: boolean) =>
        setLinkedItemIds((current) =>
            checked
                ? [...current, itemId]
                : current.filter((id) => id !== itemId),
        );

    const handleSubmit = async () => {
        if (!trackerId) return;
        setIsSubmitting(true);
        await onAdd({ trackerId, viewId, linkedItemIds });
        setIsSubmitting(false);
    };

    // Newly added, so nothing is linked yet — pass null so no target reads as "linked".
    const linkTargets = linkTargetsForViewWidget(items, trackerId, null);

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

            {trackerId && linkTargets.length > 0 && (
                <Stack gap="xs">
                    <Text size="sm" fw={500}>
                        Link existing widgets (optional)
                    </Text>
                    <Text size="xs" c="dimmed">
                        These widgets will follow this selector's dropdown.
                    </Text>
                    {linkTargets.map((target) => (
                        <Checkbox
                            key={target.itemId}
                            label={target.label}
                            description={target.note}
                            checked={linkedItemIds.includes(target.itemId)}
                            onChange={(event) =>
                                toggleLinked(target.itemId, event.currentTarget.checked)
                            }
                        />
                    ))}
                </Stack>
            )}

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
