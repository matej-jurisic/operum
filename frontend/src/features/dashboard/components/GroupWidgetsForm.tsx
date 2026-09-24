import { Button, Group, Stack, Text } from "@mantine/core";
import { useState } from "react";
import {
    DashboardItemDisplayMode,
    DashboardWidgetDto,
    WidgetTypes,
} from "../types/DashboardDto";
import { WidgetChecklist } from "./WidgetChecklist";

interface Props {
    widgets: DashboardWidgetDto[];
    color: string;
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (itemIds: string[]) => Promise<void>;
}

const MIN_SELECTION = 2;

/** Creates a container by tagging the chosen widgets as its members: grouping has no
    "empty container" state to drop onto the board anymore, so this is the only way in. */
export function GroupWidgetsForm({ widgets, color, onBack, onAdd }: Props) {
    const [selected, setSelected] = useState<Set<string>>(new Set());
    const [isSubmitting, setIsSubmitting] = useState(false);

    // Only a widget that's not already part of some grouping and sits on the board itself
    // (not nested in a tabs container) can be picked -- the same set a plain drag would be
    // able to reach on the root grid.
    const candidates = widgets.filter(
        (w) =>
            !w.parentItemId &&
            w.type !== WidgetTypes.Container &&
            w.type !== WidgetTypes.TabsContainer &&
            w.layout.displayMode !== DashboardItemDisplayMode.Hidden,
    );

    const toggle = (id: string) =>
        setSelected((prev) => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });

    const handleSubmit = async () => {
        if (selected.size < MIN_SELECTION) return;
        setIsSubmitting(true);
        await onAdd([...selected]);
        setIsSubmitting(false);
    };

    return (
        <Stack gap="md">
            {candidates.length < MIN_SELECTION ? (
                <Text size="sm" c="dimmed">
                    Add at least two other widgets to the board before grouping them.
                </Text>
            ) : (
                <>
                    <Text size="sm" c="dimmed">
                        Pick the widgets to group together.
                    </Text>
                    <WidgetChecklist
                        widgets={candidates}
                        color={color}
                        selected={selected}
                        onToggle={toggle}
                    />
                </>
            )}

            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onBack}>
                    Back
                </Button>
                <Button
                    disabled={selected.size < MIN_SELECTION}
                    loading={isSubmitting}
                    onClick={handleSubmit}
                >
                    Create group{selected.size > 0 ? ` (${selected.size})` : ""}
                </Button>
            </Group>
        </Stack>
    );
}
