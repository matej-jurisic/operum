import { useMantineColorScheme } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import {
    Spotlight,
    SpotlightActionData,
    SpotlightActionGroupData,
} from "@mantine/spotlight";
import { createElement } from "react";
import { TbCompass, TbLayoutDashboard, TbPlus, TbUser } from "react-icons/tb";
import { useNavigate } from "react-router-dom";
import { observer } from "mobx-react";
import { resolveTrackerIcon } from "../../constants/TrackerIcons";
import navigationStore from "../../stores/NavigationStore";

/**
 * The command palette (mod+K, or the sidebar's Search button). Jumps to any
 * accessible tracker or dashboard and offers a few quick actions. Navigation
 * only -- entry search is a separate, later piece of work.
 */
const AppSpotlight = observer(() => {
    const navigate = useNavigate();
    const { toggleColorScheme } = useMantineColorScheme();
    const isMobile = useMediaQuery("(max-width: 48em)");

    const trackerActions: SpotlightActionData[] = navigationStore.trackers.map(
        (tracker) => ({
            id: `tracker-${tracker.id}`,
            label: tracker.name,
            leftSection: createElement(resolveTrackerIcon(tracker.icon), {
                size: 18,
            }),
            onClick: () => navigate(`/trackers/${tracker.id}`),
        }),
    );

    const dashboardActions: SpotlightActionData[] =
        navigationStore.dashboards.map((dashboard) => ({
            id: `dashboard-${dashboard.id}`,
            label: dashboard.name,
            leftSection: <TbLayoutDashboard size={18} />,
            onClick: () => navigate(`/dashboard/${dashboard.id}`),
        }));

    const quickActions: SpotlightActionData[] = [
        {
            id: "go-explore",
            label: "Explore",
            leftSection: <TbCompass size={18} />,
            onClick: () => navigate("/explore"),
        },
        {
            id: "new-tracker",
            label: "New tracker",
            leftSection: <TbPlus size={18} />,
            onClick: () => navigationStore.startTrackerCreate("wizard"),
        },
        {
            id: "new-dashboard",
            label: "New dashboard",
            leftSection: <TbLayoutDashboard size={18} />,
            onClick: () => navigationStore.startDashboardCreate(),
        },
        {
            id: "go-profile",
            label: "Go to profile",
            leftSection: <TbUser size={18} />,
            onClick: () => navigate("/profile"),
        },
        {
            id: "toggle-theme",
            label: "Toggle light / dark theme",
            onClick: () => toggleColorScheme(),
        },
    ];

    const groups: SpotlightActionGroupData[] = [
        { group: "Trackers", actions: trackerActions },
        { group: "Dashboards", actions: dashboardActions },
        { group: "Actions", actions: quickActions },
    ].filter((g) => g.actions.length > 0);

    return (
        <Spotlight
            actions={groups}
            shortcut="mod + K"
            nothingFound="Nothing found"
            highlightQuery
            fullScreen={isMobile}
            searchProps={{
                placeholder: "Search trackers and dashboards...",
            }}
        />
    );
});

export default AppSpotlight;
