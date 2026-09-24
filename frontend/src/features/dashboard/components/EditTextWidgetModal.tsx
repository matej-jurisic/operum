import { Button, Group, Modal, Stack, Textarea, TextInput } from "@mantine/core";
import { useState } from "react";

interface Props {
    itemId: string;
    kind: "header" | "note" | "tabsContainer";
    initialText: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, text: string) => Promise<void>;
}

// Kept in step with DataLimits.MaxHeaderTextLength / MaxNoteTextLength on the backend.
const MAX_LENGTH: Record<Props["kind"], number> = {
    header: 100,
    note: 500,
    tabsContainer: 100,
};

const COPY: Record<Props["kind"], { title: string; label: string; placeholder: string }> = {
    header: { title: "Edit header", label: "Text", placeholder: "Section title" },
    note: {
        title: "Edit note",
        label: "Text",
        placeholder: "Anything worth keeping on the dashboard",
    },
    tabsContainer: {
        title: "Rename tabs container",
        label: "Name",
        placeholder: "Tabs container",
    },
};

/** Unlike EditWidgetModal, needs no fetch: the text is already in Config, which the board holds. */
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
