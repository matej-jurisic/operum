import {
    Container,
    Group,
    Stack,
    Tabs,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useLocation, useNavigate } from "react-router-dom";
import Header from "../components/Header";
import TrackerList from "../components/TrackerList";
import UsersPanel from "../components/UsersPanel";

export default function AdminPanel() {
    const location = useLocation();
    const navigate = useNavigate();
    const currentTab = location.pathname.split("/").pop() || "users";
    const theme = useMantineTheme();

    return (
        <>
            <Stack h="100%" gap="md">
                <Group justify="space-between" w={"100%"} align="flex-start">
                    <Group align="center" flex={1} justify="space-between">
                        <Title c={theme.primaryColor} order={2}>
                            Admin Panel
                        </Title>
                    </Group>
                    <Header />
                </Group>
                <Stack flex="1" mih={0}>
                    <Tabs
                        variant="default"
                        keepMounted={true}
                        value={currentTab}
                        onChange={(value) => navigate(`/admin-panel/${value}`)}
                        h="100%"
                        display={"flex"}
                        style={{ flexDirection: "column" }}
                    >
                        <Tabs.List>
                            <Tabs.Tab value="users">Users</Tabs.Tab>
                            <Tabs.Tab value="templates">Templates</Tabs.Tab>
                        </Tabs.List>

                        <Container
                            fluid
                            flex={1}
                            w="100%"
                            py="md"
                            px={0}
                            mih={0}
                        >
                            <Tabs.Panel value="users" h="100%">
                                <UsersPanel />
                            </Tabs.Panel>
                            <Tabs.Panel value="templates" h="100%">
                                <TrackerList isTemplates />
                            </Tabs.Panel>
                        </Container>
                    </Tabs>
                </Stack>
            </Stack>
        </>
    );
}
