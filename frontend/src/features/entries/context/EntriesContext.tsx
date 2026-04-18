import React, { createContext, useCallback, useContext, useState } from "react";
import { EntryDto } from "../../entries/types/EntryDto";
import { useTracker } from "../../trackers/context/TrackerContext";
import { entriesController } from "../api/entriesController";

const PAGE_SIZE = 50;

type EntriesContextType = {
    entries: EntryDto[];
    entriesDirty: boolean;
    selectedEntryIds: Set<string>;
    isSelectMode: boolean;
    allEntriesSelected: boolean;
    someEntriesSelected: boolean;
    page: number;
    pageSize: number;
    totalCount: number;
    refreshEntries: (viewIds?: string[], pageOverride?: number) => Promise<void>;
    refreshEntriesIfDirty: (viewIds?: string[]) => Promise<void>;
    goToPage: (page: number) => Promise<void>;
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
    _recalculateEntries: (entryIds: string[]) => Promise<void>;
};

const EntriesContext = createContext<EntriesContextType | undefined>(undefined);

export const EntriesProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const { tracker, selectedViewIds } = useTracker();
    const [entries, setEntries] = useState<EntryDto[]>([]);
    const [entriesDirty, setEntriesDirty] = useState(true);
    const [selectedEntryIds, setSelectedEntryIds] = useState<Set<string>>(
        new Set()
    );
    const [isSelectMode, setIsSelectMode] = useState(false);
    const [page, setPage] = useState(1);
    const [totalCount, setTotalCount] = useState(0);

    const allEntriesSelected =
        entries.length > 0 && entries.every((e) => selectedEntryIds.has(e.id));
    const someEntriesSelected =
        !allEntriesSelected && entries.some((e) => selectedEntryIds.has(e.id));

    const refreshEntries = useCallback(
        async (implicitViewIds?: string[], pageOverride?: number) => {
            const targetPage = pageOverride ?? page;
            const response = await entriesController.getEntries(
                tracker.id,
                implicitViewIds ?? selectedViewIds,
                targetPage,
                PAGE_SIZE
            );
            setEntries(response.data.items);
            setTotalCount(response.data.totalCount);
            setPage(response.data.page);
            setEntriesDirty(false);
        },
        [tracker.id, selectedViewIds, page]
    );

    const refreshEntriesIfDirty = useCallback(
        async (viewIds?: string[]) => {
            if (entriesDirty) await refreshEntries(viewIds);
        },
        [entriesDirty, refreshEntries]
    );

    const goToPage = useCallback(
        async (newPage: number) => {
            await refreshEntries(undefined, newPage);
        },
        [refreshEntries]
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
        const allPageSelected = entries.every((e) => selectedEntryIds.has(e.id));
        setSelectedEntryIds((prev) => {
            const newSet = new Set(prev);
            if (allPageSelected) {
                entries.forEach((e) => newSet.delete(e.id));
            } else {
                entries.forEach((e) => newSet.add(e.id));
            }
            return newSet;
        });
    }, [entries, selectedEntryIds]);

    const clearSelection = useCallback(() => {
        setSelectedEntryIds(new Set());
        setIsSelectMode(false);
    }, []);

    const _createEntry = async (fieldValues: Record<string, string>) => {
        await entriesController.createEntry(tracker.id, fieldValues);
        await refreshEntries(undefined, 1);
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
        const targetPage = entries.length === 1 && page > 1 ? page - 1 : page;
        await refreshEntries(undefined, targetPage);
    };

    const _deleteEntries = async (entryIds: string[]) => {
        await entriesController.deleteEntries(tracker.id, entryIds);
        clearSelection();
        await refreshEntries(undefined, 1);
    };

    const _importEntries = async (file: File | null) => {
        if (!file) return;
        const formData = new FormData();
        formData.append("file", file);
        await entriesController.importEntries(tracker.id, formData);
        await refreshEntries(undefined, 1);
    };

    const _recalculateEntries = async (entryIds: string[]) => {
        await entriesController.recalculateEntries(tracker.id, entryIds);
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
                page,
                pageSize: PAGE_SIZE,
                totalCount,
                refreshEntries,
                refreshEntriesIfDirty,
                goToPage,
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
                _recalculateEntries,
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
