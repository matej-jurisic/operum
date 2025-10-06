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
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import {
    AnalyticResultDto,
    NumericChartAnalyticResultDto,
    ScatterChartAnalyticResultDto,
    SingleValueAnalyticResultDto,
} from "../model/AnalyticResultDto";
import { TrackerAnalyticsResponseDto } from "../model/TrackerAnalyticsResponseDto";
import { ChartCard } from "./ChartCard";
import { ScatterChartCard } from "./ScatterChartCard";
import { StatCard } from "./StatCard";

export const StatCardMemo = React.memo(StatCard);
export const ChartCardMemo = React.memo(ChartCard);
export const ScatterChartCardMemo = React.memo(ScatterChartCard);

interface AnalyticsGridProps {
    analytics: TrackerAnalyticsResponseDto;
    isConfiguring: boolean;
    onEntryClick: (entryId: string) => void;
}

const UpdateAnalyticsOrder = async (
    trackerId: string,
    trackerAnalyticIds: string[]
) => {
    await api.put(`/trackers/${trackerId}/analytics/reorder`, {
        trackerAnalyticIds,
    });
};

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
    const [orderedAnalytics, setOrderedAnalytics] = useState(
        analytics.analytics
    );

    // Sync local state when prop changes
    useEffect(() => {
        setOrderedAnalytics(analytics.analytics);
    }, [analytics.analytics]);

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
    );

    const handleDragEnd = (event: DragEndEvent) => {
        const { active, over } = event;
        if (!isConfiguring || !over || active.id === over.id) return;

        const oldIndex = orderedAnalytics.findIndex(
            (a) => a.trackerAnalyticId === active.id
        );
        const newIndex = orderedAnalytics.findIndex(
            (a) => a.trackerAnalyticId === over.id
        );

        const newOrder = arrayMove(orderedAnalytics, oldIndex, newIndex);
        setOrderedAnalytics(newOrder); // update UI immediately

        // Fire-and-forget backend update
        UpdateAnalyticsOrder(
            tracker.id,
            newOrder.map((a) => a.trackerAnalyticId)
        ).catch((err) => {
            console.error("Failed to update analytics order:", err);
        });
    };

    const renderCard = (analytic: AnalyticResultDto) => {
        switch (analytic.resultType) {
            case "SingleValue":
                return (
                    <StatCardMemo
                        analytic={analytic as SingleValueAnalyticResultDto}
                        onEntryClick={onEntryClick}
                        isConfiguring={isConfiguring}
                    />
                );
            case "NumericChart":
                return (
                    <ChartCardMemo
                        analytic={analytic as NumericChartAnalyticResultDto}
                        isConfiguring={isConfiguring}
                    />
                );
            case "ScatterChart":
                return (
                    <ScatterChartCardMemo
                        analytic={analytic as ScatterChartAnalyticResultDto}
                        isConfiguring={isConfiguring}
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
                    items={orderedAnalytics.map((a) => a.trackerAnalyticId)}
                    strategy={rectSortingStrategy}
                >
                    <SimpleGrid
                        cols={{ base: 1, sm: 2, md: 3, lg: 3, xl: 4 }}
                        spacing="md"
                    >
                        {orderedAnalytics.map((analytic) => (
                            <SortableCardWrapper
                                key={analytic.trackerAnalyticId}
                                id={analytic.trackerAnalyticId}
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
