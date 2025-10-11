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
import { UserDto } from "../../auth/types/UserDto";
import { usersController } from "../api/usersController";
import { ChangeUserRoleDto } from "../types/requests/ChangeUserRoleDto";

interface Props {
    user: UserDto;
    onClose: () => void;
    onRoleChange: (newRoles: string[]) => void;
}

export default function UserRolesFormDialog(props: Props) {
    const [allRoles, setAllRoles] = useState<string[]>([]);

    useEffect(() => {
        const fetchRoles = async () => {
            const response = await usersController.getRoles();
            setAllRoles(response.data);
        };
        fetchRoles();
    }, []);

    const form = useForm<ChangeUserRoleDto>({
        initialValues: {
            roleName: props.user.roles.length > 0 ? props.user.roles[0] : "",
        },
    });

    const handleChange = async (value: ChangeUserRoleDto) => {
        await usersController.changeRole(props.user.id, value);
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
