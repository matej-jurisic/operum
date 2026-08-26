import React, { createContext, useCallback, useContext, useState } from "react";
import { widgetsController } from "../api/widgetsController";
import {
    CreateEntriesWidgetDto,
    CreateWidgetDto,
    EntriesWidgetDefinitionDto,
    UpdateEntriesWidgetDto,
    UpdateWidgetDto,
    WidgetDto,
} from "../types/WidgetDto";

type WidgetsContextType = {
    widgets: WidgetDto[];
    entriesWidgets: EntriesWidgetDefinitionDto[];
    isLoading: boolean;
    refresh: () => Promise<void>;
    createWidget: (dto: CreateWidgetDto) => Promise<WidgetDto>;
    updateWidget: (widgetId: string, dto: UpdateWidgetDto) => Promise<void>;
    deleteWidget: (widgetId: string) => Promise<void>;
    createEntriesWidget: (dto: CreateEntriesWidgetDto) => Promise<EntriesWidgetDefinitionDto>;
    updateEntriesWidget: (entriesWidgetId: string, dto: UpdateEntriesWidgetDto) => Promise<void>;
    deleteEntriesWidget: (entriesWidgetId: string) => Promise<void>;
};

const WidgetsContext = createContext<WidgetsContextType | undefined>(undefined);

/**
 * The Widget Library's state: every chart Widget and Entries EntriesWidget the current user
 * owns, app-scoped rather than nested under a tracker like AnalyticsContext used to be --
 * a widget can span several trackers, so it can't hang off one tracker's cache-invalidation
 * flag. Kept deliberately simple for now: refetch-on-mount plus a manual refresh after every
 * write, rather than the dirty-flag pattern the tracker-scoped contexts use.
 */
export const WidgetsProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const [widgets, setWidgets] = useState<WidgetDto[]>([]);
    const [entriesWidgets, setEntriesWidgets] = useState<EntriesWidgetDefinitionDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const refresh = useCallback(async () => {
        setIsLoading(true);
        const [widgetsRes, entriesRes] = await Promise.all([
            widgetsController.getWidgets(),
            widgetsController.getEntriesWidgets(),
        ]);
        setWidgets(widgetsRes.data ?? []);
        setEntriesWidgets(entriesRes.data ?? []);
        setIsLoading(false);
    }, []);

    const createWidget = async (dto: CreateWidgetDto) => {
        const res = await widgetsController.createWidget(dto);
        await refresh();
        return res.data;
    };

    const updateWidget = async (widgetId: string, dto: UpdateWidgetDto) => {
        await widgetsController.updateWidget(widgetId, dto);
        await refresh();
    };

    // Cascades on the server: every dashboard placing this widget loses that placement
    // too. The caller is expected to have warned the user before getting here.
    const deleteWidget = async (widgetId: string) => {
        await widgetsController.deleteWidget(widgetId);
        await refresh();
    };

    const createEntriesWidget = async (dto: CreateEntriesWidgetDto) => {
        const res = await widgetsController.createEntriesWidget(dto);
        await refresh();
        return res.data;
    };

    const updateEntriesWidget = async (
        entriesWidgetId: string,
        dto: UpdateEntriesWidgetDto
    ) => {
        await widgetsController.updateEntriesWidget(entriesWidgetId, dto);
        await refresh();
    };

    const deleteEntriesWidget = async (entriesWidgetId: string) => {
        await widgetsController.deleteEntriesWidget(entriesWidgetId);
        await refresh();
    };

    return (
        <WidgetsContext.Provider
            value={{
                widgets,
                entriesWidgets,
                isLoading,
                refresh,
                createWidget,
                updateWidget,
                deleteWidget,
                createEntriesWidget,
                updateEntriesWidget,
                deleteEntriesWidget,
            }}
        >
            {children}
        </WidgetsContext.Provider>
    );
};

export const useWidgets = () => {
    const ctx = useContext(WidgetsContext);
    if (!ctx) throw new Error("useWidgets must be used within WidgetsProvider");
    return ctx;
};
