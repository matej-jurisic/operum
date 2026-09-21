import {
    Alert,
    Button,
    Group,
    List,
    Modal,
    Stack,
    Textarea,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { MdErrorOutline, MdWarningAmber } from "react-icons/md";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { dashboardController } from "../api/dashboardController";
import {
    DASHBOARD_DOCUMENT_SCHEMA_VERSION,
    DashboardDocumentDto,
} from "../types/DashboardDto";

interface Props {
    /** Absent to build a new board from the document instead. */
    dashboardId?: string;
    color: string;
    onClose: () => void;
    onSaved: (dashboardId: string) => Promise<void>;
}

const messagesFrom = (error: unknown): string[] => {
    const messages = (error as ApiResponse | undefined)?.messages;
    return messages?.length ? messages : ["Something went wrong."];
};

const NEW_BOARD: DashboardDocumentDto = {
    schemaVersion: DASHBOARD_DOCUMENT_SCHEMA_VERSION,
    board: { name: "New board" },
    items: [],
};

/** The keys the document lists, or null while it isn't shaped like a document yet. */
const listedKeys = (parsed: unknown): Set<string> | null => {
    const items = (parsed as { items?: unknown } | null)?.items;
    if (!Array.isArray(items)) return null;
    return new Set(items.map((item) => (item as { key?: string } | null)?.key ?? ""));
};

export function BoardDocumentModal({ dashboardId, color, onClose, onSaved }: Props) {
    const isImport = !dashboardId;
    const [text, setText] = useState("");
    const [isLoaded, setIsLoaded] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [errors, setErrors] = useState<string[]>([]);
    // What the board held when the editor opened, so a widget the text no longer lists can
    // be named before it is deleted.
    const [onBoard, setOnBoard] = useState<Map<string, string>>(new Map());
    const [pendingDeletes, setPendingDeletes] = useState<string[]>([]);

    useEffect(() => {
        if (!dashboardId) {
            setText(JSON.stringify(NEW_BOARD, null, 2));
            setIsLoaded(true);
            return;
        }

        let cancelled = false;

        const load = async () => {
            try {
                const res = await dashboardController.getDashboardDocument(dashboardId);
                if (cancelled) return;
                setText(JSON.stringify(res.data, null, 2));
                setOnBoard(
                    new Map(res.data.items.map((i) => [i.key, i.name || i.type || i.key])),
                );
                setIsLoaded(true);
            } catch {
                if (!cancelled) onClose();
            }
        };

        load();
        return () => {
            cancelled = true;
        };
    }, [dashboardId, onClose]);

    const handleSave = async () => {
        let parsed: unknown;
        try {
            parsed = JSON.parse(text);
        } catch (error) {
            setErrors([`Not valid JSON: ${(error as Error).message}`]);
            return;
        }

        const listed = listedKeys(parsed);
        const removed = listed
            ? [...onBoard].filter(([key]) => !listed.has(key)).map(([, name]) => name)
            : [];

        if (removed.length > 0 && pendingDeletes.join("\n") !== removed.join("\n")) {
            setErrors([]);
            setPendingDeletes(removed);
            return;
        }

        setErrors([]);
        setIsSaving(true);
        try {
            if (dashboardId) {
                await dashboardController.saveDashboardDocument(dashboardId, parsed);
                await onSaved(dashboardId);
            } else {
                const res = await dashboardController.createDashboardFromDocument(parsed);
                await onSaved(res.data.id);
            }
            onClose();
        } catch (error) {
            setPendingDeletes([]);
            setErrors(messagesFrom(error));
        } finally {
            setIsSaving(false);
        }
    };

    const deleteCount = pendingDeletes.length;

    return (
        <Modal
            opened
            onClose={onClose}
            title={isImport ? "Import board from JSON" : "Edit board JSON"}
            size="xl"
            centered
        >
            <Stack gap="md">
                {errors.length > 0 && (
                    <Alert
                        color="red"
                        title="Nothing was saved"
                        icon={<MdErrorOutline size={18} />}
                    >
                        <List size="sm" spacing={4}>
                            {errors.map((message) => (
                                <List.Item key={message}>{message}</List.Item>
                            ))}
                        </List>
                    </Alert>
                )}

                {deleteCount > 0 && (
                    <Alert
                        color="yellow"
                        title={`Saving deletes ${deleteCount} ${deleteCount === 1 ? "widget" : "widgets"}`}
                        icon={<MdWarningAmber size={18} />}
                    >
                        <List size="sm" spacing={4}>
                            {pendingDeletes.map((name, index) => (
                                <List.Item key={`${name}-${index}`}>{name}</List.Item>
                            ))}
                        </List>
                    </Alert>
                )}

                <Textarea
                    aria-label="Board JSON"
                    autosize
                    minRows={18}
                    maxRows={26}
                    spellCheck={false}
                    disabled={!isLoaded}
                    value={text}
                    onChange={(event) => {
                        setText(event.currentTarget.value);
                        setPendingDeletes([]);
                    }}
                    styles={{
                        input: {
                            fontFamily: "monospace",
                            fontSize: "12px",
                            whiteSpace: "pre",
                            overflowX: "auto",
                        },
                    }}
                />

                <Group justify="flex-end" mt="sm">
                    <Button variant="default" onClick={onClose}>
                        Cancel
                    </Button>
                    <Button
                        color={deleteCount > 0 ? "red" : color}
                        loading={isSaving}
                        disabled={!isLoaded}
                        onClick={handleSave}
                    >
                        {deleteCount > 0
                            ? `Delete ${deleteCount} and save`
                            : isImport
                              ? "Import"
                              : "Save"}
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
