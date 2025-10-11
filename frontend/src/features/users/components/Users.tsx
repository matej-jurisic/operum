import { Paper, ScrollArea, Skeleton, Stack, Table, Text } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import globalStore from "../../../shared/stores/GlobalStore";
import { UserDto } from "../../auth/types/UserDto";
import { usersController } from "../api/usersController";
import UserRolesFormDialog from "./UserRolesFormDialog";
import { UsersCards } from "./UsersCards";
import UsersTable from "./UsersTable";

enum OpenDialogType {
    EditRoles,
    ConfirmMail,
}

export default function Users() {
    const [users, setUsers] = useState<UserDto[]>([]);
    const [selectedUser, setSelectedUser] = useState<UserDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [loading, setLoading] = useState<boolean>(true);

    // Check if mobile
    const isMobile = useMediaQuery("(max-width: 768px)");

    useEffect(() => {
        const GetData = async () => {
            setLoading(true);
            const users = await usersController.getUserList();
            setUsers(users.data);
            setLoading(false);
        };

        GetData();
    }, []);

    const handleEditRoles = (user: UserDto) => {
        setSelectedUser(user);
        setOpenDialogType(OpenDialogType.EditRoles);
    };

    const handleConfirmMail = (user: UserDto) => {
        setSelectedUser(user);
        setOpenDialogType(OpenDialogType.ConfirmMail);
    };

    return (
        <>
            <Skeleton visible={loading} h={"100%"}>
                <Stack gap="md" h={"100%"}>
                    <ScrollArea flex={1}>
                        {loading && <></>}

                        {!loading && users.length === 0 && (
                            <Paper withBorder p="xl" radius="md">
                                <Stack gap="md" align="center">
                                    <Text size="lg" fw={500} c="dimmed">
                                        No Users Available
                                    </Text>
                                    <Text ta="center" c="dimmed">
                                        Users will appear here when they are
                                        registered.
                                    </Text>
                                </Stack>
                            </Paper>
                        )}

                        {!loading && users.length > 0 && (
                            <>
                                {isMobile ? (
                                    <UsersCards
                                        users={users}
                                        onEditRoles={handleEditRoles}
                                        onConfirmMail={handleConfirmMail}
                                        currentUserId={
                                            globalStore.currentUser?.id
                                        }
                                    />
                                ) : (
                                    <Table.ScrollContainer minWidth={0}>
                                        <UsersTable
                                            users={users}
                                            handleEditRoles={handleEditRoles}
                                            handleConfirmMail={
                                                handleConfirmMail
                                            }
                                        />
                                    </Table.ScrollContainer>
                                )}
                            </>
                        )}
                    </ScrollArea>
                </Stack>
            </Skeleton>

            {/* Dialogs */}
            {openDialogType === OpenDialogType.EditRoles && selectedUser && (
                <UserRolesFormDialog
                    user={selectedUser}
                    onClose={() => {
                        setSelectedUser(undefined);
                        setOpenDialogType(undefined);
                    }}
                    onRoleChange={async () => {
                        const response = await usersController.getUserList();
                        setUsers(response.data);
                    }}
                />
            )}
            {openDialogType === OpenDialogType.ConfirmMail && selectedUser && (
                <ConfirmationDialog
                    isOpen
                    onClose={() => {
                        setSelectedUser(undefined);
                        setOpenDialogType(undefined);
                    }}
                    onConfirm={async () => {
                        await usersController.cnofirmEmail(selectedUser.id);
                        const response = await usersController.getUserList();
                        setUsers(response.data);
                        setSelectedUser(undefined);
                        setOpenDialogType(undefined);
                    }}
                    title="Mail Confirmation"
                    severity="info"
                    message={`Set mail as confirmed for user ${selectedUser.userName}?`}
                />
            )}
        </>
    );
}
