import React, { createContext, useCallback, useContext, useState } from "react";
import { useTracker } from "../../trackers/context/TrackerContext";
import { ViewDto } from "../../views/types/ViewDto";
import { CreateViewDto } from "../../views/types/requests/CreateViewDto";
import { UpdateViewDto } from "../../views/types/requests/UpdateViewDto";
import { viewsController } from "../api/viewsController";

type ViewsContextType = {
    views: ViewDto[];
    refreshViews: () => Promise<void>;
    refreshViewsIfDirty: () => Promise<void>;
    // API methods - internal use only
    _createView: (view: CreateViewDto) => Promise<void>;
    _updateView: (viewId: string, view: UpdateViewDto) => Promise<void>;
    _deleteView: (viewId: string) => Promise<void>;
    _updateViewOrder: (viewIds: string[]) => Promise<void>;
};

const ViewsContext = createContext<ViewsContextType | undefined>(undefined);

export const ViewsProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const { tracker, selectedViewIds, _setSelectedViewIds } = useTracker();
    const [views, setViews] = useState<ViewDto[]>([]);
    const [viewsDirty, setViewsDirty] = useState(true);

    const refreshViews = useCallback(async () => {
        const response = await viewsController.getViewList(tracker.id);
        setViews(response.data);
        setViewsDirty(false);
    }, [tracker.id]);

    const refreshViewsIfDirty = useCallback(async () => {
        if (viewsDirty) await refreshViews();
    }, [viewsDirty, refreshViews]);

    const _createView = async (viewData: CreateViewDto) => {
        await viewsController.createView(tracker.id, viewData);
        await refreshViews();
    };

    const _updateView = async (viewId: string, viewData: UpdateViewDto) => {
        await viewsController.updateView(tracker.id, viewId, viewData);
        await refreshViews();
    };

    const _deleteView = async (viewId: string) => {
        await viewsController.deleteView(tracker.id, viewId);
        if (selectedViewIds.includes(viewId)) {
            _setSelectedViewIds(selectedViewIds.filter((id) => id !== viewId));
        }
        await refreshViews();
    };

    const _updateViewOrder = async (viewIds: string[]) => {
        await viewsController.updateViewOrder(tracker.id, viewIds);
        await refreshViews();
    };

    return (
        <ViewsContext.Provider
            value={{
                views,
                refreshViews,
                refreshViewsIfDirty,
                _createView,
                _updateView,
                _deleteView,
                _updateViewOrder,
            }}
        >
            {children}
        </ViewsContext.Provider>
    );
};

export const useViews = () => {
    const ctx = useContext(ViewsContext);
    if (!ctx) throw new Error("useViews must be used within ViewsProvider");
    return ctx;
};
