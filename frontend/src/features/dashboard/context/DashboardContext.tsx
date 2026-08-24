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
    DashboardLayoutItemDto,
    DashboardWidgetDto,
} from "../types/DashboardDto";

type DashboardContextType = {
    widgets: DashboardWidgetDto[];
    isLoading: boolean;
    refreshWidgets: () => Promise<void>;
    addItem: (dto: AddDashboardItemDto) => Promise<void>;
    addItemFromAnalytic: (dto: AddDashboardItemFromAnalyticDto) => Promise<void>;
    removeItem: (itemId: string) => Promise<void>;
    saveLayout: (layout: DashboardLayoutItemDto[]) => Promise<void>;
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

    const removeItem = async (itemId: string) => {
        await dashboardController.removeDashboardItem(dashboardId, itemId);
        await refreshWidgets();
    };

    // The grid has already moved the cards on screen by the time this runs, so the new
    // placement is kept locally rather than re-fetched: re-reading the board would
    // recalculate every chart just to redraw them where they already are.
    const saveLayout = async (layout: DashboardLayoutItemDto[]) => {
        setWidgets((current) =>
            current.map((widget) => {
                const placement = layout.find((l) => l.itemId === widget.id);
                return placement
                    ? {
                          ...widget,
                          layout: {
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
