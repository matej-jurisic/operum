import { Button, Group, MultiSelect, Select, Stack, TextInput } from "@mantine/core";
import { useEffect, useState } from "react";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateAndPlaceEntriesWidgetDto } from "../types/DashboardDto";
import { ExpandableOptionFields } from "./ExpandableOptionFields";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: CreateAndPlaceEntriesWidgetDto) => Promise<void>;
}

/**
 * Defines a new Widget Library Entries table and places it on this board in one step: the
 * tracker it reads from and which of that tracker's fields it shows as columns. How the
 * table is filtered isn't chosen here — that comes from linking it to a View Selector
 * widget on the board afterwards.
 */
export function EntriesWidgetForm({ onBack, onAdd }: Props) {
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerId, setTrackerId] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [columnFieldIds, setColumnFieldIds] = useState<string[]>([]);
    const [expandable, setExpandable] = useState(false);
    const [mobileExpandable, setMobileExpandable] = useState(false);
    const [isLoadingTracker, setIsLoadingTracker] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
    }, []);

    const handleTrackerChange = async (value: string | null) => {
        setTrackerId(value);
        setColumnFieldIds([]);
        setFields([]);
        if (!value) return;

        setIsLoadingTracker(true);
        const res = await fieldsController.getFields(value);
        setFields(res.data ?? []);
        setIsLoadingTracker(false);
    };

    const handleSubmit = async () => {
        if (!trackerId) return;
        setIsSubmitting(true);
        await onAdd({
            trackerId,
            name: name.trim() || undefined,
            columnFieldIds: columnFieldIds.length ? columnFieldIds : undefined,
            expandable,
            mobileExpandable,
        });
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

            <TextInput
                label="Name"
                description="Shown in the Widget Library"
                placeholder="Optional"
                maxLength={100}
                value={name}
                onChange={(event) => setName(event.currentTarget.value)}
            />

            <MultiSelect
                label="Columns"
                description="Leave empty to show every field"
                placeholder={
                    isLoadingTracker
                        ? "Loading..."
                        : columnFieldIds.length > 0
                          ? undefined
                          : "Every field"
                }
                data={fields.map((f) => ({ value: f.id, label: f.name }))}
                value={columnFieldIds}
                onChange={setColumnFieldIds}
                disabled={!trackerId || isLoadingTracker}
                searchable
                clearable
            />

            <ExpandableOptionFields
                expandable={expandable}
                mobileExpandable={mobileExpandable}
                onExpandableChange={setExpandable}
                onMobileExpandableChange={setMobileExpandable}
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
