import React, {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useState,
} from "react";
import { widgetsController } from "../api/widgetsController";
import {
    EntriesWidgetDefinitionDto,
    UpdateEntriesWidgetDto,
    UpdateWidgetDto,
    WidgetDto,
} from "../types/WidgetDto";

// A Widget Library definition is created together with its first placement, from the board
// (see DashboardContext.createAndPlaceWidget). This context only reads the library and
// edits/deletes what's already in it.
type WidgetsContextType = {
    widgets: WidgetDto[];
    entriesWidgets: EntriesWidgetDefinitionDto[];
    isLoading: boolean;
    refresh: () => Promise<void>;
    updateWidget: (widgetId: string, dto: UpdateWidgetDto) => Promise<void>;
    deleteWidget: (widgetId: string) => Promise<void>;
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

    useEffect(() => {
        refresh();
    }, [refresh]);

    return (
        <WidgetsContext.Provider
            value={{
                widgets,
                entriesWidgets,
                isLoading,
                refresh,
                updateWidget,
                deleteWidget,
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
