import { Badge, Container, Group, Stack, Tabs, ThemeIcon, Title } from "@mantine/core";
import { useDocumentTitle } from "../../../shared/hooks/useDocumentTitle";
import { useMediaQuery } from "@mantine/hooks";
import { createElement, useCallback, useEffect, useState } from "react";
import {
    CiBellOn,
    CiBoxList,
    CiGrid41,
    CiHashtag,
    CiUser,
    CiViewList,
} from "react-icons/ci";
import { useNavigate, useParams } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import NotFound from "../../../shared/components/NotFound";
import { readDefaultPage } from "../../../shared/constants/defaultPage";
import { ComposedTrackerProvider } from "../../../shared/context/ComposedTrackerProvider";
import globalStore from "../../../shared/stores/GlobalStore";
import navigationStore from "../../../shared/stores/NavigationStore";
import Constants from "../../constants/components/Constants";
import Entries from "../../entries/components/Entries";
import Fields from "../../fields/components/Fields";
import Notifications from "../../notifications/components/Notifications";
import { areNotificationsEnabled } from "../../notifications/config/notificationsFeature";
import SidebarBurger from "../../../shared/components/navigation/SidebarBurger";
import SelectView from "../../views/components/SelectView";
import Views from "../../views/components/Views";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { trackersController } from "../api/trackersController";
import TrackerActions from "../components/TrackerActions";
import TrackerFormDialog from "../components/TrackerFormDialog";
import TrackerUserList from "../components/TrackerUserList";
import { TrackerDto } from "../types/TrackerDto";

export default function Tracker() {
    const { trackerId, "*": splat } = useParams();
    const navigate = useNavigate();
    const [tracker, setTracker] = useState<TrackerDto>();
    const [editOpen, setEditOpen] = useState(false);
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [missingTrackerId, setMissingTrackerId] = useState<string>();

    const urlParts = (splat ?? "").split("/").filter(Boolean);
    const rawTab = urlParts[0] || "entries";
    // Queries no longer have their own tab; redirect to the Views tab's Queries sub-tab.
    const requestedTab = rawTab === "queries" ? "views" : rawTab;
    // Analytics moved to dashboard widgets; notifications may be disabled by feature flag.
    const activeTab =
        (requestedTab === "notifications" && !areNotificationsEnabled) ||
        requestedTab === "analytics"
            ? "entries"
            : requestedTab;
    const action = urlParts[1];

    const fetchTracker = useCallback(async () => {
        if (trackerId) {
            try {
                const response = await trackersController.getTracker(trackerId);
                setTracker(response.data);
            } catch (error) {
                // A response body means the tracker is gone; no response means offline/timeout.
                if (error) setMissingTrackerId(trackerId);
            }
        }
    }, [trackerId]);

    useEffect(() => {
        fetchTracker();
    }, [fetchTracker]);

    const handleDelete = async () => {
        if (!trackerId) return;
        try {
            await trackersController.deleteTracker(trackerId);
        } finally {
            setDeleteOpen(false);
        }
        await navigationStore.refreshTrackers();
        navigate(readDefaultPage(), { replace: true });
    };

    const isMobile = useMediaQuery("(max-width: 48em)");
    useDocumentTitle(tracker?.name);

    // Providers below only read initialTracker once, so hold render until it matches the URL.
    if (trackerId && missingTrackerId === trackerId) {
        return <NotFound path={`/trackers/${trackerId}`} />;
    }
    if (!tracker || tracker.id !== trackerId) return <></>;

    const isOwner = tracker.ownerId === globalStore.currentUser?.id;
    const canEditSchema = isOwner || tracker.currentUserCanEditSchema;

    return (
        <ComposedTrackerProvider key={tracker.id} initialTracker={tracker}>
            <Stack h="100%" gap={"md"}>
                <Group align="center" w="100%" justify="space-between" wrap="nowrap">
                    <Group gap="sm" align="center" wrap="nowrap" style={{ minWidth: 0 }}>
                        <SidebarBurger />
                        {tracker.icon && (
                            <ThemeIcon size={32} radius="md" variant="light" color={tracker.color} style={{ flexShrink: 0 }}>
                                {createElement(resolveTrackerIcon(tracker.icon), { size: 18 })}
                            </ThemeIcon>
                        )}
                        <Title order={3} c={tracker.color}>
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
                    <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
                        <SelectView />
                        {isOwner && (
                            <TrackerActions
                                color={tracker.color}
                                isMobile={!!isMobile}
                                onEdit={() => setEditOpen(true)}
                                onDelete={() => setDeleteOpen(true)}
                            />
                        )}
                    </Group>
                </Group>

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
                                px={isMobile ? "xs" : undefined}
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
                                px={isMobile ? "xs" : undefined}
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
                                px={isMobile ? "xs" : undefined}
                                leftSection={
                                    isMobile ? (
                                        <CiBoxList size={18} />
                                    ) : undefined
                                }
                            >
                                {(!isMobile || activeTab === "views") &&
                                    "Views"}
                            </Tabs.Tab>
                            {areNotificationsEnabled && (
                                <Tabs.Tab
                                    value="notifications"
                                    px={isMobile ? "xs" : undefined}
                                    leftSection={
                                        isMobile ? (
                                            <CiBellOn size={18} />
                                        ) : undefined
                                    }
                                >
                                    {(!isMobile ||
                                        activeTab === "notifications") &&
                                        "Notifications"}
                                </Tabs.Tab>
                            )}
                            {canEditSchema && (
                                <Tabs.Tab
                                    value="constants"
                                    px={isMobile ? "xs" : undefined}
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
                                    px={isMobile ? "xs" : undefined}
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
                            {areNotificationsEnabled && (
                                <Tabs.Panel value="notifications" h="100%">
                                    <Notifications />
                                </Tabs.Panel>
                            )}
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

            {editOpen && (
                <TrackerFormDialog
                    trackerId={tracker.id}
                    initialValues={tracker}
                    onClose={() => setEditOpen(false)}
                    onConfirm={() => {
                        fetchTracker();
                        navigationStore.refreshTrackers();
                    }}
                />
            )}

            <ConfirmationDialog
                isOpen={deleteOpen}
                onClose={() => setDeleteOpen(false)}
                onConfirm={handleDelete}
                message="Are you sure you want to delete the tracker?"
                severity="warning"
                title="Delete tracker"
                confirmLabel="Delete"
            />
        </ComposedTrackerProvider>
    );
}
