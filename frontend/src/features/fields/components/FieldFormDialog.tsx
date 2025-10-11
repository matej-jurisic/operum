import {
    Button,
    Checkbox,
    Modal,
    Select,
    Stack,
    Textarea,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateFieldDto } from "../types/CreateFieldDto";
import { FieldDto } from "../types/FieldDto";
import { UpdateFieldDto } from "../types/UpdateFieldDto";

interface FieldFormDialogProps {
    tracker: TrackerDto;
    fieldId?: string;
    initialValues?: FieldDto;
    onClose: () => void;
}

export function FieldFormDialog(props: FieldFormDialogProps) {
    const { createField, updateField } = useTrackerOperations();

    const form = useForm<CreateFieldDto & UpdateFieldDto>({
        initialValues: props.initialValues || {
            name: "",
            type: "string",
            required: false,
            description: "",
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

    const handleSubmit = async (values: CreateFieldDto & UpdateFieldDto) => {
        if (props.fieldId) {
            updateField(props.fieldId, values);
        } else {
            createField(values);
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

                    <Textarea
                        label="Description"
                        placeholder="Enter field description"
                        autosize
                        maxLength={500}
                        {...form.getInputProps("description")}
                    />

                    <Checkbox
                        label="Required"
                        checked={form.values.required}
                        {...form.getInputProps("required")}
                    />

                    <Button color={props.tracker.color} type="submit">
                        {props.fieldId ? "Update" : "Create"}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
