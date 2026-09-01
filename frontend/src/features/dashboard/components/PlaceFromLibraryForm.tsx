import { Alert, Button, Group, MultiSelect, Paper, Select, Stack, Text, TextInput } from "@mantine/core";
import { useEffect, useState } from "react";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { trackersController } from "../../trackers/api/trackersController";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { WidgetDto, EntriesWidgetDefinitionDto } from "../../widgets/types/WidgetDto";
import { useWidgets } from "../../widgets/context/WidgetsContext";
import { PlaceEntriesWidgetDto, PlaceWidgetDto } from "../types/DashboardDto";
import { ExpandableOptionFields } from "./ExpandableOptionFields";
import { SourceViewSelect, ViewSelection } from "./SourceViewSelect";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onPlaceWidget: (dto: PlaceWidgetDto) => Promise<void>;
    onPlaceEntriesWidget: (dto: PlaceEntriesWidgetDto) => Promise<void>;
    /** When the library item is already chosen (the Charts/Tables tab's "Add" action), the
        form drops its own tracker/widget pickers and only shows the placement settings. */
    presetWidget?: WidgetDto;
    presetEntriesWidget?: EntriesWidgetDefinitionDto;
}

// Prefixes distinguishing a chart Widget's id from an EntriesWidget's in the one picker
// below, the same trick SourceViewSelect uses to offer a fixed view and a linked one from
// a single Select.
const WIDGET_PREFIX = "widget:";
const ENTRIES_PREFIX = "entries:";

type SourceOverride = ViewSelection & { label: string };

/**
 * Places an existing Widget Library chart or Entries table onto this board by reference:
 * unlike the old copy-on-add, nothing is duplicated here. Editing the widget afterwards --
 * in the Library, or from any other dashboard placing it -- changes what this placement
 * draws too.
 */
