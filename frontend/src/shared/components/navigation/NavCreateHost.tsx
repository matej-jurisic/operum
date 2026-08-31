import { observer } from "mobx-react";
import { useNavigate } from "react-router-dom";
import { dashboardController } from "../../../features/dashboard/api/dashboardController";
import BoardFormModal from "../../../features/dashboard/components/BoardFormModal";
import TrackerFormDialog from "../../../features/trackers/components/TrackerFormDialog";
import TrackerWizard from "../../../features/trackers/components/TrackerWizard";
import navigationStore from "../../stores/NavigationStore";

/**
 * The tracker / dashboard creation dialogs, hosted once for the whole signed-in
 * shell. The sidebar "+" buttons and the command palette only flip a flag on
 * NavigationStore; this watches those flags and renders the right dialog.
 * Rendered inside AppLayout so it always has a router context.
 */
const NavCreateHost = observer(() => {
    const navigate = useNavigate();
    const mode = navigationStore.trackerCreate;

    return (
        <>
            {mode === "wizard" && (
                <TrackerWizard onClose={() => navigationStore.stopTrackerCreate()} />
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
