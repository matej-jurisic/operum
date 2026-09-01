import { Button, Group, Modal, Stack } from "@mantine/core";
import { useEffect, useState } from "react";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { useDashboard } from "../context/DashboardContext";
import { UpdateDashboardEntriesItemDto, WidgetTypes } from "../types/DashboardDto";
import { ExpandableOptionFields } from "./ExpandableOptionFields";
import { SourceViewSelect, ViewSelection } from "./SourceViewSelect";

interface Props {
    itemId: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, dto: UpdateDashboardEntriesItemDto) => Promise<void>;
}

interface EntriesWidgetConfig {
    viewId?: string | null;
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
 * here: which view its table reads through (or which View widget it follows instead), and
 * whether it collapses to a button on each grid. The tracker it reads from is fixed at add
 * time, the same as an Analytic widget's definition is — changing that means adding a new
 * widget rather than quietly turning this one into a table over something else.
 */
export function EditEntriesWidgetModal({ itemId, color, onClose, onSave }: Props) {
    const { widgets } = useDashboard();
    const widget = widgets.find((w) => w.id === itemId);
    const config = widget?.type === WidgetTypes.Entries ? parseEntriesConfig(widget.config) : null;

    const [views, setViews] = useState<ViewDto[] | null>(null);
    const [selection, setSelection] = useState<ViewSelection>({
        viewId: config?.viewId ?? null,
    });
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
        viewsController.getViewList(trackerId).then((res) => setViews(res.data ?? []));
    }, [trackerId, onClose]);

    const handleSubmit = async () => {
        setIsSubmitting(true);
        try {
            await onSave(itemId, {
                viewId: selection.viewId,
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
            {views && (
                <Stack gap="md">
                    <SourceViewSelect
                        views={views}
                        value={selection}
                        onChange={setSelection}
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
