import {
    Badge,
    Code,
    Divider,
    Group,
    Modal,
    Paper,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { useFields } from "../../fields/context/FieldsContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { TrackerConstantDto } from "../types/TrackerConstantDto";

interface ConstantDetailsDialogProps {
    constant: TrackerConstantDto;
    tracker: TrackerDto;
    onClose: () => void;
}

export default function ConstantDetailsDialog({
    constant,
    tracker,
    onClose,
}: ConstantDetailsDialogProps) {
    const { fields } = useFields();
    const getField = (fieldId: string) => fields.find((f) => f.id === fieldId);

    return (
        <Modal
            opened
            centered
            onClose={onClose}
            title={
                <Group justify="space-between" wrap="nowrap" mr="xs">
                    <Title order={4} className="wrapped-text" lineClamp={3}>
                        {constant.name}
                    </Title>
                    <Badge color={tracker.color} variant="filled" miw="max-content">
                        Constant Details
                    </Badge>
                </Group>
            }
            size="md"
        >
            <Stack gap="sm">
                <Divider label="Definition" />

                <Group justify="space-between" wrap="nowrap">
                    <Text fw={500}>Type</Text>
                    <Badge variant="light" color="blue">{constant.type}</Badge>
                </Group>

                <Group justify="space-between" wrap="nowrap">
                    <Text fw={500}>Base Value</Text>
                    <Code>{constant.value}</Code>
                </Group>

                {constant.values?.length > 0 && (
                    <>
                        <Divider label="Conditional Values" />
                        <Stack gap="sm">
                            {constant.values
                                .slice()
                                .sort((a, b) => a.priority - b.priority)
                                .map((cv) => (
                                    <Paper key={cv.id} p="sm" withBorder>
                                        <Stack gap="xs">
                                            <Group justify="space-between" wrap="nowrap">
                                                <Text size="sm" fw={500}>
                                                    Priority {cv.priority}
                                                </Text>
                                                <Code>{cv.value}</Code>
                                            </Group>
                                            {cv.filters.length > 0 && (
                                                <>
                                                    <Divider label="Filters" labelPosition="center" />
                                                    {cv.filters.map((f) => {
                                                        const field = getField(f.fieldId);
                                                        return (
                                                            <Group key={f.id} justify="space-between" wrap="nowrap">
                                                                <Text fw={500}>{field?.name ?? f.fieldId}</Text>
                                                                <Group gap="xs">
                                                                    <Badge color={tracker.color} variant="light">
                                                                        {formatOperator(f.operator)}
                                                                    </Badge>
                                                                    <Badge color={tracker.color} variant="outline">
                                                                        {f.value
                                                                            ? renderValue(field?.type, f.value)
                                                                            : "Empty"}
                                                                    </Badge>
                                                                </Group>
                                                            </Group>
                                                        );
                                                    })}
                                                </>
                                            )}
                                        </Stack>
                                    </Paper>
                                ))}
                        </Stack>
                    </>
                )}
            </Stack>
        </Modal>
    );
}
