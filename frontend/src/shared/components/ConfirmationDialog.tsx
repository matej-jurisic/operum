import { Button, Group, Modal, Stack, Text } from "@mantine/core";
import { JSX } from "react";

interface ConfirmationDialogProps {
    isOpen: boolean;
    onClose: () => void;
    onConfirm: () => void;
    title: string;
    message: string | JSX.Element;
    /** Name the action ("Delete", "Remove"); defaults to "Confirm". */
    confirmLabel?: string;
    /** "warning" marks a destructive action and turns the confirm button red. */
    severity?: "info" | "warning";
}

export default function ConfirmationDialog({
    isOpen,
    onClose,
    onConfirm,
    title,
    message,
    confirmLabel = "Confirm",
    severity = "info",
}: ConfirmationDialogProps) {
    return (
        <Modal
            centered
            opened={isOpen}
            onClose={onClose}
            title={title}
            withCloseButton
            padding="lg"
        >
            <Stack gap="lg">
                {typeof message === "string" ? (
                    <Text className="truncated-text">{message}</Text>
                ) : (
                    message
                )}

                <Group justify="center" gap="sm">
                    <Button variant="outline" onClick={onClose} color="gray">
                        Cancel
                    </Button>
                    <Button
                        autoFocus
                        onClick={onConfirm}
                        color={severity === "warning" ? "red" : "blue"}
                    >
                        {confirmLabel}
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
