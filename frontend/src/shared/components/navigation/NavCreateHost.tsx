import { observer } from "mobx-react";
import { useNavigate } from "react-router-dom";
import { dashboardController } from "../../../features/dashboard/api/dashboardController";
import BoardFormModal from "../../../features/dashboard/components/BoardFormModal";
import TrackerFormDialog from "../../../features/trackers/components/TrackerFormDialog";
import TrackerWizard from "../../../features/trackers/components/TrackerWizard";
import navigationStore from "../../stores/NavigationStore";

/** Watches NavigationStore's create flags (set by the sidebar "+" buttons and command palette) and renders the matching dialog. */
const NavCreateHost = observer(() => {
    const navigate = useNavigate();
    const mode = navigationStore.trackerCreate;

    return (
        <>
            {mode === "wizard" && (
                <TrackerWizard
                    onClose={() => navigationStore.stopTrackerCreate()}
                    onConfirm={(created) => {
                        navigationStore.stopTrackerCreate();
                        navigationStore.refreshTrackers();
                        navigate(`/trackers/${created.id}`);
                    }}
                />
            )}

            {(mode === "blank" || mode === "template") && (
                <TrackerFormDialog
                    withTemplate={mode === "template"}
                    onClose={() => navigationStore.stopTrackerCreate()}
                    onConfirm={(created) => {
                        navigationStore.stopTrackerCreate();
                        navigationStore.refreshTrackers();
                        if (created) navigate(`/trackers/${created.id}`);
                    }}
                />
            )}

            {navigationStore.dashboardCreateOpen && (
                <BoardFormModal
                    onClose={() => navigationStore.stopDashboardCreate()}
                    onSubmit={async (values) => {
                        try {
                            const res =
                                await dashboardController.createDashboard(values);
                            navigationStore.stopDashboardCreate();
                            await navigationStore.refreshDashboards();
                            navigate(`/dashboard/${res.data.id}`);
                        } catch {
                            // the api layer already surfaced the error
                        }
                    }}
                />
            )}
        </>
    );
});

export default NavCreateHost;
