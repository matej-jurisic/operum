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
import { TbFileImport } from "react-icons/tb";
import IconPicker from "../../trackers/components/IconPicker";
import { DashboardDto } from "../types/DashboardDto";
import { BoardDocumentModal } from "./BoardDocumentModal";

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
    /** Creating only: offers building the board from a JSON document instead. */
    onImported?: (dashboardId: string) => Promise<void>;
    onSubmit: (values: {
        name: string;
        color?: string;
        icon?: string;
    }) => Promise<void>;
}

export default function BoardFormModal({ board, onClose, onImported, onSubmit }: Props) {
    const theme = useMantineTheme();
    const [name, setName] = useState(board?.name ?? "");
    const [color, setColor] = useState(board?.color ?? "indigo");
    const [icon, setIcon] = useState<string | undefined>(board?.icon);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isImporting, setIsImporting] = useState(false);

    const handleSubmit = async () => {
        if (!name.trim() || isSubmitting) return;
        setIsSubmitting(true);
        try {
            await onSubmit({ name: name.trim(), color, icon });
        } finally {
            setIsSubmitting(false);
        }
    };

    if (isImporting && onImported) {
        return <BoardDocumentModal color={color} onClose={onClose} onSaved={onImported} />;
    }

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
                <Group justify={!board && onImported ? "space-between" : "flex-end"}>
                    {!board && onImported && (
                        <Button
                            variant="subtle"
                            color="gray"
                            leftSection={<TbFileImport size={16} />}
                            onClick={() => setIsImporting(true)}
                        >
                            Import from JSON
                        </Button>
                    )}
                    <Group>
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
                </Group>
            </Stack>
        </Modal>
    );
}
