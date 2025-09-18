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
import StickyContainer from "../components/StickyContainer";
import TrackerList from "../components/TrackerList";
import UsersPanel from "../components/UsersPanel";

export default function AdminPanel() {
    const location = useLocation();
    const navigate = useNavigate();
    const currentTab = location.pathname.split("/").pop() || "users";
    const theme = useMantineTheme();

    return (
        <>
            <Stack gap="lg">
                <Group justify="space-between" w={"100%"}>
                    <Group align="center" flex={1} justify="space-between">
                        <Title c={theme.primaryColor} order={2}>
                            Admin Panel
                        </Title>
                    </Group>
                    <Header />
                </Group>
                <Tabs
                    variant="default"
                    keepMounted={true}
                    value={currentTab}
                    onChange={(value) => navigate(`/admin-panel/${value}`)}
                >
                    <StickyContainer>
                        <Tabs.List>
                            <Tabs.Tab value="users">Users</Tabs.Tab>
                            <Tabs.Tab value="templates">Templates</Tabs.Tab>
                        </Tabs.List>
                    </StickyContainer>
                    <Container px={0} pt="md" fluid>
                        <Tabs.Panel value="users">
                            <UsersPanel />
                        </Tabs.Panel>
                        <Tabs.Panel value="templates">
                            <TrackerList isTemplates />
                        </Tabs.Panel>
                    </Container>
                </Tabs>
            </Stack>
        </>
    );
}
