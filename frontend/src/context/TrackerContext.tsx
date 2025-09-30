import { AxiosRequestConfig } from "axios";
import React, {
    createContext,
    Dispatch,
    SetStateAction,
    useCallback,
    useContext,
    useEffect,
    useState,
} from "react";
import api from "../api/api";
import { EntryDto } from "../model/EntryDto";
import { FieldDto } from "../model/FieldDto";
import { AddTrackerAnalyticDto } from "../model/requests/AddTrackerAnalyticDto";
import { CreateViewDto } from "../model/requests/CreateViewDto";
import { FieldUpsertDto } from "../model/requests/FieldUpsertDto";
import { TrackerAnalyticsResponseDto } from "../model/TrackerAnalyticsResponseDto";
import { TrackerDto } from "../model/TrackerDto";
import { ViewDto } from "../model/ViewDto";

type TrackerContextType = {
    entries: EntryDto[];
    fields: FieldDto[];
    analytics: TrackerAnalyticsResponseDto | undefined;
    views: ViewDto[];
    tracker: TrackerDto;
    selectedViewId: string | undefined;
    setSelectedViewId: (viewId: string | undefined) => void;
    setTracker: Dispatch<SetStateAction<TrackerDto>>;
    refreshEntriesIfDirty: (viewId?: string) => Promise<void>;
    refreshFieldsIfDirty: () => Promise<void>;
    refreshViewsIfDirty: () => Promise<void>;
    refreshAnalyticsIfDirty: () => Promise<void>;
    CreateField: (trackerId: string, values: FieldUpsertDto) => Promise<void>;
    UpdateFieldOrder: (trackerId: string, fieldIds: string[]) => Promise<void>;
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
    DeleteView: (trackerId: string, viewId: string) => Promise<void>;
    CreateView: (trackerId: string, view: CreateViewDto) => Promise<void>;
    AddAnalyticToTracker: (
        trackerAnalytic: AddTrackerAnalyticDto
    ) => Promise<void>;
    RemoveAnalyticFromTracker: (trackerAnalyticId: string) => Promise<void>;
    ImportEntries: (trackerId: string, file: File | null) => Promise<void>;
    analyticsDirty: boolean;
    entriesDirty: boolean;
    // Column visibility state
    visibleColumns: Record<string, boolean>;
    visibleFields: FieldDto[];
    toggleColumn: (columnId: string) => void;
    setAllColumnsVisible: (visible: boolean) => void;
    // Selection state
    selectedEntryIds: Set<string>;
    isSelectMode: boolean;
    allEntriesSelected: boolean;
    someEntriesSelected: boolean;
    toggleEntrySelection: (entryId: string) => void;
    toggleSelectAll: () => void;
    clearSelection: () => void;
    setIsSelectMode: Dispatch<SetStateAction<boolean>>;
};

const TrackerContext = createContext<TrackerContextType | undefined>(undefined);

const GetEntries = async (trackerId: string, viewId?: string) => {
    const response = await api.get(`/trackers/${trackerId}/entries`, {
        params: viewId ? { viewId } : {},
    });
    return response.data.data;
};

