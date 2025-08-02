import { Button, Modal, Stack, TextInput } from "@mantine/core";
import { useForm } from "@mantine/form";
import api from "../api/api";
import { CreateTrackerDto } from "../model/requests/CreateTrackerDto";

export interface CreateTrackerDialogProps {
    onClose: () => void;
    onCreate?: () => void;
}

export default function CreateTrackerDialog({
    onClose,
    onCreate,
}: CreateTrackerDialogProps) {
    const form = useForm<CreateTrackerDto>({
        initialValues: {
            name: "",
            description: "",
        },
        validate: {
            name: (value) =>
                value.trim().length === 0 ? "Field name is required" : null,
        },
    });
    const handleSubmit = async (values: CreateTrackerDto) => {
        await api.post("/trackers", values);
        form.reset();
        onClose();
        onCreate?.();
    };
    return (
        <Modal opened onClose={onClose} title="Create Tracker" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    <TextInput
                        label="Tracker Name"
                        placeholder="Enter tracker name"
                        {...form.getInputProps("name")}
                    />
                    <TextInput
                        label="Description"
                        placeholder="Enter tracker description"
                        {...form.getInputProps("description")}
                    />
                    <Button type="submit">Create Tracker</Button>
                </Stack>
            </form>
        </Modal>
    );
}
