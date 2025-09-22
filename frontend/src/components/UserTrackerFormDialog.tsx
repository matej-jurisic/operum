import { Button, Modal, Stack, TextInput } from "@mantine/core";
import { useForm } from "@mantine/form";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import ModifyUserTrackerDto from "../model/requests/ModifyUserTrackerDto";
interface Props {
    onClose: () => void;
}

const AddUserToTracker = async (
    trackerId: string,
    request: ModifyUserTrackerDto
) => {
    await api.post(`/trackers/${trackerId}/users`, request);
};

export default function UserTrackerFormDialog(props: Props) {
    const { tracker } = useTracker();

    const form = useForm<ModifyUserTrackerDto>({
        initialValues: {
            username: "",
        },
    });

    const handleSubmit = async (values: ModifyUserTrackerDto) => {
        await AddUserToTracker(tracker.id, values);
        props.onClose();
        form.reset();
    };

    return (
        <Modal opened onClose={props.onClose} title={"Add User"} centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack align="stretch">
                    <TextInput
                        label="Username"
                        placeholder="Enter username"
                        maxLength={20}
                        {...form.getInputProps("username")}
                    />
                    <Button color={tracker.color} type="submit">
                        Add
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