const GetFields = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/fields`);
    return response.data.data;
};

const GetTrackerAnalytics = async (trackerId: string, viewId?: string) => {
    const response = await api.get(`/trackers/${trackerId}/analytics`, {
        params: viewId ? { viewId } : {},
    });
    return response.data.data;
};

const GetViewList = async (trackerId: string) => {
    const response = await api.get(`trackers/${trackerId}/views`);
    return response.data.data;
};

export const TrackerProvider: React.FC<{
    initialTracker: TrackerDto;
    children: React.ReactNode;
}> = ({ initialTracker, children }) => {
    const [entries, setEntries] = useState<EntryDto[]>([]);
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [analytics, setAnalytics] = useState<TrackerAnalyticsResponseDto>();
    const [views, setViews] = useState<ViewDto[]>([]);
    const [tracker, setTracker] = useState<TrackerDto>(initialTracker);
    const [internalSelectedViewId, setInternalSelectedViewId] = useState<
        string | undefined
    >(initialTracker.defaultViewId);

    const [entriesDirty, setEntriesDirty] = useState(true);
    const [fieldsDirty, setFieldsDirty] = useState(true);
    const [analyticsDirty, setAnalyticsDirty] = useState(true);
    const [viewsDirty, setViewsDirty] = useState(true);

    // Column visibility state
    const [visibleColumns, setVisibleColumns] = useState<
        Record<string, boolean>
    >({});

    // Selection state
    const [selectedEntryIds, setSelectedEntryIds] = useState<Set<string>>(
        new Set()
    );
    const [isSelectMode, setIsSelectMode] = useState(false);

    // Initialize column visibility when fields are loaded
    useEffect(() => {
        if (fields.length > 0) {
            const initialVisibility: Record<string, boolean> = {};

            // Set all fields as visible by default
            fields.forEach((field) => {
                if (!visibleColumns.hasOwnProperty(field.id)) {
                    initialVisibility[field.id] = true;
                }
            });

            // Always show these system columns by default if not set
            if (!visibleColumns.hasOwnProperty("createdAt")) {
                initialVisibility["createdAt"] = true;
            }
            if (!visibleColumns.hasOwnProperty("actions")) {
                initialVisibility["actions"] = true;
            }

            // Only update if we have new columns to set
            if (Object.keys(initialVisibility).length > 0) {
                setVisibleColumns((prev) => ({
                    ...prev,
                    ...initialVisibility,
                }));
            }
        }
    }, [fields]);

    // Get visible fields based on visibility state
    const visibleFields = fields.filter((field) => visibleColumns[field.id]);

    // Selection computed properties
    const allEntriesSelected =
        entries.length > 0 && selectedEntryIds.size === entries.length;
    const someEntriesSelected =
        selectedEntryIds.size > 0 && selectedEntryIds.size < entries.length;

    // Toggle column visibility
    const toggleColumn = useCallback((columnId: string) => {
        setVisibleColumns((prev) => ({
            ...prev,
            [columnId]: !prev[columnId],
        }));
    }, []);

    // Set all columns visible/hidden
    const setAllColumnsVisible = useCallback(
        (visible: boolean) => {
            const newVisibility: Record<string, boolean> = {};
            fields.forEach((field) => {
                newVisibility[field.id] = visible;
            });
            newVisibility["createdAt"] = visible;
            newVisibility["actions"] = visible;
            setVisibleColumns(newVisibility);
        },
        [fields]
    );

    // Selection functions
    const toggleEntrySelection = useCallback((entryId: string) => {
        setSelectedEntryIds((prev) => {
            const newSet = new Set(prev);
            if (newSet.has(entryId)) {
                newSet.delete(entryId);
            } else {
                newSet.add(entryId);
            }
            return newSet;
        });
    }, []);

    const toggleSelectAll = useCallback(() => {
        const allEntryIds = new Set(entries.map((entry) => entry.id));
        const allSelected = selectedEntryIds.size === allEntryIds.size;

        if (allSelected) {
            setSelectedEntryIds(new Set()); // Deselect all
        } else {
            setSelectedEntryIds(allEntryIds); // Select all
        }
    }, [entries, selectedEntryIds.size]);

    const clearSelection = useCallback(() => {
        setSelectedEntryIds(new Set());
        setIsSelectMode(false);
    }, []);

    const refreshEntries = useCallback(
        async (implicitViewId?: string) => {
            const data = await GetEntries(
                tracker.id,
                implicitViewId ?? internalSelectedViewId
            );
            setEntries(data);
            setEntriesDirty(false);
        },
        [tracker.id, internalSelectedViewId]
    );

    const refreshFields = useCallback(async () => {
        const data = await GetFields(tracker.id);
        setFields(data);
        setFieldsDirty(false);
    }, [tracker.id]);

    const refreshAnalytics = useCallback(
        async (implicitViewId?: string) => {
            const data = await GetTrackerAnalytics(
                tracker.id,
                implicitViewId ?? internalSelectedViewId
            );
            setAnalytics(data);
            setAnalyticsDirty(false);
        },
        [tracker.id, internalSelectedViewId]
    );

    const refreshViews = useCallback(async () => {
        const data = await GetViewList(tracker.id);
        setViews(data);
        setViewsDirty(false);
    }, [tracker.id]);

    const markEntriesDirty = useCallback(() => setEntriesDirty(true), []);
    const markAnalyticsDirty = useCallback(() => setAnalyticsDirty(true), []);

    const refreshIfDirty = useCallback(
        async (dataset: "entries" | "fields" | "analytics" | "views") => {
            if (dataset === "entries" && entriesDirty) await refreshEntries();
            if (dataset === "fields" && fieldsDirty) await refreshFields();
            if (dataset === "analytics" && analyticsDirty)
                await refreshAnalytics();
            if (dataset === "views" && viewsDirty) await refreshViews();
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

    const UpdateFieldOrder = async (trackerId: string, fieldIds: string[]) => {
        await api.put(`/trackers/${trackerId}/fields/reorder`, {
            fieldIds,
        });
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
        // Remove from selection if selected
        setSelectedEntryIds((prev) => {
            const newSet = new Set(prev);
            newSet.delete(entryId);
            return newSet;
        });
        refreshEntries();
        markAnalyticsDirty();
    };

    const DeleteEntries = async (trackerId: string, entryIds: string[]) => {
        await api.delete(`/trackers/${trackerId}/entries`, {
            data: { entryIds },
        });
        // Clear selection after bulk delete
        clearSelection();
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

    const DeleteView = async (trackerId: string, viewId: string) => {
        await api.delete(`trackers/${trackerId}/views/${viewId}`);
        if (viewId === internalSelectedViewId) {
            markEntriesDirty();
            markAnalyticsDirty();
            setInternalSelectedViewId(undefined);
        }
        refreshViews();
    };

    const CreateView = async (trackerId: string, viewData: CreateViewDto) => {
        await api.post(`/trackers/${trackerId}/views`, viewData);
        refreshViews();
    };

    const AddAnalyticToTracker = async (
        trackerAnalytic: AddTrackerAnalyticDto
    ) => {
        await api.post(`/trackers/${tracker.id}/analytics`, trackerAnalytic);
        refreshAnalytics();
    };

    const RemoveAnalyticFromTracker = async (trackerAnalyticId: string) => {
        await api.delete(
            `/trackers/${tracker.id}/analytics/${trackerAnalyticId}`
        );
        refreshAnalytics();
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

        refreshEntries();
    };

    const setSelectedViewId = (viewId: string | undefined) => {
        setInternalSelectedViewId(viewId);
        markAnalyticsDirty();
        markEntriesDirty();
    };

    return (
        <TrackerContext.Provider
            value={{
                entries,
                fields,
                analytics,
                tracker,
                views,
                selectedViewId: internalSelectedViewId,
                setSelectedViewId,
                setTracker,
                refreshEntriesIfDirty: () => refreshIfDirty("entries"),
                refreshFieldsIfDirty: () => refreshIfDirty("fields"),
                refreshAnalyticsIfDirty: () => refreshIfDirty("analytics"),
                refreshViewsIfDirty: () => refreshIfDirty("views"),
                CreateField,
                UpdateField,
                UpdateFieldOrder,
                DeleteField,
                DeleteEntry,
                DeleteEntries,
                CreateEntry,
                UpdateEntry,
                DeleteView,
                CreateView,
                ImportEntries,
                AddAnalyticToTracker,
                RemoveAnalyticFromTracker,
                analyticsDirty,
                entriesDirty,
                // Column visibility
                visibleColumns,
                visibleFields,
                toggleColumn,
                setAllColumnsVisible,
                // Selection
                selectedEntryIds,
                isSelectMode,
                allEntriesSelected,
                someEntriesSelected,
                toggleEntrySelection,
                toggleSelectAll,
                clearSelection,
                setIsSelectMode,
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
