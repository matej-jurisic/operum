import React, {
    createContext,
    useCallback,
    useContext,
    useState,
} from "react";
import { dashboardController } from "../api/dashboardController";
import {
    AddDashboardItemDto,
    AddDashboardItemFromAnalyticDto,
    AddDashboardQuickAddItemDto,
    AddDashboardViewItemDto,
    DashboardLayoutItemDto,
    DashboardWidgetDto,
    LayoutVariant,
    LayoutVariants,
} from "../types/DashboardDto";

type DashboardContextType = {
    widgets: DashboardWidgetDto[];
    isLoading: boolean;
    refreshWidgets: () => Promise<void>;
    addItem: (dto: AddDashboardItemDto) => Promise<void>;
    addItemFromAnalytic: (dto: AddDashboardItemFromAnalyticDto) => Promise<void>;
    addQuickAddItem: (dto: AddDashboardQuickAddItemDto) => Promise<void>;
    addViewItem: (dto: AddDashboardViewItemDto) => Promise<void>;
    setViewSelection: (itemId: string, viewId: string | null) => Promise<void>;
    removeItem: (itemId: string) => Promise<void>;
    saveLayout: (
        variant: LayoutVariant,
        layout: DashboardLayoutItemDto[]
    ) => Promise<void>;
};

const DashboardContext = createContext<DashboardContextType | undefined>(undefined);

export const DashboardProvider: React.FC<{
    dashboardId: string;
    children: React.ReactNode;
}> = ({ dashboardId, children }) => {
    const [widgets, setWidgets] = useState<DashboardWidgetDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);

    const refreshWidgets = useCallback(async () => {
        setIsLoading(true);
        const res = await dashboardController.getDashboardWidgets(dashboardId);
        setWidgets(res.data ?? []);
        setIsLoading(false);
    }, [dashboardId]);

    const addItem = async (dto: AddDashboardItemDto) => {
        await dashboardController.addDashboardItem(dashboardId, dto);
        await refreshWidgets();
    };

    const addItemFromAnalytic = async (dto: AddDashboardItemFromAnalyticDto) => {
        await dashboardController.addDashboardItemFromAnalytic(dashboardId, dto);
        await refreshWidgets();
    };

    const addQuickAddItem = async (dto: AddDashboardQuickAddItemDto) => {
        await dashboardController.addQuickAddItem(dashboardId, dto);
        await refreshWidgets();
    };

    const addViewItem = async (dto: AddDashboardViewItemDto) => {
        await dashboardController.addViewItem(dashboardId, dto);
        await refreshWidgets();
    };

    // The dropdown's own widget re-renders from the response immediately, same as any other
    // widget update, and so does every widget whose source is linked to it — the selection
    // is saved server-side (ViewWidgetConfigDto.ViewId), so this is the same "recompute the
    // whole board" the server already does on every load, not a client-only toggle.
    const setViewSelection = async (itemId: string, viewId: string | null) => {
        const res = await dashboardController.setViewWidgetSelection(dashboardId, itemId, {
            viewId,
        });
        setWidgets(res.data ?? []);
    };

    const removeItem = async (itemId: string) => {
        await dashboardController.removeDashboardItem(dashboardId, itemId);
        await refreshWidgets();
    };

    // The grid has already moved the cards on screen by the time this runs, so the new
    // placement is kept locally rather than re-fetched: re-reading the board would
    // recalculate every chart just to redraw them where they already are.
    //
    // Only the grid that was actually arranged is touched, on the client as well as on the
    // server: the other one is a different arrangement of the same widgets, not a stale
    // copy of this one.
    const saveLayout = async (
        variant: LayoutVariant,
        layout: DashboardLayoutItemDto[]
    ) => {
        const key =
            variant === LayoutVariants.Mobile ? "mobileLayout" : "layout";

        setWidgets((current) =>
            current.map((widget) => {
                const placement = layout.find((l) => l.itemId === widget.id);
                return placement
                    ? {
                          ...widget,
                          [key]: {
                              x: placement.x,
                              y: placement.y,
                              w: placement.w,
                              h: placement.h,
                          },
                      }
                    : widget;
            })
        );

        await dashboardController.updateDashboardLayout(dashboardId, {
            variant,
            items: layout,
        });
    };

    return (
        <DashboardContext.Provider
            value={{
                widgets,
                isLoading,
                refreshWidgets,
                addItem,
                addItemFromAnalytic,
                addQuickAddItem,
                addViewItem,
                setViewSelection,
                removeItem,
                saveLayout,
            }}
        >
            {children}
        </DashboardContext.Provider>
    );
};

export const useDashboard = () => {
    const ctx = useContext(DashboardContext);
    if (!ctx) throw new Error("useDashboard must be used within DashboardProvider");
    return ctx;
};
