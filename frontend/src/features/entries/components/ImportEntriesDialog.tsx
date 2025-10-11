import {
    ActionIcon,
    Box,
    Button,
    Code,
    FileInput,
    Group,
    List,
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
                    <Text size="sm" c="dimmed">
                        Upload a CSV file to import entries. Make sure header
                        names match the field names in the tracker.
                    </Text>

                    <Box>
                        <Group justify="space-between" align="center" mb={4}>
                            <Text size="sm" fw={500}>
                                Expected CSV header format (Order does not
                                matter):
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

                    <Box>
                        <Text size="sm" fw={500} mb={8}>
                            Date & Time Format Requirements:
                        </Text>
                        <List size="sm" spacing={4}>
                            <List.Item>
                                <Text size="sm">
                                    <Text span fw={500}>
                                        Date:
                                    </Text>{" "}
                                    Use DD/MM/YYYY format (e.g., 10/01/2004)
                                </Text>
                            </List.Item>
                            <List.Item>
                                <Text size="sm">
                                    <Text span fw={500}>
                                        Time:
                                    </Text>{" "}
                                    Use HH:MM:SS format (e.g., 10:32:22)
                                </Text>
                            </List.Item>
                            <List.Item>
                                <Text size="sm">
                                    <Text span fw={500}>
                                        DateTime:
                                    </Text>{" "}
                                    Assumes UTC by default (e.g., 10/01/2004
                                    10:32:22)
                                </Text>
                            </List.Item>
                            <List.Item>
                                <Text size="sm">
                                    <Text span fw={500}>
                                        DateTime with timezone:
                                    </Text>{" "}
                                    Add offset like +02:00 or +02 (e.g.,
                                    10/01/2004 10:32:22 +02:00)
                                </Text>
                            </List.Item>
                        </List>
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
