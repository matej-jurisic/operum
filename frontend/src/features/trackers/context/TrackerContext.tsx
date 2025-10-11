import React, { createContext, useContext, useState } from "react";
import { TrackerDto } from "../types/TrackerDto";

type TrackerContextType = {
    tracker: TrackerDto;
    selectedViewId: string | undefined;
    setTracker: React.Dispatch<React.SetStateAction<TrackerDto>>;
    _setSelectedViewId: (viewId: string | undefined) => void;
};

const TrackerContext = createContext<TrackerContextType | undefined>(undefined);

export const TrackerProvider: React.FC<{
    initialTracker: TrackerDto;
    children: React.ReactNode;
}> = ({ initialTracker, children }) => {
    const [tracker, setTracker] = useState<TrackerDto>(initialTracker);
    const [selectedViewId, _setSelectedViewId] = useState<string | undefined>(
        initialTracker.defaultViewId
    );

    return (
        <TrackerContext.Provider
            value={{
                tracker,
                selectedViewId,
                setTracker,
                _setSelectedViewId,
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
