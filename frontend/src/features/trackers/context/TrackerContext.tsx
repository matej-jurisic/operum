import React, { createContext, useContext, useState } from "react";
import { TrackerDto } from "../types/TrackerDto";

type TrackerContextType = {
    tracker: TrackerDto;
    selectedViewIds: string[];
    setTracker: React.Dispatch<React.SetStateAction<TrackerDto>>;
    _setSelectedViewIds: (viewIds: string[]) => void;
};

const TrackerContext = createContext<TrackerContextType | undefined>(undefined);

export const TrackerProvider: React.FC<{
    initialTracker: TrackerDto;
    children: React.ReactNode;
}> = ({ initialTracker, children }) => {
    const [tracker, setTracker] = useState<TrackerDto>(initialTracker);
    const [selectedViewIds, _setSelectedViewIds] = useState<string[]>(
        initialTracker.defaultViewId ? [initialTracker.defaultViewId] : []
    );

    return (
        <TrackerContext.Provider
            value={{
                tracker,
                selectedViewIds,
                setTracker,
                _setSelectedViewIds,
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
