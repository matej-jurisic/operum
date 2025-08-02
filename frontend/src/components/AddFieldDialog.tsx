import {
    Button,
    Checkbox,
    Group,
    Modal,
    Select,
    Stack,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import api from "../api/api";

interface CreateFieldDto {
    name: string;
    description?: string;
    type: string;
    required: boolean;
}

interface AddFieldDialogProps {
    trackerId: string;
    onClose: () => void;
    onFieldAdded?: () => void;
}

const fieldTypes = [
    { value: "string", label: "String" },
    { value: "number", label: "Number" },
    { value: "bool", label: "Bool" },
    { value: "date", label: "Date" },
    { value: "timespan", label: "Timespan" },
    { value: "datetime", label: "Datetime" },
];

export function AddFieldDialog({
    trackerId,
    onClose,
    onFieldAdded,
}: AddFieldDialogProps) {
    const form = useForm<CreateFieldDto>({
        initialValues: {
            name: "",
            type: "string",
            description: "",
            required: false,
        },

        validate: {
            name: (value) =>
                value.trim().length === 0 ? "Field name is required" : null,
            type: (value) => (value ? null : "Type is required"),
        },
    });

    const handleSubmit = async (values: CreateFieldDto) => {
        await api.post(`/trackers/${trackerId}/fields`, values);
        onClose();
        form.reset();
        onFieldAdded?.();
    };

    return (
        <Modal opened onClose={onClose} title="Add Field to Tracker" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    <TextInput
                        label="Field Name"
                        placeholder="Enter field name"
                        {...form.getInputProps("name")}
                    />

                    <Select
                        label="Type"
                        placeholder="Choose field type"
                        data={fieldTypes}
                        required
                        {...form.getInputProps("type")}
                    />

                    <TextInput
                        label="Description"
                        placeholder="Enter field description"
                        {...form.getInputProps("description")}
                    />

                    <Checkbox
                        label="Required"
                        {...form.getInputProps("required")}
                    />

                    <Group mt="md">
                        <Button type="submit">Add Field</Button>
                    </Group>
                </Stack>
            </form>
        </Modal>
    );
}
