import { TrackerDto } from "./TrackerDto";

export interface TrackerState {
    tracker: TrackerDto;
    selectedViewIds: string[];
}
