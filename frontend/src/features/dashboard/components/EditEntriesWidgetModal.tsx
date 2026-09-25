import { Button, Group, Modal, MultiSelect, Stack, TextInput } from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { useDashboard } from "../context/DashboardContext";
import {
    DashboardItemDisplayMode,
    parseFilterWidgetConfig,
    UpdateDashboardEntriesItemDto,
    WidgetTypes,
} from "../types/DashboardDto";
import { FilterFollowChecklist } from "./FilterFollowChecklist";
import { filterCandidatesFor, followLinksComplete } from "./filterLinkUtils";
import { SourceViewSelect } from "./SourceViewSelect";
import { WidgetDisplayModeFields } from "./WidgetDisplayModeFields";

interface Props {
    itemId: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, dto: UpdateDashboardEntriesItemDto) => Promise<void>;
}

interface EntriesWidgetConfig {
    columnFieldIds?: string[];
    viewId?: string | null;
}

// The tracker isn't part of Config; it's fixed on the shared EntriesWidget definition
// and comes from the rendered entriesWidget field instead.
function parseEntriesConfig(config: string | undefined): EntriesWidgetConfig | null {
    if (!config) return null;
    try {
        return JSON.parse(config);
    } catch {
        return null;
    }
}

/** The tracker is fixed at add time; filtering comes only from linked View Selector widgets. */
export function EditEntriesWidgetModal({ itemId, color, onClose, onSave }: Props) {
    const { widgets, syncFilterFollows } = useDashboard();
    const widget = widgets.find((w) => w.id === itemId);
    const config = widget?.type === WidgetTypes.Entries ? parseEntriesConfig(widget.config) : null;
    const filterCandidates = useMemo(() => filterCandidatesFor(widgets), [widgets]);

    const [fields, setFields] = useState<FieldDto[] | null>(null);
    const [name, setName] = useState(widget?.entriesWidget?.rawName ?? "");
    const [columnFieldIds, setColumnFieldIds] = useState<string[]>(
        config?.columnFieldIds ?? [],
    );
    const [views, setViews] = useState<ViewDto[]>([]);
    const [viewId, setViewId] = useState<string | null>(config?.viewId ?? null);
    const [displayMode, setDisplayMode] = useState(
        widget?.layout.displayMode ?? DashboardItemDisplayMode.Full,
    );
    const [mobileDisplayMode, setMobileDisplayMode] = useState(
        widget?.mobileLayout.displayMode ?? DashboardItemDisplayMode.Full,
    );
    const [isSubmitting, setIsSubmitting] = useState(false);

    const trackerId =
        widget?.type === WidgetTypes.Entries ? widget.entriesWidget?.trackerId : undefined;

    // Reconstructed from the board's filter widgets, whose config already keys fieldByQuery
    // by clause slot id -- exactly what FilterFollowChecklist expects.
    const [filterLinks, setFilterLinks] = useState<Record<string, Record<string, string>>>(
        () => {
            const out: Record<string, Record<string, string>> = {};
            for (const w of widgets) {
                if (w.type !== WidgetTypes.Filter) continue;
                const link = parseFilterWidgetConfig(w.config)?.links.find(
                    (l) => l.itemId === itemId && l.trackerId === trackerId,
                );
                if (link) out[w.id] = link.fieldByQuery;
            }
            return out;
        },
    );

    useEffect(() => {
        if (!trackerId) {
            onClose();
            return;
        }
        Promise.all([
            fieldsController.getFields(trackerId),
            viewsController.getViewList(trackerId),
        ]).then(([fieldsRes, viewsRes]) => {
            setViews(viewsRes.data ?? []);
            setFields(fieldsRes.data ?? []);
        });
    }, [trackerId, onClose]);

    const canSubmit =
        !!fields && followLinksComplete(filterLinks, filterCandidates, fields);

    const handleSubmit = async () => {
        setIsSubmitting(true);
        try {
            if (trackerId) {
                // Applied first: this table's follow links must land before the field/column save.
                await syncFilterFollows(itemId, [{ trackerId, links: filterLinks }]);
            }
            await onSave(itemId, {
                name: name.trim(),
                columnFieldIds: columnFieldIds.length ? columnFieldIds : undefined,
                viewId,
                displayMode,
                mobileDisplayMode,
            });
        } finally {
            setIsSubmitting(false);
        }

        onClose();
    };

    return (
        <Modal opened onClose={onClose} title="Edit widget" size="md" centered>
            {/* Global request loader already covers the fetch above. */}
            {fields && (
                <Stack gap="md">
                    <TextInput
                        label="Name"
                        placeholder={widget?.entriesWidget?.trackerName ?? "Optional"}
                        maxLength={100}
                        value={name}
                        onChange={(event) => setName(event.currentTarget.value)}
                    />

                    <MultiSelect
                        label="Columns"
                        description="Leave empty to show every field"
                        placeholder={columnFieldIds.length > 0 ? undefined : "Every field"}
                        data={fields.map((f) => ({ value: f.id, label: f.name }))}
                        value={columnFieldIds}
                        onChange={setColumnFieldIds}
                        searchable
                        clearable
                    />

                    <SourceViewSelect
                        views={views}
                        value={{ viewId }}
                        onChange={(selection) => setViewId(selection.viewId)}
                    />

                    <FilterFollowChecklist
                        fields={fields}
                        filters={filterCandidates}
                        links={filterLinks}
                        onLinksChange={setFilterLinks}
                    />

                    <WidgetDisplayModeFields
                        displayMode={displayMode}
                        mobileDisplayMode={mobileDisplayMode}
                        onDisplayModeChange={setDisplayMode}
                        onMobileDisplayModeChange={setMobileDisplayMode}
                    />

                    <Group justify="flex-end" mt="sm">
                        <Button variant="default" onClick={onClose}>
                            Cancel
                        </Button>
                        <Button
                            color={color}
                            disabled={!canSubmit}
                            loading={isSubmitting}
                            onClick={handleSubmit}
                        >
                            Save
                        </Button>
                    </Group>
                </Stack>
            )}
        </Modal>
    );
}
