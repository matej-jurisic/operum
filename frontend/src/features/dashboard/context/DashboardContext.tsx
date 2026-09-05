import React, {
    createContext,
    useCallback,
    useContext,
    useState,
} from "react";
import { dashboardController } from "../api/dashboardController";
import {
    FilterFollowLinks,
    filterWidgetIndexByQueryId,
    filterWidgetToSaveDto,
    toFollowerLink,
} from "../components/filterLinkUtils";
import {
    AddDashboardHeaderItemDto,
    AddDashboardNoteItemDto,
    AddDashboardQuickAddItemDto,
    CreateAndPlaceEntriesWidgetDto,
    CreateAndPlaceWidgetDto,
    DashboardItemDto,
    DashboardLayoutItemDto,
    DashboardWidgetDto,
    LayoutVariant,
    LayoutVariants,
    PlaceEntriesWidgetDto,
    PlaceWidgetDto,
    SaveFilterItemDto,
    UpdateDashboardEntriesItemDto,
    UpdateDashboardItemDto,
} from "../types/DashboardDto";

type DashboardContextType = {
    /** The board these widgets belong to, for anything that needs to read it back. */
    dashboardId: string;
    widgets: DashboardWidgetDto[];
    isLoading: boolean;
    refreshWidgets: () => Promise<void>;
    /** followFilters carries which of the board's existing filter widgets to link the new
        widget's tracker(s) to, one entry per source, applied in the same step so it never
        loads unfiltered first. See FilterFollowChecklist. */
    createAndPlaceWidget: (
        dto: CreateAndPlaceWidgetDto,
        followFilters?: FilterFollowLinks[],
    ) => Promise<DashboardItemDto | undefined>;
    placeWidget: (
        dto: PlaceWidgetDto,
        followFilters?: FilterFollowLinks[],
    ) => Promise<DashboardItemDto | undefined>;
    addQuickAddItem: (dto: AddDashboardQuickAddItemDto) => Promise<void>;
    addFilterItem: (dto: SaveFilterItemDto) => Promise<void>;
    createAndPlaceEntriesWidget: (
        dto: CreateAndPlaceEntriesWidgetDto,
        followFilters?: FilterFollowLinks,
    ) => Promise<DashboardItemDto | undefined>;
    placeEntriesWidget: (
        dto: PlaceEntriesWidgetDto,
        followFilters?: FilterFollowLinks,
    ) => Promise<DashboardItemDto | undefined>;
    addHeaderItem: (dto: AddDashboardHeaderItemDto) => Promise<void>;
    addDividerItem: () => Promise<void>;
    addNoteItem: (dto: AddDashboardNoteItemDto) => Promise<void>;
    addContainerItem: () => Promise<void>;
    updateItem: (itemId: string, dto: UpdateDashboardItemDto) => Promise<void>;
    updateEntriesItem: (itemId: string, dto: UpdateDashboardEntriesItemDto) => Promise<void>;
    setFilterValues: (
        itemId: string,
        values: Record<string, string | null>
    ) => Promise<void>;
    updateFilterItem: (itemId: string, dto: SaveFilterItemDto) => Promise<void>;
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

    // Appends one link per source to whichever existing filter widget it names, resubmitting
    // each affected filter widget once (even when more than one source follows it) so an
    // "unfiltered" first paint is never visible once the new widget lands on the board.
    const applyFilterFollows = async (itemId: string, sources: FilterFollowLinks[]) => {
        const byFilter = new Map<
            string,
            { trackerId: string; fieldByQueryId: Record<string, string> }[]
        >();
        for (const { trackerId, links } of sources) {
            for (const [filterItemId, fieldByQueryId] of Object.entries(links)) {
                if (Object.keys(fieldByQueryId).length === 0) continue;
                const list = byFilter.get(filterItemId) ?? [];
                list.push({ trackerId, fieldByQueryId });
                byFilter.set(filterItemId, list);
            }
        }
        for (const [filterItemId, followers] of byFilter) {
            const widget = widgets.find((w) => w.id === filterItemId);
            if (!widget) continue;
            const indexByQueryId = filterWidgetIndexByQueryId(widget);
            const dto = filterWidgetToSaveDto(widget);
            dto.links = [
                ...dto.links,
                ...followers.map((f) => toFollowerLink(indexByQueryId, { itemId, ...f })),
            ];
            await dashboardController.updateFilterItem(dashboardId, filterItemId, dto);
        }
    };

    const createAndPlaceWidget = async (
        dto: CreateAndPlaceWidgetDto,
        followFilters?: FilterFollowLinks[],
    ) => {
        const res = await dashboardController.createAndPlaceWidget(dashboardId, dto);
        if (res.data && followFilters?.length) {
            await applyFilterFollows(res.data.id, followFilters);
        }
        await refreshWidgets();
        return res.data;
    };

    const placeWidget = async (dto: PlaceWidgetDto, followFilters?: FilterFollowLinks[]) => {
        const res = await dashboardController.placeWidget(dashboardId, dto);
        if (res.data && followFilters?.length) {
            await applyFilterFollows(res.data.id, followFilters);
        }
        await refreshWidgets();
        return res.data;
    };

    const addQuickAddItem = async (dto: AddDashboardQuickAddItemDto) => {
        await dashboardController.addQuickAddItem(dashboardId, dto);
        await refreshWidgets();
    };

    const addFilterItem = async (dto: SaveFilterItemDto) => {
        await dashboardController.addFilterItem(dashboardId, dto);
        await refreshWidgets();
    };

    const createAndPlaceEntriesWidget = async (
        dto: CreateAndPlaceEntriesWidgetDto,
        followFilters?: FilterFollowLinks,
    ) => {
        const res = await dashboardController.createAndPlaceEntriesWidget(dashboardId, dto);
        if (res.data && followFilters) {
            await applyFilterFollows(res.data.id, [followFilters]);
        }
        await refreshWidgets();
        return res.data;
    };

    const placeEntriesWidget = async (
        dto: PlaceEntriesWidgetDto,
        followFilters?: FilterFollowLinks,
    ) => {
        const res = await dashboardController.placeEntriesWidget(dashboardId, dto);
        if (res.data && followFilters) {
            await applyFilterFollows(res.data.id, [followFilters]);
        }
        await refreshWidgets();
        return res.data;
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

    const addContainerItem = async () => {
        await dashboardController.addContainerItem(dashboardId);
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
    // its columns and its per-grid display mode can change, but a changed column set still
    // changes what the table shows, so the whole board comes back recomputed.
    const updateEntriesItem = async (itemId: string, dto: UpdateDashboardEntriesItemDto) => {
        const res = await dashboardController.updateEntriesItem(dashboardId, itemId, dto);
        setWidgets(res.data ?? []);
    };

    // Same "recompute the whole board" as setFilterPreset: the typed value is saved
    // server-side and every widget the filter widget's own clauses link re-filters by it.
    const setFilterValues = async (
        itemId: string,
        values: Record<string, string | null>
    ) => {
        const res = await dashboardController.setFilterValues(dashboardId, itemId, {
            values,
        });
        setWidgets(res.data ?? []);
    };

    const updateFilterItem = async (itemId: string, dto: SaveFilterItemDto) => {
        const res = await dashboardController.updateFilterItem(dashboardId, itemId, dto);
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
        const isMobile = variant === LayoutVariants.Mobile;
        const key = isMobile ? "mobileLayout" : "layout";

        setWidgets((current) =>
            current.map((widget) => {
                const placement = layout.find((l) => l.itemId === widget.id);
                if (!placement) return widget;

                return {
                    ...widget,
                    // The wide grid is the only one that nests, so a mobile save leaves
                    // whatever container a widget belongs to on the desktop board alone.
                    // Applying it here as well as server-side keeps the grid from
                    // re-deriving the old tree and bouncing a just-moved tile back.
                    parentItemId: isMobile
                        ? widget.parentItemId
                        : (placement.parentItemId ?? undefined),
                    [key]: {
                        ...widget[key],
                        x: placement.x,
                        y: placement.y,
                        w: placement.w,
                        h: placement.h,
                    },
                };
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
                addFilterItem,
                createAndPlaceEntriesWidget,
                placeEntriesWidget,
                addHeaderItem,
                addDividerItem,
                addNoteItem,
                addContainerItem,
                updateItem,
                updateEntriesItem,
                setFilterValues,
                updateFilterItem,
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
