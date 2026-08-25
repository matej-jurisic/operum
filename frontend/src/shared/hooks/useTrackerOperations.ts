import { useAnalytics } from "../../features/analytics/context/AnalyticsContext";
import { CreateAnalyticDto } from "../../features/analytics/types/requests/CreateAnalyticDto";
import { UpdateAnalyticDto } from "../../features/analytics/types/requests/UpdateAnalyticDto";
import { useEntries } from "../../features/entries/context/EntriesContext";
import { EntrySelection } from "../../features/entries/types/EntrySelection";
import { useFields } from "../../features/fields/context/FieldsContext";
import { CreateFieldDto } from "../../features/fields/types/CreateFieldDto";
import { UpdateFieldDto } from "../../features/fields/types/UpdateFieldDto";
import { useQueries } from "../../features/queries/context/QueriesContext";
import { CreateQueryDto } from "../../features/queries/types/requests/CreateQueryDto";
import { UpdateQueryDto } from "../../features/queries/types/requests/UpdateQueryDto";
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

    const { markAnalyticsDirty, _addAnalytic, _updateAnalytic, _removeAnalytic } =
        useAnalytics();

    const { refreshViews, _createView, _updateView, _deleteView, _updateViewOrder } =
        useViews();

    const { markQueriesDirty, _createQuery, _updateQuery, _deleteQuery } = useQueries();

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
        // A query is a clause over one field, so deleting the field takes its queries
        // with it and the views built on them read differently afterwards.
        markQueriesDirty();
        await refreshViews();
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

    const deleteEntries = async (selection: EntrySelection) => {
        await _deleteEntries(selection);
        markAnalyticsDirty();
    };

    const importEntries = async (file: File | null) => {
        await _importEntries(file);
        markAnalyticsDirty();
    };

    const recalculateEntries = async (selection: EntrySelection) => {
        await _recalculateEntries(selection);
        markAnalyticsDirty();
    };

    // ========================================
    // Analytic Operations
    // ========================================
    const addAnalytic = async (trackerAnalytic: CreateAnalyticDto) => {
        await _addAnalytic(trackerAnalytic);
    };

    const updateAnalytic = async (
        trackerAnalyticId: string,
        update: UpdateAnalyticDto
    ) => {
        await _updateAnalytic(trackerAnalyticId, update);
    };

    const removeAnalytic = async (trackerAnalyticId: string) => {
        await _removeAnalytic(trackerAnalyticId);
    };

    // ========================================
    // View Operations
    // ========================================
    const createView = async (view: CreateViewDto) => {
        await _createView(view);
        // Creating a view can also create brand-new ad-hoc queries, so the
        // cached queries list needs refreshing before it's trusted again.
        markQueriesDirty();
    };

    const updateView = async (viewId: string, view: UpdateViewDto) => {
        await _updateView(viewId, view);
        markEntriesDirty();
        markAnalyticsDirty();
        // Same as above — editing a view can create new ad-hoc queries.
        markQueriesDirty();
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
    // Query Operations
    // ========================================
    const createQuery = async (query: CreateQueryDto) => await _createQuery(query);

    // Editing or deleting a query changes every view built on it, so the cached views
    // and anything drawn through them have to be re-read.
    const updateQuery = async (queryId: string, query: UpdateQueryDto) => {
        const saved = await _updateQuery(queryId, query);
        await refreshViews();
        markEntriesDirty();
        markAnalyticsDirty();
        return saved;
    };

    const deleteQuery = async (queryId: string) => {
        await _deleteQuery(queryId);
        await refreshViews();
        markEntriesDirty();
        markAnalyticsDirty();
    };

    // ========================================
    // Tracker Operations
    // ========================================

    const setSelectedView = async (viewId: string | null) => {
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
        recalculateEntries,

        // Analytic operations
        addAnalytic,
        updateAnalytic,
        removeAnalytic,

        // View operations
        createView,
        updateView,
        deleteView,
        updateViewOrder,

        // Query operations
        createQuery,
        updateQuery,
        deleteQuery,

        // Tracker operations
        setSelectedView,
    };
};
