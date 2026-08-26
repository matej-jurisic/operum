import { Button, Group, Stack, Textarea } from "@mantine/core";
import { useState } from "react";
import { AddDashboardNoteItemDto } from "../types/DashboardDto";

interface Props {
    /** Steps back to the widget type picker. */
    onBack: () => void;
    onAdd: (dto: AddDashboardNoteItemDto) => Promise<void>;
}

// Kept in step with DataLimits.MaxNoteTextLength on the backend.
const MAX_LENGTH = 500;

/** Writes the free-form text a Note widget draws. Nothing else about it is configurable —
    it carries no tracker, no view, no chart. */
export function NoteWidgetForm({ onBack, onAdd }: Props) {
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
            <Textarea
                label="Text"
                placeholder="Anything worth keeping on the board"
                autosize
                minRows={4}
                maxRows={10}
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
