import { Badge, Container, Group, Stack, Tabs, Title } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import {
    CiFilter,
    CiGrid41,
    CiHashtag,
    CiUser,
    CiViewList,
    CiWavePulse1,
} from "react-icons/ci";
import { useNavigate, useParams } from "react-router-dom";
import Header from "../../../shared/components/Header";
import { ComposedTrackerProvider } from "../../../shared/context/ComposedTrackerProvider";
import globalStore from "../../../shared/stores/GlobalStore";
import AnalyiticsList from "../../analytics/components/Analytics";
import Constants from "../../constants/components/Constants";
import Entries from "../../entries/components/Entries";
import Fields from "../../fields/components/Fields";
import SelectView from "../../views/components/SelectView";
import Views from "../../views/components/Views";
import { trackersController } from "../api/trackersController";
import TrackerUserList from "../components/TrackerUserList";
import { TrackerDto } from "../types/TrackerDto";

export default function Tracker() {
    const { trackerId, "*": splat } = useParams();
    const navigate = useNavigate();
    const [tracker, setTracker] = useState<TrackerDto>();

    const urlParts = (splat ?? "").split("/").filter(Boolean);
    const activeTab = urlParts[0] || "entries";
    const action = urlParts[1];

    useEffect(() => {
        const fetchData = async () => {
            if (trackerId) {
                const response = await trackersController.getTracker(trackerId);
                setTracker(response.data);
            }
        };
        fetchData();
    }, [trackerId]);

    const isMobile = useMediaQuery("(max-width: 48em)");

    if (!tracker) return <></>;

    const isOwner = tracker.ownerId === globalStore.currentUser?.id;
    const canEditSchema = isOwner || tracker.currentUserCanEditSchema;

    return (
        <ComposedTrackerProvider initialTracker={tracker}>
            <Stack h="100%" gap={"md"}>
                <Stack justify="space-between" w="100%">
                    <Group justify="space-between">
                        <SelectView />
                        <Header color={tracker.color} />
                    </Group>
                    <Group align="center" flex={1} justify="space-between">
                        <Title order={2} c={tracker.color}>
                            {tracker.name}
                        </Title>
                        {tracker.trackerTypeName && (
                            <Badge variant="light">
                                {tracker.trackerTypeName}
                            </Badge>
                        )}
                        {tracker.ownerId !== globalStore.currentUser?.id && (
                            <Badge variant="light" color={tracker.color}>
                                Owned by: {tracker.ownerName}
                            </Badge>
                        )}
                    </Group>
                </Stack>

                <Stack flex="1" mih={0}>
                    <Tabs
                        variant="default"
                        color={tracker.color}
                        keepMounted={false}
                        value={activeTab}
                        onChange={(value) =>
                            value && navigate(`/trackers/${trackerId}/${value}`)
                        }
                        h="100%"
                        display={"flex"}
                        style={{ flexDirection: "column", gap: "xs" }}
                    >
                        <Tabs.List>
                            <Tabs.Tab
                                value="entries"
                                leftSection={
                                    isMobile ? (
                                        <CiViewList size={18} />
                                    ) : undefined
                                }
                            >
                                {(!isMobile || activeTab === "entries") &&
                                    "Entries"}
                            </Tabs.Tab>
                            <Tabs.Tab
                                value="fields"
                                leftSection={
                                    isMobile ? (
                                        <CiGrid41 size={18} />
                                    ) : undefined
                                }
                            >
                                {(!isMobile || activeTab === "fields") &&
                                    "Fields"}
                            </Tabs.Tab>
                            <Tabs.Tab
                                value="views"
                                leftSection={
                                    isMobile ? (
                                        <CiFilter size={18} />
                                    ) : undefined
                                }
                            >
                                {(!isMobile || activeTab === "views") &&
                                    "Views"}
                            </Tabs.Tab>
                            <Tabs.Tab
                                value="analytics"
                                leftSection={
                                    isMobile ? (
                                        <CiWavePulse1 size={18} />
                                    ) : undefined
                                }
                            >
                                {(!isMobile || activeTab === "analytics") &&
                                    "Analytics"}
                            </Tabs.Tab>
                            {canEditSchema && (
                                <Tabs.Tab
                                    value="constants"
                                    leftSection={
                                        isMobile ? (
                                            <CiHashtag size={18} />
                                        ) : undefined
                                    }
                                >
                                    {(!isMobile || activeTab === "constants") &&
                                        "Constants"}
                                </Tabs.Tab>
                            )}
                            {isOwner && !tracker.trackerTypeId && (
                                <Tabs.Tab
                                    value="users"
                                    leftSection={
                                        isMobile ? (
                                            <CiUser size={18} />
                                        ) : undefined
                                    }
                                >
                                    {(!isMobile || activeTab === "users") &&
                                        "Users"}
                                </Tabs.Tab>
                            )}
                        </Tabs.List>

                        <Container
                            fluid
                            flex={1}
                            w="100%"
                            py="md"
                            px={0}
                            mih={0}
                        >
                            <Tabs.Panel value="entries" h="100%">
                                <Entries autoOpenCreate={action === "create"} />
                            </Tabs.Panel>
                            <Tabs.Panel value="views" h="100%">
                                <Views tracker={tracker} />
                            </Tabs.Panel>
                            <Tabs.Panel value="fields" h="100%">
                                <Fields tracker={tracker} />
                            </Tabs.Panel>
                            <Tabs.Panel value="analytics" h="100%">
                                <AnalyiticsList />
                            </Tabs.Panel>
                            <Tabs.Panel value="constants" h="100%">
                                <Constants tracker={tracker} />
                            </Tabs.Panel>
                            <Tabs.Panel value="users" h="100%">
                                <TrackerUserList />
                            </Tabs.Panel>
                        </Container>
                    </Tabs>
                </Stack>
            </Stack>
        </ComposedTrackerProvider>
    );
}
