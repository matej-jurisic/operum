import {
    Alert,
    Button,
    Group,
    List,
    Modal,
    Stack,
    Text,
    Textarea,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { MdErrorOutline } from "react-icons/md";
import { ApiResponse } from "../../../shared/types/ApiResponse";
import { dashboardController } from "../api/dashboardController";

interface Props {
    dashboardId: string;
    color: string;
    onClose: () => void;
    onSaved: () => Promise<void>;
}

const messagesFrom = (error: unknown): string[] => {
    const messages = (error as ApiResponse | undefined)?.messages;
    return messages?.length ? messages : ["Something went wrong."];
};

export function BoardDocumentModal({ dashboardId, color, onClose, onSaved }: Props) {
    const [text, setText] = useState("");
    const [isLoaded, setIsLoaded] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [errors, setErrors] = useState<string[]>([]);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            try {
                const res = await dashboardController.getDashboardDocument(dashboardId);
                if (cancelled) return;
                setText(JSON.stringify(res.data, null, 2));
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

        setErrors([]);
        setIsSaving(true);
        try {
            await dashboardController.saveDashboardDocument(dashboardId, parsed);
            await onSaved();
            onClose();
        } catch (error) {
            setErrors(messagesFrom(error));
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <Modal opened onClose={onClose} title="Edit board JSON" size="xl" centered>
            <Stack gap="md">
                <Text size="sm" c="dimmed">
                    Layout, colors and text are editable. A field you leave out stays as it
                    is, and one set to null is cleared. Each widget's sources and filter links
                    are read-only, and widgets are added or removed from the board itself.
                </Text>

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

                <Textarea
                    aria-label="Board JSON"
                    autosize
                    minRows={18}
                    maxRows={26}
                    spellCheck={false}
                    disabled={!isLoaded}
                    value={text}
                    onChange={(event) => setText(event.currentTarget.value)}
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
                        color={color}
                        loading={isSaving}
                        disabled={!isLoaded}
                        onClick={handleSave}
                    >
                        Save
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
