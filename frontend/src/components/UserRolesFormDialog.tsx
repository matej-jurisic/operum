import {
    Avatar,
    Button,
    Group,
    Modal,
    Select,
    Stack,
    Text,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useState } from "react";
import { IoMdPerson } from "react-icons/io";
import api from "../api/api";
import { ApplicationUserDto } from "../model/ApplicationUserDto";
import { ModifyUserRoleReuqestDto } from "../model/requests/ModifyUserRoleRequestDto";

interface Props {
    user: ApplicationUserDto;
    onClose: () => void;
    onRoleChange: (newRoles: string[]) => void;
}

const GetRoles = async () => {
    const roles = await api.get("/users/roles");
    return roles.data.data as string[];
};

const ChangeRole = async (
    userId: string,
    roleRequest: ModifyUserRoleReuqestDto
) => {
    await api.post(`/users/${userId}/role`, roleRequest);
};

export default function UserRolesFormDialog(props: Props) {
    const [allRoles, setAllRoles] = useState<string[]>([]);

    useEffect(() => {
        const fetchRoles = async () => {
            setAllRoles(await GetRoles());
        };
        fetchRoles();
    }, []);

    const form = useForm<ModifyUserRoleReuqestDto>({
        initialValues: {
            roleName: props.user.roles.length > 0 ? props.user.roles[0] : "",
        },
    });

    const handleChange = async (value: ModifyUserRoleReuqestDto) => {
        await ChangeRole(props.user.id, value);
        props.onRoleChange([...props.user.roles, value.roleName]);
        props.onClose();
    };

    return (
        <Modal
            opened
            onClose={props.onClose}
            title={
                <Group>
                    <Avatar size="sm" radius="xl" color="indigo">
                        <IoMdPerson size={16} />
                    </Avatar>
                    <Text>{props.user.userName}</Text>
                </Group>
            }
            centered
        >
            <form onSubmit={form.onSubmit(handleChange)}>
                <Stack>
                    <Select
                        label="Select role"
                        data={allRoles}
                        required
                        {...form.getInputProps("roleName")}
                    />
                    <Button type="submit" disabled={allRoles.length === 0}>
                        Add Role
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
