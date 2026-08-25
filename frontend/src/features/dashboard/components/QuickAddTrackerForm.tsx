import { Alert, Button, Group, Select, Stack } from "@mantine/core";
import { useEffect, useState } from "react";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { AddDashboardQuickAddItemDto } from "../types/DashboardDto";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: AddDashboardQuickAddItemDto) => Promise<void>;
}

// A quick-add button is only useful on a tracker that still takes manual input — one made
// entirely of calculated fields has nothing for the entry dialog to ask for.
const hasInputtableFields = (t: TrackerDto) => t.fields.some((f) => !f.isCalculated);

/**
 * Picks the tracker a quick-add button on the board opens the entry dialog for. Unlike
 * the chart-building forms this has nothing to configure beyond that: the dialog itself
 * already exists (QuickAddEntryDialog) and asks for whatever the tracker needs.
 */
export function QuickAddTrackerForm({ onBack, onAdd }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerId, setTrackerId] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers((res.data ?? []).filter(hasInputtableFields));
            setIsLoading(false);
        });
    }, []);

    const handleSubmit = async () => {
        if (!trackerId) return;
        setIsSubmitting(true);
        await onAdd({ trackerId });
        setIsSubmitting(false);
    };

    const hasNoTrackers = !isLoading && trackers.length === 0;

    return (
        <Stack gap="md">
            <Select
                label="Tracker"
                placeholder={isLoading ? "Loading..." : "Select a tracker"}
                data={trackers.map((t) => ({ value: t.id, label: t.name }))}
                value={trackerId}
                onChange={setTrackerId}
                disabled={isLoading}
                searchable
            />

            {hasNoTrackers && (
                <Alert color="gray" variant="light">
                    None of your trackers take manual entries yet.
                </Alert>
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