export function PlaceFromLibraryForm({
    onBack,
    onPlaceWidget,
    onPlaceEntriesWidget,
    presetWidget,
    presetEntriesWidget,
}: Props) {
    const { widgets, entriesWidgets, isLoading: isLoadingLibrary } = useWidgets();
    const preset = presetWidget
        ? `${WIDGET_PREFIX}${presetWidget.id}`
        : presetEntriesWidget
        ? `${ENTRIES_PREFIX}${presetEntriesWidget.id}`
        : null;
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [trackerFilter, setTrackerFilter] = useState<string | null>(null);
    const [selection, setSelection] = useState<string | null>(preset);
    const [viewsByTracker, setViewsByTracker] = useState<Map<string, ViewDto[]>>(new Map());
    const [sourceOverrides, setSourceOverrides] = useState<Record<string, SourceOverride>>({});
    const [entriesFields, setEntriesFields] = useState<FieldDto[]>([]);
    const [entriesColumnFieldIds, setEntriesColumnFieldIds] = useState<string[]>([]);
    const [expandable, setExpandable] = useState(false);
    const [mobileExpandable, setMobileExpandable] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        trackersController.getTrackerList("Accessible").then((res) => {
            setTrackers(res.data ?? []);
        });
    }, []);

    const filteredWidgets = trackerFilter
        ? widgets.filter((w) => w.sources.some((s) => s.trackerId === trackerFilter))
        : widgets;
    const filteredEntriesWidgets = trackerFilter
        ? entriesWidgets.filter((w) => w.trackerId === trackerFilter)
        : entriesWidgets;

    const selectedWidget = selection?.startsWith(WIDGET_PREFIX)
        ? widgets.find((w) => w.id === selection.slice(WIDGET_PREFIX.length))
        : undefined;
    const selectedEntriesWidget = selection?.startsWith(ENTRIES_PREFIX)
        ? entriesWidgets.find((w) => w.id === selection.slice(ENTRIES_PREFIX.length))
        : undefined;

    // Loads what the current pick needs -- a chart's per-tracker views, or an Entries
    // table's tracker fields for its column picker -- and resets the placement-only fields
    // below it, which belong to whatever was selected before.
    useEffect(() => {
        if (selectedWidget) {
            const trackerIds = [...new Set(selectedWidget.sources.map((s) => s.trackerId))];
            Promise.all(
                trackerIds.map(async (trackerId) => {
                    const res = await viewsController.getViewList(trackerId);
                    return [trackerId, res.data ?? []] as const;
                })
            ).then((entries) => setViewsByTracker(new Map(entries)));
        } else if (selectedEntriesWidget) {
            fieldsController
                .getFields(selectedEntriesWidget.trackerId)
                .then((res) => setEntriesFields(res.data ?? []));
        }

        setSourceOverrides(
            selectedWidget
                ? Object.fromEntries(
                      selectedWidget.sources.map((s) => [
                          s.id,
                          { viewId: null, label: "" },
                      ])
                  )
                : {}
        );
        setEntriesFields([]);
        setEntriesColumnFieldIds([]);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selection]);

    const handleSubmit = async () => {
        setIsSubmitting(true);
        try {
            if (selectedWidget) {
                await onPlaceWidget({
                    widgetId: selectedWidget.id,
                    expandable,
                    mobileExpandable,
                    sourceOverrides: selectedWidget.sources.map((source) => {
                        const override = sourceOverrides[source.id];
                        return {
                            widgetSourceId: source.id,
                            label: override?.label.trim() || undefined,
                            viewId: override?.viewId ?? null,
                        };
                    }),
                });
            } else if (selectedEntriesWidget) {
                await onPlaceEntriesWidget({
                    entriesWidgetId: selectedEntriesWidget.id,
                    columnFieldIds: entriesColumnFieldIds.length
                        ? entriesColumnFieldIds
                        : undefined,
                    expandable,
                    mobileExpandable,
                });
            }
        } finally {
            setIsSubmitting(false);
        }
    };

    const libraryOptions = [
        {
            group: "Charts",
            items: filteredWidgets.map((w) => ({ value: `${WIDGET_PREFIX}${w.id}`, label: w.name })),
        },
        {
            group: "Entries tables",
            items: filteredEntriesWidgets.map((w) => ({
                value: `${ENTRIES_PREFIX}${w.id}`,
                label: w.name || w.trackerName,
            })),
        },
    ];

    const hasNothingInLibrary =
        !isLoadingLibrary && filteredWidgets.length === 0 && filteredEntriesWidgets.length === 0;

    return (
        <Stack gap="md">
            {!preset && (
                <Select
                    label="Tracker"
                    placeholder="All trackers"
                    data={trackers.map((t) => ({ value: t.id, label: t.name }))}
                    value={trackerFilter}
                    onChange={(value) => {
                        setTrackerFilter(value);
                        setSelection(null);
                    }}
                    clearable
                    searchable
                />
            )}

            {!preset && (
                <Select
                    label="Widget"
                    placeholder={isLoadingLibrary ? "Loading..." : "Select a widget"}
                    data={libraryOptions}
                    value={selection}
                    onChange={setSelection}
                    disabled={isLoadingLibrary}
                    searchable
                />
            )}

            {!preset && hasNothingInLibrary && (
                <Alert color="gray" variant="light">
                    Nothing in the Widget Library yet. Open the Widget Library from the
                    board menu to build a reusable one, or create a chart directly on this
                    board.
                </Alert>
            )}

            {selectedWidget &&
                selectedWidget.sources.map((source) => {
                    const override = sourceOverrides[source.id] ?? {
                        viewId: null,
                        label: "",
                    };

                    return (
                        <Paper key={source.id} withBorder p="sm" radius="md">
                            <Stack gap="sm">
                                <Text size="sm" fw={600}>
                                    {source.trackerName}
                                    {selectedWidget.sources.length > 1 ? ` · ${source.name}` : ""}
                                </Text>
                                <TextInput
                                    label={
                                        selectedWidget.sources.length > 1 ? "Series name" : "Name"
                                    }
                                    placeholder={source.name}
                                    maxLength={100}
                                    value={override.label}
                                    onChange={(event) =>
                                        setSourceOverrides((prev) => ({
                                            ...prev,
                                            [source.id]: { ...override, label: event.currentTarget.value },
                                        }))
                                    }
                                />
                                <SourceViewSelect
                                    views={viewsByTracker.get(source.trackerId) ?? []}
                                    value={{ viewId: override.viewId }}
                                    onChange={(selectionValue) =>
                                        setSourceOverrides((prev) => ({
                                            ...prev,
                                            [source.id]: { ...override, ...selectionValue },
                                        }))
                                    }
                                />
                            </Stack>
                        </Paper>
                    );
                })}

            {selectedEntriesWidget && (
                <MultiSelect
                    label="Columns"
                    description="Leave empty to show every field"
                    placeholder={
                        entriesColumnFieldIds.length > 0 ? undefined : "Every field"
                    }
                    data={entriesFields.map((f) => ({ value: f.id, label: f.name }))}
                    value={entriesColumnFieldIds}
                    onChange={setEntriesColumnFieldIds}
                    searchable
                    clearable
                />
            )}

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
                    disabled={!selectedWidget && !selectedEntriesWidget}
                    loading={isSubmitting}
                    onClick={handleSubmit}
                >
                    Add
                </Button>
            </Group>
        </Stack>
    );
}
