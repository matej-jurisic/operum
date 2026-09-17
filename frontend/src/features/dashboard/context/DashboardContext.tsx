import React, {
    createContext,
    useCallback,
    useContext,
    useState,
} from "react";
import { dashboardController } from "../api/dashboardController";
import {
    FilterFollowLinks,
    filterWidgetIndexBySlotId,
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
    GoalConditionalTargetDto,
    LayoutVariant,
    LayoutVariants,
    PlaceEntriesWidgetDto,
    PlaceWidgetDto,
    SaveFilterItemDto,
    SaveTabsContainerDto,
    UpdateDashboardEntriesItemDto,
    UpdateDashboardItemDto,
} from "../types/DashboardDto";

type DashboardContextType = {
    /** The board these widgets belong to, for anything that needs to read it back. */
    dashboardId: string;
    widgets: DashboardWidgetDto[];
    isLoading: boolean;
    refreshWidgets: () => Promise<void>;
    /** followFilters links the new widget's tracker(s) to existing filter widgets in the
        same step, so it never loads unfiltered first. See FilterFollowChecklist. */
    createAndPlaceWidget: (
        dto: CreateAndPlaceWidgetDto,
        followFilters?: FilterFollowLinks[],
        goalConditionalTargets?: GoalConditionalTargetDto[],
    ) => Promise<DashboardItemDto | undefined>;
    placeWidget: (
        dto: PlaceWidgetDto,
        followFilters?: FilterFollowLinks[],
        goalConditionalTargets?: GoalConditionalTargetDto[],
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
    addTabsContainerItem: () => Promise<void>;
    saveTabsContainer: (itemId: string, dto: SaveTabsContainerDto) => Promise<void>;
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

    // Resubmits each affected filter widget once, even when more than one source follows it.
    const applyFilterFollows = async (itemId: string, sources: FilterFollowLinks[]) => {
        const byFilter = new Map<
            string,
            { trackerId: string; fieldBySlotId: Record<string, string> }[]
        >();
        for (const { trackerId, links } of sources) {
            for (const [filterItemId, fieldBySlotId] of Object.entries(links)) {
                if (Object.keys(fieldBySlotId).length === 0) continue;
                const list = byFilter.get(filterItemId) ?? [];
                list.push({ trackerId, fieldBySlotId });
                byFilter.set(filterItemId, list);
            }
        }
        for (const [filterItemId, followers] of byFilter) {
            const widget = widgets.find((w) => w.id === filterItemId);
            if (!widget) continue;
            const indexBySlotId = filterWidgetIndexBySlotId(widget);
            const dto = filterWidgetToSaveDto(widget);
            dto.links = [
                ...dto.links,
                ...followers.map((f) => toFollowerLink(indexBySlotId, { itemId, ...f })),
            ];
            await dashboardController.updateFilterItem(dashboardId, filterItemId, dto);
        }
    };

    // Targets are validated against the filter clauses followed, so both add paths apply
    // the follow links first, then send targets through the normal item-update endpoint.
    const saveGoalConditionalTargets = async (
        item: DashboardItemDto,
        goalConditionalTargets: GoalConditionalTargetDto[],
    ) => {
        await dashboardController.updateDashboardItem(dashboardId, item.id, {
            displayMode: item.layout.displayMode,
            mobileDisplayMode: item.mobileLayout.displayMode,
            yAxisFromZero: item.yAxisFromZero,
            goalConditionalTargets,
            color: item.color,
            showTrend: item.showTrend,
            sources: item.sources.map((s) => ({
                sourceId: s.id,
                label: s.label ?? null,
                viewId: s.viewId ?? null,
            })),
        });
    };

    const createAndPlaceWidget = async (
        dto: CreateAndPlaceWidgetDto,
        followFilters?: FilterFollowLinks[],
        goalConditionalTargets?: GoalConditionalTargetDto[],
    ) => {
        const res = await dashboardController.createAndPlaceWidget(dashboardId, dto);
        if (res.data && followFilters?.length) {
            await applyFilterFollows(res.data.id, followFilters);
        }
        if (res.data && goalConditionalTargets?.length) {
            await saveGoalConditionalTargets(res.data, goalConditionalTargets);
        }
        await refreshWidgets();
        return res.data;
    };

    const placeWidget = async (
        dto: PlaceWidgetDto,
        followFilters?: FilterFollowLinks[],
        goalConditionalTargets?: GoalConditionalTargetDto[],
    ) => {
        const res = await dashboardController.placeWidget(dashboardId, dto);
        if (res.data && followFilters?.length) {
            await applyFilterFollows(res.data.id, followFilters);
        }
        if (res.data && goalConditionalTargets?.length) {
            await saveGoalConditionalTargets(res.data, goalConditionalTargets);
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

    const addTabsContainerItem = async () => {
        await dashboardController.addTabsContainerItem(dashboardId);
        await refreshWidgets();
    };

    // Removing a tab moves its child widgets to the first remaining tab, so the server
    // hands back the whole board recalculated rather than the client guessing what moved.
    const saveTabsContainer = async (itemId: string, dto: SaveTabsContainerDto) => {
        const res = await dashboardController.saveTabsContainer(dashboardId, itemId, dto);
        setWidgets(res.data ?? []);
    };

    // An edit can change how a widget is filtered, so the server recalculates the whole board.
    const updateItem = async (itemId: string, dto: UpdateDashboardItemDto) => {
        const res = await dashboardController.updateDashboardItem(
            dashboardId,
            itemId,
            dto
        );
        setWidgets(res.data ?? []);
    };

    // A changed column set still changes what the table shows, so the whole board recomputes.
    const updateEntriesItem = async (itemId: string, dto: UpdateDashboardEntriesItemDto) => {
        const res = await dashboardController.updateEntriesItem(dashboardId, itemId, dto);
        setWidgets(res.data ?? []);
    };

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

    // Nothing else on the board depends on a text widget's content, so only that item is patched in.
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

    // Kept local rather than re-fetched: re-reading the board would recalculate every chart
    // just to redraw them where they already are. Only the grid actually arranged is touched.
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
                    // Only the wide grid nests, so a mobile save leaves the desktop container alone.
                    parentItemId: isMobile
                        ? widget.parentItemId
                        : (placement.parentItemId ?? undefined),
                    parentTabId: isMobile
                        ? widget.parentTabId
                        : (placement.parentTabId ?? undefined),
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
                addTabsContainerItem,
                saveTabsContainer,
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
