import { Button, Group, Modal, MultiSelect, Stack } from "@mantine/core";
import { useEffect, useState } from "react";
import { fieldsController } from "../../fields/api/fieldsController";
import { FieldDto } from "../../fields/types/FieldDto";
import { useDashboard } from "../context/DashboardContext";
import { UpdateDashboardEntriesItemDto, WidgetTypes } from "../types/DashboardDto";
import { ExpandableOptionFields } from "./ExpandableOptionFields";

interface Props {
    itemId: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, dto: UpdateDashboardEntriesItemDto) => Promise<void>;
}

interface EntriesWidgetConfig {
    columnFieldIds?: string[];
}

// The widget's own Config is already sitting in the board the context holds — same as a
// QuickAdd or View widget's — so there's nothing to fetch before this can read it. Which
// tracker the table reads from isn't part of Config at all -- it's fixed on the shared
// EntriesWidget definition and comes from the rendered entriesWidget field instead.
function parseEntriesConfig(config: string | undefined): EntriesWidgetConfig | null {
    if (!config) return null;
    try {
        return JSON.parse(config);
    } catch {
        return null;
    }
}

/**
 * Edits an Entries widget after it has been placed. Only what the board itself decides is
 * here: which of the tracker's fields it shows as columns, and whether it collapses to a
 * button on each grid. The tracker it reads from is fixed at add time, and how it's
 * filtered comes only from the View Selector widgets it's linked to.
 */
export function EditEntriesWidgetModal({ itemId, color, onClose, onSave }: Props) {
    const { widgets } = useDashboard();
    const widget = widgets.find((w) => w.id === itemId);
    const config = widget?.type === WidgetTypes.Entries ? parseEntriesConfig(widget.config) : null;

    const [fields, setFields] = useState<FieldDto[] | null>(null);
    const [columnFieldIds, setColumnFieldIds] = useState<string[]>(
        config?.columnFieldIds ?? [],
    );
    const [expandable, setExpandable] = useState(widget?.layout.expandable ?? false);
    const [mobileExpandable, setMobileExpandable] = useState(widget?.mobileLayout.expandable ?? false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const trackerId =
        widget?.type === WidgetTypes.Entries ? widget.entriesWidget?.trackerId : undefined;

    useEffect(() => {
        if (!trackerId) {
            onClose();
            return;
        }
        fieldsController.getFields(trackerId).then((res) => setFields(res.data ?? []));
    }, [trackerId, onClose]);

    const handleSubmit = async () => {
        setIsSubmitting(true);
        try {
            await onSave(itemId, {
                columnFieldIds: columnFieldIds.length ? columnFieldIds : undefined,
                expandable,
                mobileExpandable,
            });
        } finally {
            setIsSubmitting(false);
        }

        onClose();
    };

    return (
        <Modal opened onClose={onClose} title="Edit widget" size="md" centered>
            {/* The global request loader already covers the fetch above, so this renders
                nothing rather than stacking a second spinner on top of it. */}
            {fields && (
                <Stack gap="md">
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

                    <ExpandableOptionFields
                        expandable={expandable}
                        mobileExpandable={mobileExpandable}
                        onExpandableChange={setExpandable}
                        onMobileExpandableChange={setMobileExpandable}
                    />

                    <Group justify="flex-end" mt="sm">
                        <Button variant="default" onClick={onClose}>
                            Cancel
                        </Button>
                        <Button
                            color={color}
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
