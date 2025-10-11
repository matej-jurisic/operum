import {
    closestCorners,
    DndContext,
    DragEndEvent,
    PointerSensor,
    useSensor,
    useSensors,
} from "@dnd-kit/core";
import {
    arrayMove,
    rectSortingStrategy,
    SortableContext,
    useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { SimpleGrid, Stack } from "@mantine/core";
import { CSSProperties, useEffect, useState } from "react";

import { restrictToParentElement } from "@dnd-kit/modifiers";
import React from "react";
import { useTracker } from "../../trackers/context/TrackerContext";
import { analyticsController } from "../api/analyticsController";
import {
    AnalyticDto,
    CalendarAnalyticResultDto,
    NumericChartAnalyticResultDto,
    ScatterPlotAnalyticResultDto,
    SingleValueAnalyticResultDto,
} from "../types/AnalyticDto";
import { CalendarCard } from "./CalendarCard";
import { LineChartCard } from "./LineChartCard";
import { ScatterPlotCard } from "./ScatterPlotCard";
import { StatCard } from "./StatCard";

export const StatCardMemo = React.memo(StatCard);
export const LineChartCardMemo = React.memo(LineChartCard);
export const ScatterPlotCardMemo = React.memo(ScatterPlotCard);

interface AnalyticsGridProps {
    analytics: AnalyticDto[];
    isConfiguring: boolean;
    onEntryClick: (entryId: string) => void;
}

interface SortableCardWrapperProps {
    id: string;
    children: React.ReactNode;
    isReordering: boolean;
}

function SortableCardWrapper({
    id,
    children,
    isReordering,
}: SortableCardWrapperProps) {
    const sortable = useSortable({ id, disabled: !isReordering });

    const style: CSSProperties = {
        transform: CSS.Translate.toString(sortable.transform),
        transition: sortable.isDragging ? sortable.transition : "none",
        opacity: sortable.isDragging ? 0.7 : 1,
        touchAction: isReordering ? "none" : "pan-y",
        cursor: isReordering ? "grab" : "default",
    };

    return (
        <div
            ref={sortable.setNodeRef}
            style={style}
            {...sortable.attributes}
            {...sortable.listeners}
        >
            {children}
        </div>
    );
}

export function AnalyticsGrid({
    analytics,
    isConfiguring,
    onEntryClick,
}: AnalyticsGridProps) {
    const { tracker } = useTracker();

    // Local state to reflect current order
    const [orderedAnalytics, setOrderedAnalytics] = useState(analytics);

    // Sync local state when prop changes
    useEffect(() => {
        setOrderedAnalytics(analytics);
    }, [analytics]);

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
    );

    const handleDragEnd = (event: DragEndEvent) => {
        const { active, over } = event;
        if (!isConfiguring || !over || active.id === over.id) return;

        const oldIndex = orderedAnalytics.findIndex((a) => a.id === active.id);
        const newIndex = orderedAnalytics.findIndex((a) => a.id === over.id);

        const newOrder = arrayMove(orderedAnalytics, oldIndex, newIndex);
        setOrderedAnalytics(newOrder); // update UI immediately

        // Fire-and-forget backend update
        analyticsController.updateAnalyticsOrder(
            tracker.id,
            newOrder.map((x) => x.id)
        );
    };

    const renderCard = (analytic: AnalyticDto) => {
        switch (analytic.resultType) {
            case "SingleValue":
                return (
                    <StatCardMemo
                        analytic={analytic as SingleValueAnalyticResultDto}
                        isConfiguring={isConfiguring}
                        onEntryClick={onEntryClick}
                    />
                );
            case "NumericChart":
                return (
                    <LineChartCardMemo
                        analytic={analytic as NumericChartAnalyticResultDto}
                        isConfiguring={isConfiguring}
                    />
                );
            case "ScatterPlot":
                return (
                    <ScatterPlotCardMemo
                        analytic={analytic as ScatterPlotAnalyticResultDto}
                        isConfiguring={isConfiguring}
                    />
                );
            case "Calendar":
                return (
                    <CalendarCard
                        analytic={analytic as CalendarAnalyticResultDto}
                        isConfiguring={isConfiguring}
                        onEntryClick={onEntryClick}
                    />
                );
            default:
                return null;
        }
    };

    return (
        <Stack>
            <DndContext
                sensors={sensors}
                onDragEnd={handleDragEnd}
                modifiers={[restrictToParentElement]}
                collisionDetection={closestCorners}
            >
                <SortableContext
                    items={orderedAnalytics.map((a) => a.id)}
                    strategy={rectSortingStrategy}
                >
                    <SimpleGrid
                        cols={{ base: 1, sm: 2, md: 3, lg: 3, xl: 4 }}
                        spacing="md"
                    >
                        {orderedAnalytics.map((analytic) => (
                            <SortableCardWrapper
                                key={analytic.id}
                                id={analytic.id}
                                isReordering={isConfiguring}
                            >
                                {renderCard(analytic)}
                            </SortableCardWrapper>
                        ))}
                    </SimpleGrid>
                </SortableContext>
            </DndContext>
        </Stack>
    );
}
