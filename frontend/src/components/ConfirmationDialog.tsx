import { Button, Group, Modal, Stack, Text } from "@mantine/core";
import { JSX } from "react";

interface ConfirmationDialogProps {
    isOpen: boolean;
    onClose: () => void;
    onConfirm: () => void;
    title?: string;
    message: string | JSX.Element;
    severity?: "info" | "warning" | "important";
}

export default function ConfirmationDialog({
    isOpen,
    onClose,
    onConfirm,
    title,
    message,
    severity = "info",
}: ConfirmationDialogProps) {
    const confirmColor = {
        info: "blue",
        warning: "red",
        important: "red",
    }[severity];

    return (
        <Modal
            centered
            opened={isOpen}
            onClose={onClose}
            title={
                title ?? severity.charAt(0).toUpperCase() + severity.slice(1)
            }
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
                    <Button autoFocus onClick={onConfirm} color={confirmColor}>
                        Confirm
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
