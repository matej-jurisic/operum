import { useEntries } from "../../features/entries/context/EntriesContext";
import { notifySuccess } from "../utils/notify";
import { EntrySelection } from "../../features/entries/types/EntrySelection";
import { useFields } from "../../features/fields/context/FieldsContext";
import { CreateFieldDto } from "../../features/fields/types/CreateFieldDto";
import { ExtractFieldsDto } from "../../features/fields/types/ExtractFieldsDto";
import { UpdateFieldDto } from "../../features/fields/types/UpdateFieldDto";
import { useTracker } from "../../features/trackers/context/TrackerContext";
import { useViews } from "../../features/views/context/ViewsContext";
import { CreateViewDto } from "../../features/views/types/requests/CreateViewDto";
import { UpdateViewDto } from "../../features/views/types/requests/UpdateViewDto";
import navigationStore from "../stores/NavigationStore";

export const useTrackerOperations = () => {
    const {
        _createField,
        _updateField,
        _updateFieldOrder,
        _deleteField,
        _extractFields,
    } = useFields();

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
        // Deleting a field drops clauses bound to it, so views built on them change too.
        await refreshViews();
    };

    const extractFields = async (values: ExtractFieldsDto) => {
        const result = await _extractFields(values);
        markEntriesDirty();
        // Extracted fields' clauses are gone, and a new tracker now exists in the sidebar.
        await refreshViews();
        await navigationStore.refreshTrackers();
        return result;
    };

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
        if (!file) return;
        await _importEntries(file);
        notifySuccess("Entries imported");
    };

    const recalculateEntries = async (selection: EntrySelection) => {
        await _recalculateEntries(selection);
        notifySuccess("Calculated fields updated");
    };

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

    const setSelectedView = async (viewId: string | null) => {
        _setSelectedViewId(viewId);
        markEntriesDirty();
    };

    return {
        createField,
        updateField,
        updateFieldOrder,
        deleteField,
        extractFields,

        createEntry,
        updateEntry,
        deleteEntry,
        deleteEntries,
        importEntries,
        recalculateEntries,

        createView,
        updateView,
        deleteView,
        updateViewOrder,

        setSelectedView,
    };
};
