import {
    Button,
    Group,
    Modal,
    MultiSelect,
    Select,
    Stack,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { AddDashboardItemDto, AnalyticSummaryDto } from "../types/DashboardDto";

interface Props {
    onClose: () => void;
    onAdd: (dto: AddDashboardItemDto) => Promise<void>;
}

export function AddDashboardItemModal({ onClose, onAdd }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [selectedTrackerId, setSelectedTrackerId] = useState<string | null>(null);
    const [analytics, setAnalytics] = useState<AnalyticSummaryDto[]>([]);
    const [selectedAnalyticId, setSelectedAnalyticId] = useState<string | null>(null);
    const [views, setViews] = useState<ViewDto[]>([]);
    const [selectedViewIds, setSelectedViewIds] = useState<string[]>([]);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
    }, []);

    useEffect(() => {
        if (!selectedTrackerId) {
            setAnalytics([]);
            setViews([]);
            setSelectedAnalyticId(null);
            setSelectedViewIds([]);
            return;
        }

        trackersController.getTrackerAnalyticsSummary(selectedTrackerId).then((res) => {
            setAnalytics(res.data ?? []);
            setSelectedAnalyticId(null);
        });

        viewsController.getViewList(selectedTrackerId).then((res) => {
            setViews(res.data ?? []);
            setSelectedViewIds([]);
        });
    }, [selectedTrackerId]);

    const handleSubmit = async () => {
        if (!selectedTrackerId || !selectedAnalyticId) return;
        setIsSubmitting(true);
        await onAdd({
            trackerId: selectedTrackerId,
            analyticId: selectedAnalyticId,
            viewIds: selectedViewIds,
        });
        setIsSubmitting(false);
        onClose();
    };

    const trackerOptions = trackers.map((t) => ({
        value: t.id,
        label: t.name,
    }));

    const analyticOptions = analytics.map((a) => ({
        value: a.id,
        label: a.name,
    }));

    const viewOptions = views.map((v) => ({
        value: v.id,
        label: v.name,
    }));

    const canSubmit = !!selectedTrackerId && !!selectedAnalyticId;

    return (
        <Modal opened onClose={onClose} title="Add analytic to dashboard" size="md" centered>
            <Stack gap="md">
                <Select
                    label="Tracker"
                    placeholder="Select a tracker"
                    data={trackerOptions}
                    value={selectedTrackerId}
                    onChange={setSelectedTrackerId}
                    searchable
                />
                <Select
                    label="Analytic"
                    placeholder="Select an analytic"
                    data={analyticOptions}
                    value={selectedAnalyticId}
                    onChange={setSelectedAnalyticId}
                    searchable
                    disabled={!selectedTrackerId}
                />
                <MultiSelect
                    label="Filter by views (optional)"
                    placeholder="All entries"
                    data={viewOptions}
                    value={selectedViewIds}
                    onChange={setSelectedViewIds}
                    disabled={!selectedTrackerId}
                />
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
