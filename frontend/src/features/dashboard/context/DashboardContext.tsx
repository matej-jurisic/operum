import React, {
    createContext,
    useCallback,
    useContext,
    useState,
} from "react";
import { dashboardController } from "../api/dashboardController";
import {
    AddDashboardHeaderItemDto,
    AddDashboardNoteItemDto,
    AddDashboardQuickAddItemDto,
    CreateAndPlaceEntriesWidgetDto,
    CreateAndPlaceWidgetDto,
    DashboardLayoutItemDto,
    DashboardWidgetDto,
    LayoutVariant,
    LayoutVariants,
    PlaceEntriesWidgetDto,
    PlaceWidgetDto,
    SaveParameterItemDto,
    SaveViewSelectorItemDto,
    UpdateDashboardEntriesItemDto,
    UpdateDashboardItemDto,
} from "../types/DashboardDto";

type DashboardContextType = {
    /** The board these widgets belong to, for anything that needs to read it back. */
    dashboardId: string;
    widgets: DashboardWidgetDto[];
    isLoading: boolean;
    refreshWidgets: () => Promise<void>;
    createAndPlaceWidget: (dto: CreateAndPlaceWidgetDto) => Promise<void>;
    placeWidget: (dto: PlaceWidgetDto) => Promise<void>;
    addQuickAddItem: (dto: AddDashboardQuickAddItemDto) => Promise<void>;
    addViewSelectorItem: (dto: SaveViewSelectorItemDto) => Promise<void>;
    addParameterItem: (dto: SaveParameterItemDto) => Promise<void>;
    createAndPlaceEntriesWidget: (dto: CreateAndPlaceEntriesWidgetDto) => Promise<void>;
    placeEntriesWidget: (dto: PlaceEntriesWidgetDto) => Promise<void>;
    addHeaderItem: (dto: AddDashboardHeaderItemDto) => Promise<void>;
    addDividerItem: () => Promise<void>;
    addNoteItem: (dto: AddDashboardNoteItemDto) => Promise<void>;
    updateItem: (itemId: string, dto: UpdateDashboardItemDto) => Promise<void>;
    updateEntriesItem: (itemId: string, dto: UpdateDashboardEntriesItemDto) => Promise<void>;
    setViewSelectorSelection: (itemId: string, selectedId: string | null) => Promise<void>;
    updateViewSelectorItem: (itemId: string, dto: SaveViewSelectorItemDto) => Promise<void>;
    setParameterValues: (
        itemId: string,
        values: Record<string, string | null>
    ) => Promise<void>;
    updateParameterItem: (itemId: string, dto: SaveParameterItemDto) => Promise<void>;
    setTextContent: (itemId: string, text: string) => Promise<void>;
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

    const createAndPlaceWidget = async (dto: CreateAndPlaceWidgetDto) => {
        await dashboardController.createAndPlaceWidget(dashboardId, dto);
        await refreshWidgets();
    };

    const placeWidget = async (dto: PlaceWidgetDto) => {
        await dashboardController.placeWidget(dashboardId, dto);
        await refreshWidgets();
    };

    const addQuickAddItem = async (dto: AddDashboardQuickAddItemDto) => {
        await dashboardController.addQuickAddItem(dashboardId, dto);
        await refreshWidgets();
    };

    const addViewSelectorItem = async (dto: SaveViewSelectorItemDto) => {
        await dashboardController.addViewSelectorItem(dashboardId, dto);
        await refreshWidgets();
    };

    const addParameterItem = async (dto: SaveParameterItemDto) => {
        await dashboardController.addParameterItem(dashboardId, dto);
        await refreshWidgets();
    };

    const createAndPlaceEntriesWidget = async (dto: CreateAndPlaceEntriesWidgetDto) => {
        await dashboardController.createAndPlaceEntriesWidget(dashboardId, dto);
        await refreshWidgets();
    };

    const placeEntriesWidget = async (dto: PlaceEntriesWidgetDto) => {
        await dashboardController.placeEntriesWidget(dashboardId, dto);
        await refreshWidgets();
    };

    const addHeaderItem = async (dto: AddDashboardHeaderItemDto) => {
        await dashboardController.addHeaderItem(dashboardId, dto);
        await refreshWidgets();
    };

    const addDividerItem = async () => {
        await dashboardController.addDividerItem(dashboardId);
        await refreshWidgets();
    };

    const addNoteItem = async (dto: AddDashboardNoteItemDto) => {
        await dashboardController.addNoteItem(dashboardId, dto);
        await refreshWidgets();
    };

    // Renders from the response for the same reason setViewSelection below does: an edit
    // can change how a widget is filtered, so the server hands back the whole board
    // recalculated rather than the client guessing at what moved.
    const updateItem = async (itemId: string, dto: UpdateDashboardItemDto) => {
        const res = await dashboardController.updateDashboardItem(
            dashboardId,
            itemId,
            dto
        );
        setWidgets(res.data ?? []);
    };

    // Same shape as updateItem: the tracker an Entries widget reads from is fixed, so only
    // its columns and its expandable flags can change, but a changed column set still
    // changes what the table shows, so the whole board comes back recomputed.
    const updateEntriesItem = async (itemId: string, dto: UpdateDashboardEntriesItemDto) => {
        const res = await dashboardController.updateEntriesItem(dashboardId, itemId, dto);
        setWidgets(res.data ?? []);
    };

    // The selector re-renders from the response immediately, and so does every widget it
    // links — the selection is saved server-side, so this is the same "recompute the whole
    // board" the server does on every load, not a client-only toggle.
    const setViewSelectorSelection = async (
        itemId: string,
        selectedId: string | null
    ) => {
        const res = await dashboardController.setViewSelectorSelection(dashboardId, itemId, {
            selectedId,
        });
        setWidgets(res.data ?? []);
    };

    // Same "recompute the whole board": the edit changes the selector's options, selection
    // and which widgets follow it, all of which change what those widgets draw.
    const updateViewSelectorItem = async (
        itemId: string,
        dto: SaveViewSelectorItemDto
    ) => {
        const res = await dashboardController.updateViewSelectorItem(dashboardId, itemId, dto);
        setWidgets(res.data ?? []);
    };

    // Same "recompute the whole board" as setViewSelectorSelection: the typed value is
    // saved server-side and every widget the parameter links re-filters by it.
    const setParameterValues = async (
        itemId: string,
        values: Record<string, string | null>
    ) => {
        const res = await dashboardController.setParameterValues(dashboardId, itemId, {
            values,
        });
        setWidgets(res.data ?? []);
    };

    const updateParameterItem = async (itemId: string, dto: SaveParameterItemDto) => {
        const res = await dashboardController.updateParameterItem(dashboardId, itemId, dto);
        setWidgets(res.data ?? []);
    };

    // Unlike updateItem/setViewSelection, nothing else on the board ever depends on a text
    // widget's own content, so the response (just that one item) is patched into place
    // rather than the whole board being recomputed and re-rendered from scratch.
    const setTextContent = async (itemId: string, text: string) => {
        const res = await dashboardController.setTextWidgetContent(dashboardId, itemId, {
            text,
        });
        if (!res.data) return;

        const config = res.data.config;
        setWidgets((current) =>
            current.map((widget) => (widget.id === itemId ? { ...widget, config } : widget))
        );
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
                              ...widget[key],
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
                dashboardId,
                widgets,
                isLoading,
                refreshWidgets,
                createAndPlaceWidget,
                placeWidget,
                addQuickAddItem,
                addViewSelectorItem,
                addParameterItem,
                createAndPlaceEntriesWidget,
                placeEntriesWidget,
                addHeaderItem,
                addDividerItem,
                addNoteItem,
                updateItem,
                updateEntriesItem,
                setViewSelectorSelection,
                updateViewSelectorItem,
                setParameterValues,
                updateParameterItem,
                setTextContent,
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
