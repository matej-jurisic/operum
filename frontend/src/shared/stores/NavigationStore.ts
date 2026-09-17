import { makeAutoObservable, runInAction } from "mobx";
import { dashboardController } from "../../features/dashboard/api/dashboardController";
import { DashboardDto } from "../../features/dashboard/types/DashboardDto";
import { trackersController } from "../../features/trackers/api/trackersController";
import { TrackerDto } from "../../features/trackers/types/TrackerDto";
import { TrackerFilters } from "../constants/TrackerFilters";

/** Which tracker-creation dialog the sidebar / command palette has asked for. */
export type TrackerCreateMode = "wizard" | "blank" | "template";

/** Loaded once at shell mount, refreshed piecemeal after create/rename/delete mutations. */
class NavigationStore {
    trackers: TrackerDto[] = [];
    dashboards: DashboardDto[] = [];
    loaded = false;
    loading = false;

    // Set by the sidebar "+" buttons / command palette; dialogs are hosted once near the shell and watch these flags.
    trackerCreate: TrackerCreateMode | null = null;
    dashboardCreateOpen = false;

    // Lives here (not AppLayout) so the burger toggling it can sit in each page's own header row.
    mobileNavOpen = false;

    constructor() {
        makeAutoObservable(this);
    }

    openMobileNav = () => {
        this.mobileNavOpen = true;
    };

    closeMobileNav = () => {
        this.mobileNavOpen = false;
    };

    toggleMobileNav = () => {
        this.mobileNavOpen = !this.mobileNavOpen;
    };

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
        this.mobileNavOpen = false;
    }
}

const navigationStore = new NavigationStore();
export default navigationStore;
