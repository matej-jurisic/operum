import React, { createContext, useCallback, useContext, useState } from "react";
import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { dashboardController } from "../api/dashboardController";
import { AddDashboardItemDto, DashboardDto } from "../types/DashboardDto";

type DashboardContextType = {
    dashboard: DashboardDto | null;
    analytics: AnalyticDto[];
    isLoading: boolean;
    refreshDashboard: () => Promise<void>;
    refreshAnalytics: () => Promise<void>;
    addItem: (dto: AddDashboardItemDto) => Promise<void>;
    removeItem: (itemId: string) => Promise<void>;
    reorderItems: (orderedIds: string[]) => Promise<void>;
};

const DashboardContext = createContext<DashboardContextType | undefined>(undefined);

export const DashboardProvider: React.FC<{
    dashboardId: string;
    children: React.ReactNode;
}> = ({ dashboardId, children }) => {
    const [dashboard, setDashboard] = useState<DashboardDto | null>(null);
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);

    const refreshDashboard = useCallback(async () => {
        const res = await dashboardController.getDashboard(dashboardId);
        setDashboard(res.data);
    }, [dashboardId]);

    const refreshAnalytics = useCallback(async () => {
        setIsLoading(true);
        const res = await dashboardController.getDashboardAnalytics(dashboardId);
        setAnalytics(res.data ?? []);
        setIsLoading(false);
    }, [dashboardId]);

    const addItem = async (dto: AddDashboardItemDto) => {
        await dashboardController.addDashboardItem(dashboardId, dto);
        await Promise.all([refreshDashboard(), refreshAnalytics()]);
    };

    const removeItem = async (itemId: string) => {
        await dashboardController.removeDashboardItem(dashboardId, itemId);
        await Promise.all([refreshDashboard(), refreshAnalytics()]);
    };

    const reorderItems = async (orderedIds: string[]) => {
        await dashboardController.reorderDashboardItems(dashboardId, orderedIds);
    };

    return (
        <DashboardContext.Provider
            value={{
                dashboard,
                analytics,
                isLoading,
                refreshDashboard,
                refreshAnalytics,
                addItem,
                removeItem,
                reorderItems,
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
