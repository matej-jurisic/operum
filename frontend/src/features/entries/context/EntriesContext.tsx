import React, {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useState,
} from "react";
import { EntryDto } from "../../entries/types/EntryDto";
import { useTracker } from "../../trackers/context/TrackerContext";
import { entriesController } from "../api/entriesController";
import { EntrySelection } from "../types/EntrySelection";

const PAGE_SIZE = 50;

type EntriesContextType = {
    entries: EntryDto[];
    entriesDirty: boolean;
    selectedEntryIds: Set<string>;
    isSelectMode: boolean;
    selectAllMatching: boolean;
    selectedCount: number;
    allEntriesSelected: boolean;
    someEntriesSelected: boolean;
    page: number;
    pageSize: number;
    totalCount: number;
    refreshEntries: (
        viewId?: string | null,
        pageOverride?: number
    ) => Promise<void>;
    refreshEntriesIfDirty: (viewId?: string | null) => Promise<void>;
    goToPage: (page: number) => Promise<void>;
    isEntrySelected: (entryId: string) => boolean;
    toggleEntrySelection: (entryId: string) => void;
    toggleSelectAll: () => void;
    selectAllMatchingEntries: () => void;
    deselectAll: () => void;
    clearSelection: () => void;
    getSelection: () => EntrySelection;
    setIsSelectMode: React.Dispatch<React.SetStateAction<boolean>>;
    markEntriesDirty: () => void;
    // API methods - internal use only
    _createEntry: (fieldValues: Record<string, string>) => Promise<void>;
    _updateEntry: (
        entryId: string,
        fieldValues: Record<string, string>
    ) => Promise<void>;
    _deleteEntry: (entryId: string) => Promise<void>;
    _deleteEntries: (selection: EntrySelection) => Promise<void>;
    _importEntries: (file: File | null) => Promise<void>;
    _recalculateEntries: (selection: EntrySelection) => Promise<void>;
};

const EntriesContext = createContext<EntriesContextType | undefined>(undefined);

export const EntriesProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const { tracker, selectedViewId } = useTracker();
    const [entries, setEntries] = useState<EntryDto[]>([]);
    const [entriesDirty, setEntriesDirty] = useState(true);
    // In "select all matching" mode these hold the exclusions instead of the picks: the
    // selection is then everything the current views match, minus whatever was ticked off.
    const [selectedEntryIds, setSelectedEntryIds] = useState<Set<string>>(
        new Set()
    );
    const [selectAllMatching, setSelectAllMatching] = useState(false);
    const [isSelectMode, setIsSelectMode] = useState(false);
    const [page, setPage] = useState(1);
    const [totalCount, setTotalCount] = useState(0);

    const isEntrySelected = useCallback(
        (entryId: string) =>
            selectAllMatching
                ? !selectedEntryIds.has(entryId)
                : selectedEntryIds.has(entryId),
        [selectAllMatching, selectedEntryIds]
    );

    const selectedCount = selectAllMatching
        ? Math.max(totalCount - selectedEntryIds.size, 0)
        : selectedEntryIds.size;

    const allEntriesSelected =
        entries.length > 0 && entries.every((e) => isEntrySelected(e.id));
    const someEntriesSelected =
        !allEntriesSelected && entries.some((e) => isEntrySelected(e.id));

    const refreshEntries = useCallback(
        async (implicitViewId?: string | null, pageOverride?: number) => {
            const targetPage = pageOverride ?? page;
            const response = await entriesController.getEntries(
                tracker.id,
                implicitViewId !== undefined ? implicitViewId : selectedViewId,
                targetPage,
                PAGE_SIZE
            );
            setEntries(response.data.items);
            setTotalCount(response.data.totalCount);
            setPage(response.data.page);
            setEntriesDirty(false);
        },
        [tracker.id, selectedViewId, page]
    );

    const refreshEntriesIfDirty = useCallback(
        async (viewId?: string | null) => {
            if (entriesDirty) await refreshEntries(viewId);
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
        const allPageSelected = entries.every((e) => isEntrySelected(e.id));
        setSelectedEntryIds((prev) => {
            const newSet = new Set(prev);
            // Ticking every entry on the page means adding them in normal mode and dropping
            // them from the exclusions in "select all matching" mode.
            const select = selectAllMatching
                ? (id: string) => newSet.delete(id)
                : (id: string) => newSet.add(id);
            const deselect = selectAllMatching
                ? (id: string) => newSet.add(id)
                : (id: string) => newSet.delete(id);

            entries.forEach((e) => (allPageSelected ? deselect : select)(e.id));
            return newSet;
        });
    }, [entries, isEntrySelected, selectAllMatching]);

    const selectAllMatchingEntries = useCallback(() => {
        setSelectAllMatching(true);
        setSelectedEntryIds(new Set());
    }, []);

    // Empties the selection but stays in select mode, so the user can start picking again.
    const deselectAll = useCallback(() => {
        setSelectedEntryIds(new Set());
        setSelectAllMatching(false);
    }, []);

    const clearSelection = useCallback(() => {
        deselectAll();
        setIsSelectMode(false);
    }, [deselectAll]);

    // A selection stated as "everything matching" means something different once the view
    // changes, and explicit picks can drop out of the result set, so start over either way.
    useEffect(() => {
        setSelectedEntryIds(new Set());
        setSelectAllMatching(false);
    }, [selectedViewId]);

    const getSelection = useCallback(
        (): EntrySelection =>
            selectAllMatching
                ? {
                      entryIds: [],
                      selectAllMatching: true,
                      viewId: selectedViewId,
                      excludedEntryIds: Array.from(selectedEntryIds),
                  }
                : {
                      entryIds: Array.from(selectedEntryIds),
                      selectAllMatching: false,
                      viewId: null,
                      excludedEntryIds: [],
                  },
        [selectAllMatching, selectedEntryIds, selectedViewId]
    );

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

    const _deleteEntries = async (selection: EntrySelection) => {
        await entriesController.deleteEntries(tracker.id, selection);
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

    const _recalculateEntries = async (selection: EntrySelection) => {
        await entriesController.recalculateEntries(tracker.id, selection);
        await refreshEntries();
    };

    return (
        <EntriesContext.Provider
            value={{
                entries,
                entriesDirty,
                selectedEntryIds,
                isSelectMode,
                selectAllMatching,
                selectedCount,
                allEntriesSelected,
                someEntriesSelected,
                page,
                pageSize: PAGE_SIZE,
                totalCount,
                refreshEntries,
                refreshEntriesIfDirty,
                goToPage,
                isEntrySelected,
                toggleEntrySelection,
                toggleSelectAll,
                selectAllMatchingEntries,
                deselectAll,
                clearSelection,
                getSelection,
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
