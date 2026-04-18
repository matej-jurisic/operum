import React, { createContext, useContext, useState } from "react";
import globalStore from "../../../shared/stores/GlobalStore";
import { TrackerDto } from "../types/TrackerDto";

type TrackerContextType = {
    tracker: TrackerDto;
    selectedViewIds: string[];
    setTracker: React.Dispatch<React.SetStateAction<TrackerDto>>;
    _setSelectedViewIds: (viewIds: string[]) => void;
    isOwner: boolean;
    canEditData: boolean;
    canEditSchema: boolean;
};

const TrackerContext = createContext<TrackerContextType | undefined>(undefined);

export const TrackerProvider: React.FC<{
    initialTracker: TrackerDto;
    children: React.ReactNode;
}> = ({ initialTracker, children }) => {
    const [tracker, setTracker] = useState<TrackerDto>(initialTracker);
    const [selectedViewIds, _setSelectedViewIds] = useState<string[]>(
        initialTracker.defaultViewIds ?? []
    );

    const isOwner = tracker.ownerId === globalStore.currentUser?.id;
    const canEditData = isOwner || tracker.currentUserCanEditData;
    const canEditSchema = isOwner || tracker.currentUserCanEditSchema;

    return (
        <TrackerContext.Provider
            value={{
                tracker,
                selectedViewIds,
                setTracker,
                _setSelectedViewIds,
                isOwner,
                canEditData,
                canEditSchema,
            }}
        >
            {children}
        </TrackerContext.Provider>
    );
};

export const useTracker = () => {
    const ctx = useContext(TrackerContext);
    if (!ctx) throw new Error("useTracker must be used within TrackerProvider");
    return ctx;
};
