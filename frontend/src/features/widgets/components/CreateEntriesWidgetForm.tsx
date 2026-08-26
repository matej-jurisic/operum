import { Button, Group, Select, Stack, TextInput } from "@mantine/core";
import { useEffect, useState } from "react";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateEntriesWidgetDto } from "../types/WidgetDto";

interface Props {
    onCancel: () => void;
    onSubmit: (dto: CreateEntriesWidgetDto) => Promise<void>;
}

/** Defines a new, reusable Entries table over one tracker. */
export function CreateEntriesWidgetForm({ onCancel, onSubmit }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerId, setTrackerId] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
    }, []);

    const handleSubmit = async () => {
        if (!trackerId) return;
        setIsSubmitting(true);
        try {
            await onSubmit({ trackerId, name: name.trim() || undefined });
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <Stack gap="md">
            <Select
                label="Tracker"
                placeholder="Select a tracker"
                data={trackers.map((t) => ({ value: t.id, label: t.name }))}
                value={trackerId}
                onChange={setTrackerId}
                searchable
            />
            <TextInput
                label="Name"
                placeholder="Optional"
                maxLength={100}
                value={name}
                onChange={(event) => setName(event.currentTarget.value)}
            />
            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onCancel}>
                    Cancel
                </Button>
                <Button disabled={!trackerId} loading={isSubmitting} onClick={handleSubmit}>
                    Create
                </Button>
            </Group>
        </Stack>
    );
}
