import React, { createContext, useCallback, useContext, useState } from "react";
import { useTracker } from "../../trackers/context/TrackerContext";
import { analyticsController } from "../api/analyticsController";
import { AnalyticDto } from "../types/AnalyticDto";
import { CreateAnalyticDto } from "../types/requests/CreateAnalyticDto";

type AnalyticsContextType = {
    analytics: AnalyticDto[];
    analyticsDirty: boolean;
    refreshAnalytics: (viewIds?: string[]) => Promise<void>;
    refreshAnalyticsIfDirty: () => Promise<void>;
    markAnalyticsDirty: () => void;
    // API methods - internal use only
    _addAnalytic: (trackerAnalytic: CreateAnalyticDto) => Promise<void>;
    _removeAnalytic: (trackerAnalyticId: string) => Promise<void>;
};

const AnalyticsContext = createContext<AnalyticsContextType | undefined>(
    undefined
);

export const AnalyticsProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const { tracker, selectedViewIds } = useTracker();
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const [analyticsDirty, setAnalyticsDirty] = useState(true);

    const refreshAnalytics = useCallback(
        async (implicitViewIds?: string[]) => {
            const response = await analyticsController.getTrackerAnalytics(
                tracker.id,
                implicitViewIds ?? selectedViewIds
            );
            setAnalytics(response.data);
            setAnalyticsDirty(false);
        },
        [tracker.id, selectedViewIds]
    );

    const refreshAnalyticsIfDirty = useCallback(async () => {
        if (analyticsDirty) await refreshAnalytics();
    }, [analyticsDirty, refreshAnalytics]);

    const markAnalyticsDirty = useCallback(() => setAnalyticsDirty(true), []);

    const _addAnalytic = async (analytic: CreateAnalyticDto) => {
        await analyticsController.addAnalytic(tracker.id, analytic);
        await refreshAnalytics();
    };

    const _removeAnalytic = async (analyticId: string) => {
        await analyticsController.removeAnalytic(tracker.id, analyticId);
        await refreshAnalytics();
    };

    return (
        <AnalyticsContext.Provider
            value={{
                analytics,
                analyticsDirty,
                refreshAnalytics,
                refreshAnalyticsIfDirty,
                markAnalyticsDirty,
                _addAnalytic,
                _removeAnalytic,
            }}
        >
            {children}
        </AnalyticsContext.Provider>
    );
};

export const useAnalytics = () => {
    const ctx = useContext(AnalyticsContext);
    if (!ctx)
        throw new Error("useAnalytics must be used within AnalyticsProvider");
    return ctx;
};
