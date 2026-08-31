import { makeAutoObservable, runInAction } from "mobx";
import { dashboardController } from "../../features/dashboard/api/dashboardController";
import { DashboardDto } from "../../features/dashboard/types/DashboardDto";
import { trackersController } from "../../features/trackers/api/trackersController";
import { TrackerDto } from "../../features/trackers/types/TrackerDto";
import { TrackerFilters } from "../constants/TrackerFilters";

/**
 * Backs the persistent sidebar and the command palette: the full set of trackers
 * and dashboards the current user can reach. Loaded once when the app shell mounts
 * and refreshed piecemeal after the mutations that create, rename, or delete either.
 */
/** Which tracker-creation dialog the sidebar / command palette has asked for. */
export type TrackerCreateMode = "wizard" | "blank" | "template";

class NavigationStore {
    trackers: TrackerDto[] = [];
    dashboards: DashboardDto[] = [];
    loaded = false;
    loading = false;

    // Creation is triggered from the sidebar "+" buttons and the command palette;
    // the actual dialogs are hosted once, near the shell, and watch these flags.
    trackerCreate: TrackerCreateMode | null = null;
    dashboardCreateOpen = false;

    constructor() {
        makeAutoObservable(this);
    }

    startTrackerCreate(mode: TrackerCreateMode) {
        this.trackerCreate = mode;
    }

    stopTrackerCreate() {
        this.trackerCreate = null;
    }

    startDashboardCreate() {
        this.dashboardCreateOpen = true;
    }

    stopDashboardCreate() {
        this.dashboardCreateOpen = false;
    }

    async load() {
        if (this.loading || this.loaded) return;
        this.loading = true;
        try {
            const [trackers, dashboards] = await Promise.all([
                trackersController.getTrackerList(TrackerFilters.Accessible),
                dashboardController.getDashboards(),
            ]);
            runInAction(() => {
                this.trackers = trackers.data ?? [];
                this.dashboards = dashboards.data ?? [];
                this.loaded = true;
            });
        } finally {
            runInAction(() => {
                this.loading = false;
            });
        }
    }

    setDashboards(dashboards: DashboardDto[]) {
        this.dashboards = dashboards;
    }

    setTrackers(trackers: TrackerDto[]) {
        this.trackers = trackers;
    }

    async refreshTrackers() {
        const response = await trackersController.getTrackerList(
            TrackerFilters.Accessible,
        );
        runInAction(() => {
            this.trackers = response.data ?? [];
        });
    }

    async refreshDashboards() {
        const response = await dashboardController.getDashboards();
        runInAction(() => {
            this.dashboards = response.data ?? [];
        });
    }

    clear() {
        this.trackers = [];
        this.dashboards = [];
        this.loaded = false;
        this.loading = false;
        this.trackerCreate = null;
        this.dashboardCreateOpen = false;
    }
}

const navigationStore = new NavigationStore();
export default navigationStore;
