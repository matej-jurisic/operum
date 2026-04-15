import { useAnalytics } from "../../features/analytics/context/AnalyticsContext";
import { CreateAnalyticDto } from "../../features/analytics/types/requests/CreateAnalyticDto";
import { useEntries } from "../../features/entries/context/EntriesContext";
import { useFields } from "../../features/fields/context/FieldsContext";
import { CreateFieldDto } from "../../features/fields/types/CreateFieldDto";
import { UpdateFieldDto } from "../../features/fields/types/UpdateFieldDto";
import { useTracker } from "../../features/trackers/context/TrackerContext";
import { useViews } from "../../features/views/context/ViewsContext";
import { CreateViewDto } from "../../features/views/types/requests/CreateViewDto";

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
    } = useEntries();

    const { markAnalyticsDirty, _addAnalytic, _removeAnalytic } =
        useAnalytics();

    const { _createView, _deleteView, _updateViewOrder } = useViews();

    const { _setSelectedViewId } = useTracker();

    // ========================================
    // Field Operations
    // ========================================
    const createField = async (values: CreateFieldDto) => {
        await _createField(values);
        markEntriesDirty();
        markAnalyticsDirty();
    };

    const updateField = async (fieldId: string, values: UpdateFieldDto) => {
        await _updateField(fieldId, values);
        markEntriesDirty();
        markAnalyticsDirty();
    };

    const updateFieldOrder = async (fieldIds: string[]) => {
        await _updateFieldOrder(fieldIds);
        markEntriesDirty();
        markAnalyticsDirty();
    };

    const deleteField = async (fieldId: string) => {
        await _deleteField(fieldId);
        markEntriesDirty();
        markAnalyticsDirty();
    };

    // ========================================
    // Entry Operations
    // ========================================
    const createEntry = async (fieldValues: Record<string, string>) => {
        await _createEntry(fieldValues);
        markAnalyticsDirty();
    };

    const updateEntry = async (
        entryId: string,
        fieldValues: Record<string, string>
    ) => {
        await _updateEntry(entryId, fieldValues);
        markAnalyticsDirty();
    };

    const deleteEntry = async (entryId: string) => {
        await _deleteEntry(entryId);
        markAnalyticsDirty();
    };

    const deleteEntries = async (entryIds: string[]) => {
        await _deleteEntries(entryIds);
        markAnalyticsDirty();
    };

    const importEntries = async (file: File | null) => {
        await _importEntries(file);
        markAnalyticsDirty();
    };

    // ========================================
    // Analytic Operations
    // ========================================
    const addAnalytic = async (trackerAnalytic: CreateAnalyticDto) => {
        await _addAnalytic(trackerAnalytic);
    };

    const removeAnalytic = async (trackerAnalyticId: string) => {
        await _removeAnalytic(trackerAnalyticId);
    };

    // ========================================
    // View Operations
    // ========================================
    const createView = async (view: CreateViewDto) => {
        await _createView(view);
    };

    const deleteView = async (viewId: string) => {
        await _deleteView(viewId);
        markEntriesDirty();
        markAnalyticsDirty();
    };

    const updateViewOrder = async (viewIds: string[]) => {
        await _updateViewOrder(viewIds);
    };

    // ========================================
    // Tracker Operations
    // ========================================

    const setSelectedView = async (viewId: string | undefined) => {
        _setSelectedViewId(viewId);
        markEntriesDirty();
        markAnalyticsDirty();
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

        // Analytic operations
        addAnalytic,
        removeAnalytic,

        // View operations
        createView,
        deleteView,
        updateViewOrder,

        // Tracker operations
        setSelectedView,
    };
};
