import { Button, Checkbox, Group, Modal, Select, Stack, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import {
    DashboardItemDto,
    UpdateDashboardViewItemDto,
    WidgetTypes,
} from "../types/DashboardDto";
import {
    linkTargetsForViewWidget,
    ViewWidgetLinkTarget,
} from "./SourceViewSelect";

const ALL_ENTRIES_VALUE = "";

interface Props {
    itemId: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, dto: UpdateDashboardViewItemDto) => Promise<void>;
}

/**
 * Edits a View selector after it has been placed. What the board decides lives here: which
 * view its dropdown starts on, and which Analytic/Entries widgets on the board follow it.
 * The tracker it lists views for is fixed at add time — changing that would orphan every
 * link, so it means adding a new selector instead.
 *
 * The linked-widget set is read from the board itself (the render list has no source
 * filters on it), so this fetches the dashboard the same way EditWidgetModal does.
 */
export function EditViewWidgetModal({ itemId, color, onClose, onSave }: Props) {
    const { dashboardId, widgets } = useDashboard();
    const isMobile = useMediaQuery("(max-width: 48em)");
    const widget = widgets.find((w) => w.id === itemId);
    const viewWidget =
        widget?.type === WidgetTypes.View ? widget.viewWidget : undefined;
    const trackerId = viewWidget?.trackerId;

    const [targets, setTargets] = useState<ViewWidgetLinkTarget[] | null>(null);
    const [viewId, setViewId] = useState<string | null>(viewWidget?.viewId ?? null);
    const [linkedItemIds, setLinkedItemIds] = useState<string[]>([]);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        if (!trackerId) {
            onClose();
            return;
        }

        dashboardController.getDashboard(dashboardId).then((res) => {
            const items: DashboardItemDto[] = res.data?.items ?? [];
            const linkTargets = linkTargetsForViewWidget(items, trackerId, itemId);
            setTargets(linkTargets);
            setLinkedItemIds(linkTargets.filter((t) => t.linked).map((t) => t.itemId));
        });
    }, [dashboardId, itemId, trackerId, onClose]);

    const toggleLinked = (targetId: string, checked: boolean) =>
        setLinkedItemIds((current) =>
            checked
                ? [...current, targetId]
                : current.filter((id) => id !== targetId),
        );

    const handleSubmit = async () => {
        setIsSubmitting(true);
        try {
            await onSave(itemId, { viewId, linkedItemIds });
        } finally {
            setIsSubmitting(false);
        }
        onClose();
    };

    const viewOptions = [
        { value: ALL_ENTRIES_VALUE, label: "All entries" },
        ...(viewWidget?.views.map((v) => ({ value: v.id, label: v.name })) ?? []),
    ];

    return (
        <Modal
            opened
            onClose={onClose}
            title="Edit view selector"
            size="md"
            centered
            fullScreen={isMobile}
        >
            {/* The global request loader covers the fetch above, so this renders nothing
                rather than stacking a second spinner on it. */}
            {targets && (
                <Stack gap="md">
                    <Select
                        label="Starting view"
                        data={viewOptions}
                        value={viewId ?? ALL_ENTRIES_VALUE}
                        onChange={(value) =>
                            setViewId(value && value !== ALL_ENTRIES_VALUE ? value : null)
                        }
                        allowDeselect={false}
                        comboboxProps={{ withinPortal: true }}
                    />

                    <Stack gap="xs">
                        <Text size="sm" fw={500}>
                            Linked widgets
                        </Text>
                        {targets.length === 0 ? (
                            <Text size="xs" c="dimmed">
                                No other widgets on this board read from this tracker yet.
                            </Text>
                        ) : (
                            <>
                                <Text size="xs" c="dimmed">
                                    Ticked widgets follow this selector's dropdown.
                                </Text>
                                {targets.map((target) => (
                                    <Checkbox
                                        key={target.itemId}
                                        label={target.label}
                                        description={target.note}
                                        checked={linkedItemIds.includes(target.itemId)}
                                        onChange={(event) =>
                                            toggleLinked(
                                                target.itemId,
                                                event.currentTarget.checked,
                                            )
                                        }
                                    />
                                ))}
                            </>
                        )}
                    </Stack>

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
