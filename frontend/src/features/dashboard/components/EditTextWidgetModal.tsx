import { Button, Group, Modal, Stack, Textarea, TextInput } from "@mantine/core";
import { useState } from "react";

interface Props {
    itemId: string;
    /** Which of the two text widgets this is editing: a header gets a single-line input
        capped short, a note gets a multi-line one with room for a paragraph. */
    kind: "header" | "note";
    initialText: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, text: string) => Promise<void>;
}

// Kept in step with DataLimits.MaxHeaderTextLength / MaxNoteTextLength on the backend.
const MAX_LENGTH: Record<Props["kind"], number> = {
    header: 100,
    note: 500,
};

/**
 * Edits a Header or Note widget's text after it has been placed. Unlike EditWidgetModal
 * this needs no fetch first — the text is already sitting in the widget's own Config, which
 * the board already holds — so there's nothing to load before the field can be shown.
 */
export function EditTextWidgetModal({ itemId, kind, initialText, color, onClose, onSave }: Props) {
    const [text, setText] = useState(initialText);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const maxLength = MAX_LENGTH[kind];
    const trimmed = text.trim();

    const handleSubmit = async () => {
        if (!trimmed) return;

        setIsSubmitting(true);
        try {
            await onSave(itemId, trimmed);
        } finally {
            setIsSubmitting(false);
        }

        onClose();
    };

    return (
        <Modal
            opened
            onClose={onClose}
            title={kind === "header" ? "Edit header" : "Edit note"}
            size="md"
            centered
        >
            <Stack gap="md">
                {kind === "header" ? (
                    <TextInput
                        label="Text"
                        placeholder="Section title"
                        maxLength={maxLength}
                        value={text}
                        onChange={(event) => setText(event.currentTarget.value)}
                        data-autofocus
                    />
                ) : (
                    <Textarea
                        label="Text"
                        placeholder="Anything worth keeping on the board"
                        autosize
                        minRows={4}
                        maxRows={10}
                        maxLength={maxLength}
                        value={text}
                        onChange={(event) => setText(event.currentTarget.value)}
                        data-autofocus
                    />
                )}

                <Group justify="flex-end" mt="sm">
                    <Button variant="default" onClick={onClose}>
                        Cancel
                    </Button>
                    <Button
                        color={color}
                        loading={isSubmitting}
                        disabled={!trimmed}
                        onClick={handleSubmit}
                    >
                        Save
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
