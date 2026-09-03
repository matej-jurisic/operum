import { Button, Group, Modal, Stack, Textarea, TextInput } from "@mantine/core";
import { useState } from "react";

interface Props {
    itemId: string;
    /** Which text widget this is editing: a header and a container title get a single-line
        input capped short, a note gets a multi-line one with room for a paragraph. */
    kind: "header" | "note" | "container";
    initialText: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, text: string) => Promise<void>;
}

// Kept in step with DataLimits.MaxHeaderTextLength / MaxNoteTextLength on the backend. A
// container title shares the header's cap.
const MAX_LENGTH: Record<Props["kind"], number> = {
    header: 100,
    note: 500,
    container: 100,
};

const COPY: Record<Props["kind"], { title: string; label: string; placeholder: string }> = {
    header: { title: "Edit header", label: "Text", placeholder: "Section title" },
    note: {
        title: "Edit note",
        label: "Text",
        placeholder: "Anything worth keeping on the board",
    },
    container: { title: "Rename container", label: "Name", placeholder: "Container" },
};

/**
 * Edits a Header or Note widget's text, or a Container's title, after it has been placed.
 * Unlike EditWidgetModal this needs no fetch first — the text is already sitting in the
 * widget's own Config, which the board already holds — so there's nothing to load before
 * the field can be shown.
 */
export function EditTextWidgetModal({ itemId, kind, initialText, color, onClose, onSave }: Props) {
    const [text, setText] = useState(initialText);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const maxLength = MAX_LENGTH[kind];
    const copy = COPY[kind];
    const singleLine = kind !== "note";
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
        <Modal opened onClose={onClose} title={copy.title} size="md" centered>
            <Stack gap="md">
                {singleLine ? (
                    <TextInput
                        label={copy.label}
                        placeholder={copy.placeholder}
                        maxLength={maxLength}
                        value={text}
                        onChange={(event) => setText(event.currentTarget.value)}
                        data-autofocus
                    />
                ) : (
                    <Textarea
                        label={copy.label}
                        placeholder={copy.placeholder}
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
