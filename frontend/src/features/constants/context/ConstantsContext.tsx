import React, {
    createContext,
    useCallback,
    useContext,
    useState,
} from "react";
import { useTracker } from "../../trackers/context/TrackerContext";
import { trackerConstantsController } from "../api/trackerConstantsController";
import {
    CreateTrackerConstantDto,
    TrackerConstantDto,
    UpdateTrackerConstantDto,
} from "../types/TrackerConstantDto";

type ConstantsContextType = {
    constants: TrackerConstantDto[];
    constantsDirty: boolean;
    refreshConstants: () => Promise<void>;
    refreshConstantsIfDirty: () => Promise<void>;
    markConstantsDirty: () => void;
    _createConstant: (values: CreateTrackerConstantDto) => Promise<void>;
    _updateConstant: (constantId: string, values: UpdateTrackerConstantDto) => Promise<void>;
    _deleteConstant: (constantId: string) => Promise<void>;
};

const ConstantsContext = createContext<ConstantsContextType | undefined>(undefined);

export const ConstantsProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const { tracker } = useTracker();
    const [constants, setConstants] = useState<TrackerConstantDto[]>([]);
    const [constantsDirty, setConstantsDirty] = useState(true);

    const refreshConstants = useCallback(async () => {
        const response = await trackerConstantsController.getConstants(tracker.id);
        setConstants(response.data);
        setConstantsDirty(false);
    }, [tracker.id]);

    const refreshConstantsIfDirty = useCallback(async () => {
        if (constantsDirty) await refreshConstants();
    }, [constantsDirty, refreshConstants]);

    const markConstantsDirty = useCallback(() => setConstantsDirty(true), []);

    const _createConstant = async (values: CreateTrackerConstantDto) => {
        await trackerConstantsController.createConstant(tracker.id, values);
        await refreshConstants();
    };

    const _updateConstant = async (constantId: string, values: UpdateTrackerConstantDto) => {
        await trackerConstantsController.updateConstant(tracker.id, constantId, values);
        await refreshConstants();
    };

    const _deleteConstant = async (constantId: string) => {
        await trackerConstantsController.deleteConstant(tracker.id, constantId);
        await refreshConstants();
    };

    return (
        <ConstantsContext.Provider
            value={{
                constants,
                constantsDirty,
                refreshConstants,
                refreshConstantsIfDirty,
                markConstantsDirty,
                _createConstant,
                _updateConstant,
                _deleteConstant,
            }}
        >
            {children}
        </ConstantsContext.Provider>
    );
};

export const useConstants = () => {
    const ctx = useContext(ConstantsContext);
    if (!ctx) throw new Error("useConstants must be used within ConstantsProvider");
    return ctx;
};
