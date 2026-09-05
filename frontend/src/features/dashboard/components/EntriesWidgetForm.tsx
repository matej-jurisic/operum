import { Button, Group, MultiSelect, Select, Stack, TextInput } from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useDashboard } from "../context/DashboardContext";
import {
    CreateAndPlaceEntriesWidgetDto,
    DashboardItemDisplayMode,
} from "../types/DashboardDto";
import { FilterFollowChecklist } from "./FilterFollowChecklist";
import {
    FilterFollowLinks,
    filterCandidatesFor,
    followLinksComplete,
} from "./filterLinkUtils";
import { WidgetDisplayModeFields } from "./WidgetDisplayModeFields";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (
        dto: CreateAndPlaceEntriesWidgetDto,
        followFilters?: FilterFollowLinks,
    ) => Promise<void>;
}

/**
 * Defines a new Widget Library Entries table and places it on this board in one step: the
 * tracker it reads from and which of that tracker's fields it shows as columns. It can also
 * be linked to any of the board's existing filter widgets right away, via the "Follow
 * filters" checklist -- otherwise that's done afterwards from the filter widget's own edit
 * dialog.
 */
export function EntriesWidgetForm({ onBack, onAdd }: Props) {
    const { widgets } = useDashboard();
    const filterCandidates = useMemo(() => filterCandidatesFor(widgets), [widgets]);
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerId, setTrackerId] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [columnFieldIds, setColumnFieldIds] = useState<string[]>([]);
    const [filterLinks, setFilterLinks] = useState<Record<string, Record<string, string>>>({});
    const [displayMode, setDisplayMode] = useState(DashboardItemDisplayMode.Full);
    const [mobileDisplayMode, setMobileDisplayMode] = useState(
        DashboardItemDisplayMode.Full,
    );
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
        setFilterLinks({});
        if (!value) return;

        setIsLoadingTracker(true);
        const res = await fieldsController.getFields(value);
        setFields(res.data ?? []);
        setIsLoadingTracker(false);
    };

    const canSubmit =
        !!trackerId && followLinksComplete(filterLinks, filterCandidates, fields);

    const handleSubmit = async () => {
        if (!canSubmit || !trackerId) return;
        setIsSubmitting(true);
        await onAdd(
            {
                trackerId,
                name: name.trim() || undefined,
                columnFieldIds: columnFieldIds.length ? columnFieldIds : undefined,
                displayMode,
                mobileDisplayMode,
            },
            { trackerId, links: filterLinks },
        );
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

            {trackerId && !isLoadingTracker && (
                <FilterFollowChecklist
                    fields={fields}
                    filters={filterCandidates}
                    links={filterLinks}
                    onLinksChange={setFilterLinks}
                />
            )}

            <WidgetDisplayModeFields
                displayMode={displayMode}
                mobileDisplayMode={mobileDisplayMode}
                onDisplayModeChange={setDisplayMode}
                onMobileDisplayModeChange={setMobileDisplayMode}
            />

            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onBack}>
                    Back
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
    );
}
