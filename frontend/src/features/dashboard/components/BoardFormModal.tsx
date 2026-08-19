import {
    Button,
    Group,
    Modal,
    Stack,
    Text,
    TextInput,
    UnstyledButton,
    useMantineTheme,
} from "@mantine/core";
import { useState } from "react";
import { FaCircle } from "react-icons/fa";
import IconPicker from "../../trackers/components/IconPicker";
import { DashboardDto } from "../types/DashboardDto";

const colorOptions = [
    "indigo",
    "blue",
    "cyan",
    "grape",
    "green",
    "lime",
    "orange",
    "pink",
    "red",
    "teal",
    "yellow",
    "violet",
];

interface Props {
    board?: DashboardDto;
    onClose: () => void;
    onSubmit: (values: {
        name: string;
        color?: string;
        icon?: string;
    }) => Promise<void>;
}

export default function BoardFormModal({ board, onClose, onSubmit }: Props) {
    const theme = useMantineTheme();
    const [name, setName] = useState(board?.name ?? "");
    const [color, setColor] = useState(board?.color ?? "indigo");
    const [icon, setIcon] = useState<string | undefined>(board?.icon);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async () => {
        if (!name.trim() || isSubmitting) return;
        setIsSubmitting(true);
        try {
            await onSubmit({ name: name.trim(), color, icon });
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <Modal
            opened
            onClose={onClose}
            title={board ? "Edit board" : "New board"}
            centered
        >
            <Stack gap="md">
                <TextInput
                    label="Board Name"
                    placeholder="Enter board name"
                    value={name}
                    onChange={(e) => setName(e.currentTarget.value)}
                    onKeyDown={(e) => e.key === "Enter" && handleSubmit()}
                    autoFocus
                />
                <Stack gap="xs">
                    <Text size="sm" fw={500}>
                        Board Color
                    </Text>
                    <Group gap="xs">
                        {colorOptions.map((c) => (
                            <UnstyledButton
                                key={c}
                                onClick={() => setColor(c)}
                                style={{
                                    borderRadius: "50%",
                                    padding: 2,
                                    border:
                                        color === c
                                            ? `2px solid ${theme.colors[c]?.[6]}`
                                            : "2px solid transparent",
                                    lineHeight: 0,
                                }}
                            >
                                <FaCircle
                                    size={22}
                                    color={theme.colors[c]?.[6]}
                                />
                            </UnstyledButton>
                        ))}
                    </Group>
                </Stack>
                <IconPicker value={icon} onChange={setIcon} color={color} />
                <Group justify="flex-end">
                    <Button variant="default" onClick={onClose}>
                        Cancel
                    </Button>
                    <Button
                        color={color}
                        disabled={!name.trim()}
                        loading={isSubmitting}
                        onClick={handleSubmit}
                    >
                        {board ? "Save" : "Create"}
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
