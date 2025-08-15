import { Button, Group, Modal, Stack, Text } from "@mantine/core";

interface ConfirmationDialogProps {
    isOpen: boolean;
    onClose: () => void;
    onConfirm: () => void;
    title?: string;
    message: string;
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
                <Text className="truncated-text">{message}</Text>

                <Group justify="center" gap="sm">
                    <Button variant="outline" onClick={onClose} color="gray">
                        Cancel
                    </Button>
                    <Button onClick={onConfirm} color={confirmColor}>
                        Confirm
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
