import { TrackerDto } from "./TrackerDto";

export interface TrackerState {
    tracker: TrackerDto;
    selectedViewId: string | undefined;
}
