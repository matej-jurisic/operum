import {
    ActionIcon,
    Button,
    Group,
    Modal,
    SegmentedControl,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { CreateViewDto } from "../model/requests/CreateViewDto";
import { TrackerDto } from "../model/TrackerDto";

interface Props {
    tracker: TrackerDto;
    onClose: () => void;
    onCreated?: () => void;
}

const MAX_SORTS = 3;

const createView = async (trackerId: string, viewData: CreateViewDto) => {
    await api.post(`/trackers/${trackerId}/views`, viewData);
};

export default function ViewFormDialog({ tracker, onClose, onCreated }: Props) {
    const { fields } = useTracker();

    const form = useForm<CreateViewDto>({
        initialValues: {
            name: "",
            sorts: [],
        },
        validate: {
            name: (value) =>
                !value.trim()
                    ? "View name is required"
                    : value.length > 50
                    ? "View name must be at most 50 characters"
                    : null,
            sorts: (sorts) => {
                if (sorts.find((s) => !s.fieldId)) {
                    return "All sorts must have a field selected";
                }
                if (sorts.length === 0) {
                    return "At least one sort is required";
                }
                if (sorts.length > MAX_SORTS) {
                    return `A maximum of ${MAX_SORTS} sorts are allowed`;
                }
                if (
                    sorts.map((s) => s.fieldId).length !==
                    new Set(sorts.map((s) => s.fieldId)).size
                ) {
                    return "Each sort field must be unique";
                }
                return null;
            },
        },
        validateInputOnBlur: true,
    });

    const fieldOptions = fields.map((field) => ({
        value: field.id,
        label: field.name,
    }));

    const canAddSort = form.values.sorts.length < MAX_SORTS;

    const addSort = () => {
        if (!canAddSort) return;

        form.insertListItem("sorts", {
            fieldId: "",
            descending: false,
        });
    };

    const removeSort = (index: number) => {
        form.removeListItem("sorts", index);
    };

    const handleSubmit = async (values: CreateViewDto) => {
        await createView(tracker.id, values);
        onClose();
        onCreated?.();
        form.reset();
    };

    return (
        <Modal opened centered onClose={onClose} title="Create View">
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="md">
                    <TextInput
                        label="View Name"
                        placeholder="Enter view name"
                        required
                        maxLength={50}
                        {...form.getInputProps("name")}
                    />

                    <Stack gap="lg" mih={200} justify="space-between">
                        <Group justify="space-between" align="center">
                            <Text fw={500} size="sm">
                                Sorting{" "}
                                {form.values.sorts.length > 0 &&
                                    `(${form.values.sorts.length}/${MAX_SORTS})`}
                            </Text>
                            <Button
                                color={tracker.color}
                                variant="light"
                                leftSection={<FiPlus size={14} />}
                                onClick={addSort}
                                size="sm"
                                disabled={!canAddSort}
                            >
                                Add Sort
                            </Button>
                        </Group>

                        {form.values.sorts.length === 0 ? (
                            <Text c="dimmed" ta="center">
                                No sorting rules added yet
                            </Text>
                        ) : (
                            <Stack gap="md" flex={1}>
                                {form.values.sorts.map((sort, index) => (
                                    <Group key={index} gap="xs" wrap="nowrap">
                                        <Select
                                            placeholder="Select field"
                                            data={fieldOptions}
                                            {...form.getInputProps(
                                                `sorts.${index}.fieldId`
                                            )}
                                            flex={1}
                                        />

                                        <SegmentedControl
                                            data={[
                                                { value: "asc", label: "Asc" },
                                                {
                                                    value: "desc",
                                                    label: "Desc",
                                                },
                                            ]}
                                            value={
                                                sort.descending ? "desc" : "asc"
                                            }
                                            onChange={(value) =>
                                                form.setFieldValue(
                                                    `sorts.${index}.descending`,
                                                    value === "desc"
                                                )
                                            }
                                        />

                                        <ActionIcon
                                            color="red"
                                            variant="outline"
                                            onClick={() => removeSort(index)}
                                            aria-label="Remove sort"
                                            size={"lg"}
                                        >
                                            <MdDelete size={18} />
                                        </ActionIcon>
                                    </Group>
                                ))}
                            </Stack>
                        )}
                        <Stack>
                            {form.errors.sorts && (
                                <Text c="red" size="xs">
                                    {form.errors.sorts}
                                </Text>
                            )}
                            <Button color={tracker.color} type="submit">
                                Create View
                            </Button>
                        </Stack>
                    </Stack>
                </Stack>
            </form>
        </Modal>
    );
}
