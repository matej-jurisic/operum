import {
    Button,
    Checkbox,
    Modal,
    Select,
    Stack,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import api from "../api/api";
import { TrackerDto } from "../model/TrackerDto";
import { fieldTypes } from "../model/constants/DataTypesForSelect";
import { CreateFieldDto } from "../model/requests/CreateFieldDto";

interface CreateFieldDialogProps {
    tracker: TrackerDto;
    onClose: () => void;
    onFieldAdded?: () => void;
}

export function CreateFieldDialog({
    tracker,
    onClose,
    onFieldAdded,
}: CreateFieldDialogProps) {
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
        await api.post(`/trackers/${tracker.id}/fields`, values);
        onClose();
        form.reset();
        onFieldAdded?.();
    };

    return (
        <Modal opened onClose={onClose} title="Add Field to Tracker" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack align="stretch">
                    <TextInput
                        label="Field Name"
                        placeholder="Enter field name"
                        {...form.getInputProps("name")}
                    />

                    <Select
                        allowDeselect={false}
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

                    <Button color={tracker.color} type="submit">
                        Create
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
