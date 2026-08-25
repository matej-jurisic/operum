import React, {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useState,
} from "react";
import { CreateFieldDto } from "../../fields/types/CreateFieldDto";
import { FieldDto } from "../../fields/types/FieldDto";
import { UpdateFieldDto } from "../../fields/types/UpdateFieldDto";
import { useTracker } from "../../trackers/context/TrackerContext";
import { fieldsController } from "../api/fieldsController";

type FieldsContextType = {
    fields: FieldDto[];
    fieldsDirty: boolean;
    /**
     * Columns the user has ticked on or off by hand for this session. What a column
     * defaults to is decided by the active view (see useVisibleColumns), so a column
     * missing from this record means "whatever the view says", not "hidden".
     */
    columnOverrides: Record<string, boolean>;
    refreshFields: () => Promise<void>;
    refreshFieldsIfDirty: () => Promise<void>;
    setColumnVisible: (columnId: string, visible: boolean) => void;
    resetColumnOverrides: () => void;
    markFieldsDirty: () => void;
    // API methods - internal use only
    _createField: (values: CreateFieldDto) => Promise<void>;
    _updateField: (fieldId: string, values: UpdateFieldDto) => Promise<void>;
    _updateFieldOrder: (fieldIds: string[]) => Promise<void>;
    _deleteField: (fieldId: string) => Promise<void>;
};

const FieldsContext = createContext<FieldsContextType | undefined>(undefined);

export const FieldsProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const { tracker, selectedViewId } = useTracker();
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [fieldsDirty, setFieldsDirty] = useState(true);
    const [columnOverrides, setColumnOverrides] = useState<
        Record<string, boolean>
    >({});

    const refreshFields = useCallback(async () => {
        const response = await fieldsController.getFields(tracker.id);
        setFields(response.data);
        setFieldsDirty(false);
    }, [tracker.id]);

    const refreshFieldsIfDirty = useCallback(async () => {
        if (fieldsDirty) await refreshFields();
    }, [fieldsDirty, refreshFields]);

    const markFieldsDirty = useCallback(() => setFieldsDirty(true), []);

    const setColumnVisible = useCallback(
        (columnId: string, visible: boolean) =>
            setColumnOverrides((prev) => ({ ...prev, [columnId]: visible })),
        []
    );

    const resetColumnOverrides = useCallback(() => setColumnOverrides({}), []);

    // The new view brings its own columns, so hand-picked ones from the old one go.
    useEffect(() => {
        setColumnOverrides({});
    }, [selectedViewId]);

    const _createField = async (values: CreateFieldDto) => {
        await fieldsController.createField(tracker.id, values);
        await refreshFields();
    };

    const _updateFieldOrder = async (fieldIds: string[]) => {
        await fieldsController.updateFieldOrder(tracker.id, fieldIds);
        await refreshFields();
    };

    const _updateField = async (fieldId: string, values: UpdateFieldDto) => {
        await fieldsController.updateField(tracker.id, fieldId, values);
        await refreshFields();
    };

    const _deleteField = async (fieldId: string) => {
        await fieldsController.deleteField(tracker.id, fieldId);
        await refreshFields();
    };

    return (
        <FieldsContext.Provider
            value={{
                fields,
                fieldsDirty,
                columnOverrides,
                refreshFields,
                refreshFieldsIfDirty,
                setColumnVisible,
                resetColumnOverrides,
                markFieldsDirty,
                _createField,
                _updateField,
                _updateFieldOrder,
                _deleteField,
            }}
        >
            {children}
        </FieldsContext.Provider>
    );
};

export const useFields = () => {
    const ctx = useContext(FieldsContext);
    if (!ctx) throw new Error("useFields must be used within FieldsProvider");
    return ctx;
};
