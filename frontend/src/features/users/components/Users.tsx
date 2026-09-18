import { ScrollArea, Stack } from "@mantine/core";
import { useEffect, useState } from "react";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import EmptyState from "../../../shared/components/EmptyState";
import globalStore from "../../../shared/stores/GlobalStore";
import { UserDto } from "../../auth/types/UserDto";
import { usersController } from "../api/usersController";
import UserCard from "./UserCard";
import UserRolesFormDialog from "./UserRolesFormDialog";

enum OpenDialogType {
    EditRoles,
    ConfirmMail,
}

export default function Users() {
    const [users, setUsers] = useState<UserDto[]>([]);
    const [selectedUser, setSelectedUser] = useState<UserDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [loaded, setLoaded] = useState(false);

    const load = async () => {
        const response = await usersController.getUserList();
        setUsers(response.data);
        setLoaded(true);
    };

    useEffect(() => {
        load();
    }, []);

    const handleEditRoles = (user: UserDto) => {
        setSelectedUser(user);
        setOpenDialogType(OpenDialogType.EditRoles);
    };

    const handleConfirmMail = (user: UserDto) => {
        setSelectedUser(user);
        setOpenDialogType(OpenDialogType.ConfirmMail);
    };

    const closeDialog = () => {
        setSelectedUser(undefined);
        setOpenDialogType(undefined);
    };

    return (
        <>
            {/* The global request loader already covers the first fetch, so this stays
                empty until the users arrive. */}
            <ScrollArea h="100%">
                {!loaded ? null : users.length === 0 ? (
                    <EmptyState
                        title="No users yet"
                        hint="Registered users will appear here."
                    />
                ) : (
                    <Stack gap="md" pb="md">
                        {users.map((user) => (
                            <UserCard
                                key={user.id}
                                user={user}
                                isCurrentUser={
                                    user.id === globalStore.currentUser?.id
                                }
                                onEditRoles={handleEditRoles}
                                onConfirmMail={handleConfirmMail}
                            />
                        ))}
                    </Stack>
                )}
            </ScrollArea>

            {openDialogType === OpenDialogType.EditRoles && selectedUser && (
                <UserRolesFormDialog
                    user={selectedUser}
                    onClose={closeDialog}
                    onRoleChange={load}
                />
            )}
            {openDialogType === OpenDialogType.ConfirmMail && selectedUser && (
                <ConfirmationDialog
                    isOpen
                    onClose={closeDialog}
                    onConfirm={async () => {
                        await usersController.cnofirmEmail(selectedUser.id);
                        await load();
                        closeDialog();
                    }}
                    title="Mail Confirmation"
                    severity="info"
                    message={`Set mail as confirmed for user ${selectedUser.userName}?`}
                />
            )}
        </>
    );
}
