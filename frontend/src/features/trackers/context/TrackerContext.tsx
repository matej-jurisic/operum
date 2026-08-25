import React, { createContext, useContext, useState } from "react";
import globalStore from "../../../shared/stores/GlobalStore";
import { TrackerDto } from "../types/TrackerDto";

type TrackerContextType = {
    tracker: TrackerDto;
    selectedViewId: string | null;
    setTracker: React.Dispatch<React.SetStateAction<TrackerDto>>;
    _setSelectedViewId: (viewId: string | null) => void;
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
    const [selectedViewId, _setSelectedViewId] = useState<string | null>(
        initialTracker.defaultViewId ?? null
    );

    const isOwner = tracker.ownerId === globalStore.currentUser?.id;
    const canEditData = isOwner || tracker.currentUserCanEditData;
    const canEditSchema = isOwner || tracker.currentUserCanEditSchema;

    return (
        <TrackerContext.Provider
            value={{
                tracker,
                selectedViewId,
                setTracker,
                _setSelectedViewId,
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
