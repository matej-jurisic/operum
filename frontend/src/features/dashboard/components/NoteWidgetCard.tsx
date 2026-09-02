import { ActionIcon, ScrollArea, Text, Textarea } from "@mantine/core";
import { useEffect, useRef, useState } from "react";
import { MdEdit } from "react-icons/md";
import {
    cardBodyProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";
import { WidgetShell } from "../../analytics/components/WidgetShell";
import { useDashboard } from "../context/DashboardContext";
import { TextWidgetConfig } from "../types/DashboardDto";

// Kept in step with DataLimits.MaxNoteTextLength on the backend, same as EditTextWidgetModal.
const MAX_LENGTH = 500;

interface Props {
    widgetId: string;
    config: TextWidgetConfig | null;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    onEdit?: (itemId: string) => void;
}

/**
 * A board widget that draws no data at all: a free-form block of text for context that
 * isn't any tracker's own — a reminder, a link, a note to whoever else can see the board.
 *
 * The text is edited in place: clicking it while the board is just being read swaps in a
 * field that saves on blur (or ⌘/Ctrl+Enter, Escape to drop the change). Arrange mode
 * still routes edits through the same dialog a Header widget uses, since the body can't be
 * clicked while every pointer gesture there belongs to the grid.
 */
export function NoteWidgetCard({
    widgetId,
    config,
    color,
    isConfiguring,
    onRemove,
    onEdit,
}: Props) {
    const layout = useCardLayout(true);
    const { setTextContent } = useDashboard();

    const text = config?.text ?? "";
    const [isEditing, setIsEditing] = useState(false);
    const [draft, setDraft] = useState(text);
    const [isSaving, setIsSaving] = useState(false);
    // Set just before the field is blurred by Escape, so the blur that follows drops the
    // change instead of saving it — blur is the one path a commit ever runs through.
    const cancelRef = useRef(false);
    const fieldRef = useRef<HTMLTextAreaElement>(null);

    // Outside of an active edit the field tracks the widget's stored text, so a change
    // saved from the Arrange-mode dialog still shows through here.
    useEffect(() => {
        if (!isEditing) setDraft(text);
    }, [text, isEditing]);

    // Land the cursor at the end of the existing text rather than selecting all of it.
    useEffect(() => {
        if (!isEditing) return;
        const field = fieldRef.current;
        if (!field) return;
        field.focus();
        field.setSelectionRange(field.value.length, field.value.length);
    }, [isEditing]);

    const startEditing = () => {
        if (isConfiguring) return;
        setDraft(text);
        setIsEditing(true);
    };

    const commit = async () => {
        if (cancelRef.current) {
            cancelRef.current = false;
            setDraft(text);
            setIsEditing(false);
            return;
        }

        const trimmed = draft.trim();
        // Nothing changed, or the note was cleared — drop back to display without a request.
        if (!trimmed || trimmed === text) {
            setDraft(text);
            setIsEditing(false);
            return;
        }

        setIsSaving(true);
        try {
            await setTextContent(widgetId, trimmed);
            setIsEditing(false);
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <WidgetShell
            layout={layout}
            fillHeight
            isConfiguring={isConfiguring}
            color={color}
            itemId={widgetId}
            onRemove={onRemove}
            onEdit={onEdit}
            title="Note"
            headerActions={
                // Arrange mode has its own edit icon (and routes to the dialog,
                // since the body can't be clicked there); this one is the
                // in-place edit offered while the board is just being read.
                !isConfiguring && !isEditing ? (
                    <ActionIcon
                        size="md"
                        color={color}
                        variant="outline"
                        aria-label="Edit note"
                        style={{ pointerEvents: "auto" }}
                        onClick={startEditing}
                    >
                        <MdEdit size={18} />
                    </ActionIcon>
                ) : undefined
            }
        >
            <ScrollArea
                style={{
                    ...cardBodyProps(true).style,
                    // Not a control the board can drag by, but arranging the board
                    // still takes over every pointer gesture inside it — same as the
                    // Entries widget's table.
                    pointerEvents: isConfiguring ? "none" : "auto",
                }}
            >
                {isEditing ? (
                    <Textarea
                        ref={fieldRef}
                        autosize
                        minRows={3}
                        maxRows={12}
                        maxLength={MAX_LENGTH}
                        value={draft}
                        disabled={isSaving}
                        onChange={(event) =>
                            setDraft(event.currentTarget.value)
                        }
                        onBlur={commit}
                        onKeyDown={(event) => {
                            if (event.key === "Escape") {
                                event.preventDefault();
                                cancelRef.current = true;
                                event.currentTarget.blur();
                            } else if (
                                event.key === "Enter" &&
                                (event.metaKey || event.ctrlKey)
                            ) {
                                event.preventDefault();
                                event.currentTarget.blur();
                            }
                        }}
                    />
                ) : (
                    <Text
                        size="sm"
                        c={text ? undefined : "dimmed"}
                        onClick={startEditing}
                        style={{
                            whiteSpace: "pre-wrap",
                            cursor: isConfiguring ? "default" : "text",
                        }}
                    >
                        {text ||
                            (isConfiguring
                                ? "This note is empty."
                                : "Empty note. Click to add text.")}
                    </Text>
                )}
            </ScrollArea>
        </WidgetShell>
    );
}
