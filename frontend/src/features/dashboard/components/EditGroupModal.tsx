import { Button, Group, Modal, Stack, Text, TextInput } from "@mantine/core";
import { useMemo, useState } from "react";
import {
    DashboardItemDisplayMode,
    DashboardWidgetDto,
    WidgetTypes,
} from "../types/DashboardDto";
import { useDashboard } from "../context/DashboardContext";
import { WidgetChecklist } from "./WidgetChecklist";

interface Props {
    groupId: string;
    initialName: string;
    widgets: DashboardWidgetDto[];
    color: string;
    onClose: () => void;
}

// Kept in step with DataLimits.MaxHeaderTextLength on the backend (shared by every
// Config-backed text field, container names included).
const MAX_NAME_LENGTH = 100;

/** Renames a group and reconciles its membership in one dialog, mirroring the picker
    shown when a group is first created -- current members arrive pre-checked, and
    unchecking one leaves it in place on the board rather than deleting it. */
export function EditGroupModal({ groupId, initialName, widgets, color, onClose }: Props) {
    const { setTextContent, updateGroupMembers } = useDashboard();
    const [name, setName] = useState(initialName);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const currentMemberIds = useMemo(
        () => new Set(widgets.filter((w) => w.parentItemId === groupId).map((w) => w.id)),
        [widgets, groupId],
    );
    const [selected, setSelected] = useState<Set<string>>(currentMemberIds);

    // A candidate is either already in this group, or free to join it: not hidden, not a
    // container itself, and not already tagged onto a different group/tabs container.
    const candidates = widgets.filter(
        (w) =>
            w.type !== WidgetTypes.Container &&
            w.type !== WidgetTypes.TabsContainer &&
            w.layout.displayMode !== DashboardItemDisplayMode.Hidden &&
            (!w.parentItemId || currentMemberIds.has(w.id)),
    );

    const toggle = (id: string) =>
        setSelected((prev) => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });

    const handleSave = async () => {
        if (selected.size === 0) return;
        setIsSubmitting(true);
        try {
            const trimmed = name.trim();
            if (trimmed !== initialName) await setTextContent(groupId, trimmed);
            await updateGroupMembers(groupId, [...selected]);
            onClose();
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <Modal opened onClose={onClose} title="Edit group" size="md" centered>
            <Stack gap="md">
                <TextInput
                    label="Name"
                    placeholder="Group"
                    maxLength={MAX_NAME_LENGTH}
                    value={name}
                    onChange={(event) => setName(event.currentTarget.value)}
                    data-autofocus
                />

                {candidates.length === 0 ? (
                    <Text size="sm" c="dimmed">
                        No widgets available to group.
                    </Text>
                ) : (
                    <WidgetChecklist
                        widgets={candidates}
                        color={color}
                        selected={selected}
                        onToggle={toggle}
                    />
                )}

                <Group justify="space-between" mt="sm">
                    <Text size="xs" c="dimmed">
                        {selected.size} selected
                    </Text>
                    <Group>
                        <Button variant="default" onClick={onClose}>
                            Cancel
                        </Button>
                        <Button
                            disabled={selected.size === 0}
                            loading={isSubmitting}
                            onClick={handleSave}
                        >
                            Save
                        </Button>
                    </Group>
                </Group>
            </Stack>
        </Modal>
    );
}
