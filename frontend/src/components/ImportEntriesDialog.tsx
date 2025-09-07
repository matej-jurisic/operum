import {
    ActionIcon,
    Box,
    Button,
    Code,
    FileInput,
    Group,
    Modal,
    Stack,
    Text,
    Tooltip,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useClipboard } from "@mantine/hooks";
import { FiCopy } from "react-icons/fi";
import { useTracker } from "../context/TrackerContext";
import { TrackerDto } from "../model/TrackerDto";

interface ImportEntriesDialogProps {
    onClose: () => void;
    tracker: TrackerDto;
}

export default function ImportEntriesDialog(props: ImportEntriesDialogProps) {
    const form = useForm<{ file: File | null }>({
        initialValues: {
            file: null,
        },
    });

    const { fields, ImportEntries } = useTracker();
    const clipboard = useClipboard();

    const generateCsvHeader = () => {
        if (!fields || fields.length === 0) {
            return "No fields available";
        }

        return fields.map((field) => field.name).join(",");
    };

    return (
        <Modal centered opened onClose={props.onClose} title="Import Entries">
            <form
                onSubmit={form.onSubmit(async (values) => {
                    await ImportEntries(props.tracker.id, values.file);
                    props.onClose();
                })}
            >
                <Stack>
                    <Text size="sm" c="dimmed">
                        Upload a CSV file to import entries. Make sure header
                        names match the field names in the tracker.
                    </Text>
                    <Box>
                        <Group justify="space-between" align="center" mb={4}>
                            <Text size="sm" fw={500}>
                                Expected CSV header format:
                            </Text>
                            <Tooltip
                                label={
                                    clipboard.copied ? "Copied!" : "Copy header"
                                }
                                position="left"
                            >
                                <ActionIcon
                                    variant="subtle"
                                    color={clipboard.copied ? "teal" : "gray"}
                                    size="sm"
                                    onClick={() =>
                                        clipboard.copy(generateCsvHeader())
                                    }
                                >
                                    <FiCopy size={14} />
                                </ActionIcon>
                            </Tooltip>
                        </Group>
                        <Code block c="blue" fz="xs">
                            {generateCsvHeader()}
                        </Code>
                    </Box>
                    <FileInput
                        variant="default"
                        accept=".csv"
                        placeholder="Upload file"
                        {...form.getInputProps("file")}
                    />
                    <Button color={props.tracker.color} type="submit">
                        Import
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
