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
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { useFields } from "../../fields/context/FieldsContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";

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

    const { fields } = useFields();
    const { importEntries } = useTrackerOperations();

    const clipboard = useClipboard();

    const generateCsvHeader = () => {
        if (!fields || fields.length === 0) {
            return "No fields available";
        }

        return fields.map((field) => field.name).join(",");
    };

    return (
        <Modal
            centered
            opened
            onClose={props.onClose}
            title="Import Entries"
            size="lg"
        >
            <form
                onSubmit={form.onSubmit(async (values) => {
                    await importEntries(values.file);
                    props.onClose();
                })}
            >
                <Stack>
                    <Box>
                        <Group justify="space-between" align="center" mb={4}>
                            <Text size="sm" fw={500}>
                                Expected header (order does not matter)
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

                    <Text size="xs" c="dimmed">
                        Dates are DD/MM/YYYY, times HH:MM:SS. A datetime is read
                        as UTC unless it carries an offset (10/01/2004 10:32:22
                        +02:00).
                    </Text>

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
