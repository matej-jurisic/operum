import { AxiosRequestConfig } from "axios";
import React, { createContext, useCallback, useContext, useState } from "react";
import api from "../api/api";
import { EntryDto } from "../model/EntryDto";
import { FieldAnalyticsDto } from "../model/FieldAnalyticsDto";
import { FieldDto } from "../model/FieldDto";
import { FieldUpsertDto } from "../model/requests/FieldUpsertDto";

type TrackerContextType = {
    entries: EntryDto[];
    fields: FieldDto[];
    analytics: FieldAnalyticsDto[];
    refreshEntriesIfDirty: () => Promise<void>;
    refreshFieldsIfDirty: () => Promise<void>;
    refreshAnalyticsIfDirty: () => Promise<void>;
    CreateField: (trackerId: string, values: FieldUpsertDto) => Promise<void>;
    UpdateField: (
        trackerId: string,
        fieldId: string,
        values: FieldUpsertDto
    ) => Promise<void>;
    DeleteField: (trackerId: string, fieldId: string) => Promise<void>;
    DeleteEntry: (trackerId: string, entryId: string) => Promise<void>;
    DeleteEntries: (trackerId: string, entryIds: string[]) => Promise<void>;
    CreateEntry: (
        trackerId: string,
        fieldValues: Record<string, string>
    ) => Promise<void>;
    UpdateEntry: (
        trackerId: string,
        entryId: string,
        fieldValues: Record<string, string>
    ) => Promise<void>;
    ImportEntries: (trackerId: string, file: File | null) => Promise<void>;
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

const GetViewList = async (trackerId: string) => {
    const response = await api.get(`trackers/${trackerId}/views`);
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

    const CreateField = async (trackerId: string, values: FieldUpsertDto) => {
        await api.post(`/trackers/${trackerId}/fields`, values);
        refreshFields();
        markEntriesDirty();
        markAnalyticsDirty();
    };

    const UpdateField = async (
        trackerId: string,
        fieldId: string,
        values: FieldUpsertDto
    ) => {
        await api.put(`/trackers/${trackerId}/fields/${fieldId}`, values);
        refreshFields();

        markEntriesDirty();
        markAnalyticsDirty();
    };

    const DeleteField = async (trackerId: string, fieldId: string) => {
        await api.delete(`/trackers/${trackerId}/fields/${fieldId}`);
        refreshFields();

        markEntriesDirty();
        markAnalyticsDirty();
    };

    const DeleteEntry = async (trackerId: string, entryId: string) => {
        await api.delete(`/trackers/${trackerId}/entries/${entryId}`);
        refreshEntries();

        markAnalyticsDirty();
    };

    const DeleteEntries = async (trackerId: string, entryIds: string[]) => {
        await api.delete(`/trackers/${trackerId}/entries`, {
            data: { entryIds },
        });
        refreshEntries();

        markAnalyticsDirty();
    };

    const CreateEntry = async (
        trackerId: string,
        fieldValues: Record<string, string>
    ) => {
        await api.post(`/trackers/${trackerId}/entries`, { fieldValues });
        refreshEntries();

        markAnalyticsDirty();
    };

    const UpdateEntry = async (
        trackerId: string,
        entryId: string,
        fieldValues: Record<string, string>
    ) => {
        await api.put(`/trackers/${trackerId}/entries/${entryId}`, {
            fieldValues,
        });
        refreshEntries();

        markAnalyticsDirty();
    };

    const ImportEntries = async (trackerId: string, file: File | null) => {
        if (!file) return;
        const formData = new FormData();
        formData.append("file", file);

        const config: AxiosRequestConfig = {
            headers: {
                "Content-Type": "multipart/form-data",
            },
        };

        await api.post(
            `/trackers/${trackerId}/entries/import-csv`,
            formData,
            config
        );
    };

    return (
        <TrackerContext.Provider
            value={{
                entries,
                fields,
                analytics,
                refreshEntriesIfDirty: () => refreshIfDirty("entries"),
                refreshFieldsIfDirty: () => refreshIfDirty("fields"),
                refreshAnalyticsIfDirty: () => refreshIfDirty("analytics"),
                CreateField,
                UpdateField,
                DeleteField,
                DeleteEntry,
                DeleteEntries,
                CreateEntry,
                UpdateEntry,
                ImportEntries,
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
