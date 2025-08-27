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
import { FieldUpsertDto } from "../model/requests/FieldUpsertDto";

interface FieldFormDialogProps {
    tracker: TrackerDto;
    fieldId?: string;
    initialValues?: FieldUpsertDto;
    onClose: () => void;
    onFieldSaved?: () => void;
}

export function FieldFormDialog(props: FieldFormDialogProps) {
    const form = useForm<FieldUpsertDto>({
        initialValues: props.initialValues || {
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

    const handleSubmit = async (values: FieldUpsertDto) => {
        if (props.fieldId) {
            await api.put(
                `/trackers/${props.tracker.id}/fields/${props.fieldId}`,
                values
            );
        } else {
            await api.post(`/trackers/${props.tracker.id}/fields`, values);
        }
        props.onClose();
        form.reset();
        props.onFieldSaved?.();
    };

    return (
        <Modal
            opened
            onClose={props.onClose}
            title="Add Field to Tracker"
            centered
        >
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

                    <Button color={props.tracker.color} type="submit">
                        Create
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
