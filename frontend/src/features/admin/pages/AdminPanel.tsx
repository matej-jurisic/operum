import {
    Container,
    Group,
    Stack,
    Tabs,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { CiAlignBottom, CiBoxList, CiUser } from "react-icons/ci";
import { PiDatabaseThin } from "react-icons/pi";
import { useLocation, useNavigate } from "react-router-dom";
import Header from "../../../shared/components/Header";
import Trackers from "../../trackers/components/Trackers";
import Users from "../../users/components/Users";
import AdminStats from "../components/AdminStats";
import AdminTrackers from "../components/AdminTrackers";

export default function AdminPanel() {
    const location = useLocation();
    const navigate = useNavigate();
    const currentTab = location.pathname.split("/").pop() || "overview";
    const theme = useMantineTheme();
    const isMobile = useMediaQuery("(max-width: 48em)");

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
                        keepMounted={false}
                        value={currentTab}
                        onChange={(value) => navigate(`/admin-panel/${value}`)}
                        h="100%"
                    >
                        <Tabs.List>
                            <Tabs.Tab
                                value="overview"
                                leftSection={
                                    isMobile ? (
                                        <CiAlignBottom size={20} />
                                    ) : undefined
                                }
                            >
                                {(!isMobile || currentTab === "overview") &&
                                    "Overview"}
                            </Tabs.Tab>
                            <Tabs.Tab
                                value="users"
                                leftSection={
                                    isMobile ? <CiUser size={20} /> : undefined
                                }
                            >
                                {(!isMobile || currentTab === "users") &&
                                    "Users"}
                            </Tabs.Tab>
                            <Tabs.Tab
                                value="trackers"
                                leftSection={
                                    isMobile ? (
                                        <PiDatabaseThin size={20} />
                                    ) : undefined
                                }
                            >
                                {(!isMobile || currentTab === "trackers") &&
                                    "Trackers"}
                            </Tabs.Tab>
                            <Tabs.Tab
                                value="templates"
                                leftSection={
                                    isMobile ? (
                                        <CiBoxList size={20} />
                                    ) : undefined
                                }
                            >
                                {(!isMobile || currentTab === "templates") &&
                                    "Templates"}
                            </Tabs.Tab>
                        </Tabs.List>

                        <Container
                            fluid
                            flex={1}
                            w="100%"
                            py="md"
                            px={0}
                            mih={0}
                        >
                            <Tabs.Panel value="overview" h="100%">
                                <AdminStats />
                            </Tabs.Panel>
                            <Tabs.Panel value="users" h="100%">
                                <Users />
                            </Tabs.Panel>
                            <Tabs.Panel value="trackers" h="100%">
                                <AdminTrackers />
                            </Tabs.Panel>
                            <Tabs.Panel value="templates" h="100%">
                                <Trackers isTemplates />
                            </Tabs.Panel>
                        </Container>
                    </Tabs>
                </Stack>
            </Stack>
        </>
    );
}
