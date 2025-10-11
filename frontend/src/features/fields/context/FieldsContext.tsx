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
    visibleColumns: Record<string, boolean>;
    visibleFields: FieldDto[];
    refreshFields: () => Promise<void>;
    refreshFieldsIfDirty: () => Promise<void>;
    toggleColumn: (columnId: string) => void;
    setAllColumnsVisible: (visible: boolean) => void;
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
    const { tracker } = useTracker();
    const [fields, setFields] = useState<FieldDto[]>([]);
    const [fieldsDirty, setFieldsDirty] = useState(true);
    const [visibleColumns, setVisibleColumns] = useState<
        Record<string, boolean>
    >({});

    // Initialize column visibility when fields are loaded
    useEffect(() => {
        if (fields.length > 0) {
            const initialVisibility: Record<string, boolean> = {};

            fields.forEach((field) => {
                if (!visibleColumns.hasOwnProperty(field.id)) {
                    initialVisibility[field.id] = true;
                }
            });

            if (!visibleColumns.hasOwnProperty("createdAt")) {
                initialVisibility["createdAt"] = true;
            }
            if (!visibleColumns.hasOwnProperty("actions")) {
                initialVisibility["actions"] = true;
            }

            if (Object.keys(initialVisibility).length > 0) {
                setVisibleColumns((prev) => ({
                    ...prev,
                    ...initialVisibility,
                }));
            }
        }
    }, [fields]);

    const visibleFields = fields.filter((field) => visibleColumns[field.id]);

    const refreshFields = useCallback(async () => {
        const response = await fieldsController.getFields(tracker.id);
        setFields(response.data);
        setFieldsDirty(false);
    }, [tracker.id]);

    const refreshFieldsIfDirty = useCallback(async () => {
        if (fieldsDirty) await refreshFields();
    }, [fieldsDirty, refreshFields]);

    const markFieldsDirty = useCallback(() => setFieldsDirty(true), []);

    const toggleColumn = useCallback((columnId: string) => {
        setVisibleColumns((prev) => ({
            ...prev,
            [columnId]: !prev[columnId],
        }));
    }, []);

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
                visibleColumns,
                visibleFields,
                refreshFields,
                refreshFieldsIfDirty,
                toggleColumn,
                setAllColumnsVisible,
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
