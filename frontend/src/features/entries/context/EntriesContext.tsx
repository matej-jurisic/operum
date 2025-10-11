import React, { createContext, useCallback, useContext, useState } from "react";
import { EntryDto } from "../../entries/types/EntryDto";
import { useTracker } from "../../trackers/context/TrackerContext";
import { entriesController } from "../api/entriesController";

type EntriesContextType = {
    entries: EntryDto[];
    entriesDirty: boolean;
    selectedEntryIds: Set<string>;
    isSelectMode: boolean;
    allEntriesSelected: boolean;
    someEntriesSelected: boolean;
    refreshEntries: (viewId?: string) => Promise<void>;
    refreshEntriesIfDirty: (viewId?: string) => Promise<void>;
    toggleEntrySelection: (entryId: string) => void;
    toggleSelectAll: () => void;
    clearSelection: () => void;
    setIsSelectMode: React.Dispatch<React.SetStateAction<boolean>>;
    markEntriesDirty: () => void;
    // API methods - internal use only
    _createEntry: (fieldValues: Record<string, string>) => Promise<void>;
    _updateEntry: (
        entryId: string,
        fieldValues: Record<string, string>
    ) => Promise<void>;
    _deleteEntry: (entryId: string) => Promise<void>;
    _deleteEntries: (entryIds: string[]) => Promise<void>;
    _importEntries: (file: File | null) => Promise<void>;
};

const EntriesContext = createContext<EntriesContextType | undefined>(undefined);

export const EntriesProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const { tracker, selectedViewId } = useTracker();
    const [entries, setEntries] = useState<EntryDto[]>([]);
    const [entriesDirty, setEntriesDirty] = useState(true);
    const [selectedEntryIds, setSelectedEntryIds] = useState<Set<string>>(
        new Set()
    );
    const [isSelectMode, setIsSelectMode] = useState(false);

    const allEntriesSelected =
        entries.length > 0 && selectedEntryIds.size === entries.length;
    const someEntriesSelected =
        selectedEntryIds.size > 0 && selectedEntryIds.size < entries.length;

    const refreshEntries = useCallback(
        async (implicitViewId?: string) => {
            const response = await entriesController.getEntries(
                tracker.id,
                implicitViewId ?? selectedViewId
            );
            setEntries(response.data);
            setEntriesDirty(false);
        },
        [tracker.id, selectedViewId]
    );

    const refreshEntriesIfDirty = useCallback(
        async (viewId?: string) => {
            if (entriesDirty) await refreshEntries(viewId);
        },
        [entriesDirty, refreshEntries]
    );

    const markEntriesDirty = useCallback(() => setEntriesDirty(true), []);

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
            setSelectedEntryIds(new Set());
        } else {
            setSelectedEntryIds(allEntryIds);
        }
    }, [entries, selectedEntryIds.size]);

    const clearSelection = useCallback(() => {
        setSelectedEntryIds(new Set());
        setIsSelectMode(false);
    }, []);

    const _createEntry = async (fieldValues: Record<string, string>) => {
        await entriesController.createEntry(tracker.id, fieldValues);
        await refreshEntries();
    };

    const _updateEntry = async (
        entryId: string,
        fieldValues: Record<string, string>
    ) => {
        await entriesController.updateEntry(tracker.id, entryId, fieldValues);
        await refreshEntries();
    };

    const _deleteEntry = async (entryId: string) => {
        await entriesController.deleteEntry(tracker.id, entryId);
        setSelectedEntryIds((prev) => {
            const newSet = new Set(prev);
            newSet.delete(entryId);
            return newSet;
        });
        await refreshEntries();
    };

    const _deleteEntries = async (entryIds: string[]) => {
        await entriesController.deleteEntries(tracker.id, entryIds);
        clearSelection();
        await refreshEntries();
    };

    const _importEntries = async (file: File | null) => {
        if (!file) return;
        const formData = new FormData();
        formData.append("file", file);

        await entriesController.importEntries(tracker.id, formData);

        await refreshEntries();
    };

    return (
        <EntriesContext.Provider
            value={{
                entries,
                entriesDirty,
                selectedEntryIds,
                isSelectMode,
                allEntriesSelected,
                someEntriesSelected,
                refreshEntries,
                refreshEntriesIfDirty,
                toggleEntrySelection,
                toggleSelectAll,
                clearSelection,
                setIsSelectMode,
                markEntriesDirty,
                _createEntry,
                _updateEntry,
                _deleteEntry,
                _deleteEntries,
                _importEntries,
            }}
        >
            {children}
        </EntriesContext.Provider>
    );
};

export const useEntries = () => {
    const ctx = useContext(EntriesContext);
    if (!ctx) throw new Error("useEntries must be used within EntriesProvider");
    return ctx;
};
