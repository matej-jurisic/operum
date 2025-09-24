import { Button, Modal, Select, Stack } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useDebouncedValue } from "@mantine/hooks";
import { useEffect, useState } from "react";
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

const GetUsers = async (search: string) => {
    const users = await api.get("/users?search=" + encodeURIComponent(search));
    return users.data.data as { userName: string }[];
};

export default function UserTrackerFormDialog(props: Props) {
    const { tracker } = useTracker();
    const [options, setOptions] = useState<string[]>([]);
    const [search, setSearch] = useState("");
    const [debounced] = useDebouncedValue(search, 300);

    const form = useForm<ModifyUserTrackerDto>({
        initialValues: {
            username: "",
        },
        validate: {
            username: (value) =>
                !value || value.trim().length === 0
                    ? "Username is required"
                    : null,
        },
    });

    useEffect(() => {
        const fetchUsers = async () => {
            if (debounced.trim().length > 1) {
                try {
                    const users = await GetUsers(debounced);
                    const validUsernames =
                        users
                            ?.filter((u) => u && u.userName)
                            ?.map((u) => u.userName) || [];
                    setOptions(validUsernames);
                } catch (error) {
                    console.error("Error fetching users:", error);
                    setOptions([]);
                }
            } else {
                setOptions([]);
            }
        };

        fetchUsers();
    }, [debounced]);

    const handleSubmit = async (values: ModifyUserTrackerDto) => {
        await AddUserToTracker(tracker.id, values);
        form.reset();
        setSearch("");
        setOptions([]);
        props.onClose();
    };

    const handleClose = () => {
        form.reset();
        setSearch("");
        setOptions([]);
        props.onClose();
    };

    return (
        <Modal opened onClose={handleClose} title="Add User" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack align="stretch">
                    <Select
                        label="Username"
                        placeholder="Search username"
                        searchable
                        searchValue={search}
                        onSearchChange={(e) => setSearch(e)}
                        data={options}
                        value={form.values.username || ""}
                        onChange={(val) => {
                            form.setFieldValue("username", val ?? "");
                        }}
                        error={form.errors.username}
                    />
                    <Button
                        color={tracker?.color || "blue"}
                        type="submit"
                        disabled={!form.values.username.trim()}
                    >
                        Add
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
