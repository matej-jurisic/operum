import React, { createContext, useCallback, useContext, useState } from "react";
import api from "../api/api";
import { EntryDto } from "../model/EntryDto";
import { FieldAnalyticsDto } from "../model/FieldAnalyticsDto";
import { FieldDto } from "../model/FieldDto";

type TrackerContextType = {
    entries: EntryDto[];
    fields: FieldDto[];
    analytics: FieldAnalyticsDto[];
    refreshEntries: () => Promise<void>;
    refreshFields: () => Promise<void>;
    refreshAnalytics: () => Promise<void>;
    markEntriesDirty: () => void;
    markFieldsDirty: () => void;
    markAnalyticsDirty: () => void;
    refreshFieldsIfDirty: () => Promise<void>;
    refreshEntriesIfDirty: () => Promise<void>;
    refreshAnalyticsIfDirty: () => Promise<void>;
};

const TrackerContext = createContext<TrackerContextType | undefined>(undefined);

const GetEntries = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/entries`);
    return response.data.data;
};

const GetFields = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/fields`);
    return response.data.data;
};

const GetTrackerAnalytics = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/analytics`);
    return response.data.data;
};

export const TrackerProvider: React.FC<{
    trackerId: string;
    children: React.ReactNode;
}> = ({ trackerId, children }) => {
    const [entries, setEntries] = useState<EntryDto[]>([]);
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [analytics, setAnalytics] = useState<FieldAnalyticsDto[]>([]);

    const [entriesDirty, setEntriesDirty] = useState(true);
    const [fieldsDirty, setFieldsDirty] = useState(true);
    const [analyticsDirty, setAnalyticsDirty] = useState(true);

    const refreshEntries = useCallback(async () => {
        const data = await GetEntries(trackerId);
        setEntries(data);
        setEntriesDirty(false);
    }, [trackerId]);

    const refreshFields = useCallback(async () => {
        const data = await GetFields(trackerId);
        setFields(data);
        setFieldsDirty(false);
    }, [trackerId]);

    const refreshAnalytics = useCallback(async () => {
        const data = await GetTrackerAnalytics(trackerId);
        setAnalytics(data);
        setAnalyticsDirty(false);
    }, [trackerId]);

    const markEntriesDirty = useCallback(() => setEntriesDirty(true), []);
    const markFieldsDirty = useCallback(() => setFieldsDirty(true), []);
    const markAnalyticsDirty = useCallback(() => setAnalyticsDirty(true), []);

    const refreshIfDirty = useCallback(
        async (dataset: "entries" | "fields" | "analytics") => {
            if (dataset === "entries" && entriesDirty) await refreshEntries();
            if (dataset === "fields" && fieldsDirty) await refreshFields();
            if (dataset === "analytics" && analyticsDirty)
                await refreshAnalytics();
        },
        [
            entriesDirty,
            fieldsDirty,
            analyticsDirty,
            refreshEntries,
            refreshFields,
            refreshAnalytics,
        ]
    );
    return (
        <TrackerContext.Provider
            value={{
                entries,
                fields,
                analytics,
                refreshEntriesIfDirty: () => refreshIfDirty("entries"),
                refreshFieldsIfDirty: () => refreshIfDirty("fields"),
                refreshAnalyticsIfDirty: () => refreshIfDirty("analytics"),
                markEntriesDirty,
                markFieldsDirty,
                markAnalyticsDirty,
                refreshAnalytics,
                refreshEntries,
                refreshFields,
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
