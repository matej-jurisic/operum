import { Button, Group, Menu, Stack, Title } from "@mantine/core";
import { CiLogout, CiUser } from "react-icons/ci";
import TrackerList from "../components/TrackerList";
import useAuth from "../hooks/useAuth";
import globalStore from "../stores/GlobalStore";

export default function Home() {
    const auth = useAuth();

    return (
        <>
            <Stack gap="lg">
                <Group justify="space-between">
                    <Title c="indigo" order={2}>
                        Operum
                    </Title>
                    <Menu>
                        <Menu.Target>
                            <Button variant="outline">
                                <CiUser size={18} />
                            </Button>
                        </Menu.Target>

                        <Menu.Dropdown>
                            <Menu.Item leftSection={<CiUser size={16} />}>
                                {globalStore.currentUser?.userName || "Guest"}
                            </Menu.Item>
                            <Menu.Item
                                leftSection={<CiLogout size={16} />}
                                onClick={async () => {
                                    await auth.logout();
                                    window.location.reload();
                                }}
                            >
                                Logout
                            </Menu.Item>
                        </Menu.Dropdown>
                    </Menu>
                </Group>
                <TrackerList />
            </Stack>
        </>
    );
}
