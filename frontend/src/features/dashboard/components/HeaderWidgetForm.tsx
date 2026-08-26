import { Button, Group, Stack, TextInput } from "@mantine/core";
import { useState } from "react";
import { AddDashboardHeaderItemDto } from "../types/DashboardDto";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: AddDashboardHeaderItemDto) => Promise<void>;
}

// Kept in step with DataLimits.MaxHeaderTextLength on the backend.
const MAX_LENGTH = 100;

/** Names the section title a Header widget draws. Nothing else about it is configurable —
    it carries no tracker, no view, no chart. */
export function HeaderWidgetForm({ onBack, onAdd }: Props) {
    const [text, setText] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);

    const trimmed = text.trim();

    const handleSubmit = async () => {
        if (!trimmed) return;
        setIsSubmitting(true);
        await onAdd({ text: trimmed });
        setIsSubmitting(false);
    };

    return (
        <Stack gap="md">
            <TextInput
                label="Text"
                placeholder="Section title"
                maxLength={MAX_LENGTH}
                value={text}
                onChange={(event) => setText(event.currentTarget.value)}
                data-autofocus
            />

            <Group justify="flex-end" mt="sm">
                <Button variant="default" onClick={onBack}>
                    Back
                </Button>
                <Button disabled={!trimmed} loading={isSubmitting} onClick={handleSubmit}>
                    Add
                </Button>
            </Group>
        </Stack>
    );
}
