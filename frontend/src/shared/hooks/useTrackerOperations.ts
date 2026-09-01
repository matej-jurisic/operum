import { useEntries } from "../../features/entries/context/EntriesContext";
import { EntrySelection } from "../../features/entries/types/EntrySelection";
import { useFields } from "../../features/fields/context/FieldsContext";
import { CreateFieldDto } from "../../features/fields/types/CreateFieldDto";
import { UpdateFieldDto } from "../../features/fields/types/UpdateFieldDto";
import { useTracker } from "../../features/trackers/context/TrackerContext";
import { useViews } from "../../features/views/context/ViewsContext";
import { CreateViewDto } from "../../features/views/types/requests/CreateViewDto";
import { UpdateViewDto } from "../../features/views/types/requests/UpdateViewDto";

export const useTrackerOperations = () => {
    const { _createField, _updateField, _updateFieldOrder, _deleteField } =
        useFields();

    const {
        markEntriesDirty,
        _createEntry,
        _updateEntry,
        _deleteEntry,
        _deleteEntries,
        _importEntries,
        _recalculateEntries,
    } = useEntries();

    const { refreshViews, _createView, _updateView, _deleteView, _updateViewOrder } =
        useViews();

    const { _setSelectedViewId } = useTracker();

    // ========================================
    // Field Operations
    // ========================================
    const createField = async (values: CreateFieldDto) => {
        await _createField(values);
        markEntriesDirty();
    };

    const updateField = async (fieldId: string, values: UpdateFieldDto) => {
        await _updateField(fieldId, values);
        markEntriesDirty();
    };

    const updateFieldOrder = async (fieldIds: string[]) => {
        await _updateFieldOrder(fieldIds);
        markEntriesDirty();
    };

    const deleteField = async (fieldId: string) => {
        await _deleteField(fieldId);
        markEntriesDirty();
        // Deleting a field drops the clauses bound to it, so the views built on them read
        // differently afterwards.
        await refreshViews();
    };

    // ========================================
    // Entry Operations
    // ========================================
    const createEntry = async (fieldValues: Record<string, string>) => {
        await _createEntry(fieldValues);
    };

    const updateEntry = async (
        entryId: string,
        fieldValues: Record<string, string>
    ) => {
        await _updateEntry(entryId, fieldValues);
    };

    const deleteEntry = async (entryId: string) => {
        await _deleteEntry(entryId);
    };

    const deleteEntries = async (selection: EntrySelection) => {
        await _deleteEntries(selection);
    };

    const importEntries = async (file: File | null) => {
        await _importEntries(file);
    };

    const recalculateEntries = async (selection: EntrySelection) => {
        await _recalculateEntries(selection);
    };

    // ========================================
    // View Operations
    // ========================================
    const createView = async (view: CreateViewDto) => {
        await _createView(view);
    };

    const updateView = async (viewId: string, view: UpdateViewDto) => {
        await _updateView(viewId, view);
        markEntriesDirty();
    };

    const deleteView = async (viewId: string) => {
        await _deleteView(viewId);
        markEntriesDirty();
    };

    const updateViewOrder = async (viewIds: string[]) => {
        await _updateViewOrder(viewIds);
    };

    // ========================================
    // Tracker Operations
    // ========================================

    const setSelectedView = async (viewId: string | null) => {
        _setSelectedViewId(viewId);
        markEntriesDirty();
    };

    return {
        // Field operations
        createField,
        updateField,
        updateFieldOrder,
        deleteField,

        // Entry operations
        createEntry,
        updateEntry,
        deleteEntry,
        deleteEntries,
        importEntries,
        recalculateEntries,

        // View operations
        createView,
        updateView,
        deleteView,
        updateViewOrder,

        // Tracker operations
        setSelectedView,
    };
};
