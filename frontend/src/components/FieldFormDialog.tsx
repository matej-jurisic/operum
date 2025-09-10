import {
    Button,
    Checkbox,
    Modal,
    Select,
    Stack,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useTracker } from "../context/TrackerContext";
import { TrackerDto } from "../model/TrackerDto";
import { fieldTypes } from "../model/constants/DataTypesForSelect";
import { FieldUpsertDto } from "../model/requests/FieldUpsertDto";

interface FieldFormDialogProps {
    tracker: TrackerDto;
    fieldId?: string;
    initialValues?: FieldUpsertDto;
    onClose: () => void;
}

export function FieldFormDialog(props: FieldFormDialogProps) {
    const { CreateField, UpdateField } = useTracker();

    const form = useForm<FieldUpsertDto>({
        initialValues: props.initialValues || {
            name: "",
            type: "string",
            required: false,
        },

        validate: {
            name: (value) =>
                value.trim().length === 0
                    ? "Field name is required"
                    : value.length > 30
                    ? "Name must be shorter than 30 characters"
                    : null,
            description: (value) =>
                value && value.length > 500
                    ? "Description must be at most 500 characters"
                    : null,
            type: (value) => (value ? null : "Type is required"),
        },
    });

    const handleSubmit = async (values: FieldUpsertDto) => {
        if (props.fieldId) {
            UpdateField(props.tracker.id, props.fieldId, values);
        } else {
            CreateField(props.tracker.id, values);
        }
        props.onClose();
        form.reset();
    };

    return (
        <Modal
            opened
            onClose={props.onClose}
            title={props.fieldId ? "Edit Field" : "Create Field"}
            centered
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack align="stretch">
                    <TextInput
                        label="Field Name"
                        placeholder="Enter field name"
                        maxLength={30}
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
                        maxLength={500}
                        {...form.getInputProps("description")}
                    />

                    <Checkbox
                        label="Required"
                        checked={form.values.required}
                        {...form.getInputProps("required")}
                    />

                    <Button color={props.tracker.color} type="submit">
                        Create
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
