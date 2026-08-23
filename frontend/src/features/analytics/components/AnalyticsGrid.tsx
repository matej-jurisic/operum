import {
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
import { Indicator, Stack } from "@mantine/core";
import { CSSProperties, useEffect, useState } from "react";

import { restrictToFirstScrollableAncestor } from "@dnd-kit/modifiers";
import React from "react";
import Masonry, { ResponsiveMasonry } from "react-responsive-masonry";
import { AnalyticResultTypeEnum } from "../enums/AnalyticResultTypeEnum";
import {
    AnalyticDto,
    BarChartAnalyticDto,
    CalendarAnalyticDto,
    ComposedChartAnalyticDto,
    DonutChartAnaylticDto,
    LineChartAnalyticDto,
    ScatterChartAnalyticDto,
    SingleValueAnalyticDto,
} from "../types/AnalyticDto";
import { BarChartCard } from "./BarChartCard";
import { CalendarCard } from "./CalendarCard";
import { ComposedChartCard } from "./ComposedChartCard";
import { DonutChartCard } from "./DonutChartCard";
import { LineChartCard } from "./LineChartCard";
import { closestToPointer } from "./MasonryCollision";
import { ScatterChartCard } from "./ScatterChartCard";
import { SingleValueCard } from "./SingleValueCard";

export const SingleValueCardMemo = React.memo(SingleValueCard);
export const LineChartCardMemo = React.memo(LineChartCard);
export const ScatterChartCardMemo = React.memo(ScatterChartCard);
export const CalendarChartCardMemo = React.memo(CalendarCard);
export const DonutChartCardMemo = React.memo(DonutChartCard);
export const BarChartCardMemo = React.memo(BarChartCard);
export const ComposedChartCardMemo = React.memo(ComposedChartCard);

interface AnalyticsGridProps {
    analytics: AnalyticDto[];
    color: string | undefined;
    isConfiguring: boolean;
    onReorder: (orderedIds: string[]) => void;
    onRemove?: (analyticId: string) => void;
    onEntryClick?: (entryId: string) => void;
}

interface SortableCardWrapperProps {
    id: string;
    children: React.ReactNode;
    isReordering: boolean;
    index: number;
    color: string | undefined;
}

function SortableCardWrapper({
    id,
    children,
    isReordering,
    index,
    color,
}: SortableCardWrapperProps) {
    const sortable = useSortable({ id, disabled: !isReordering });

    const style: CSSProperties = {
        transform: CSS.Translate.toString(sortable.transform),
        transition: sortable.isDragging ? sortable.transition : "none",
        opacity: sortable.isDragging ? 0.7 : 1,
        touchAction: isReordering ? "none" : "pan-y",
        cursor: isReordering ? "grab" : "default",
        width: "100%",
    };

    return (
        <div
            ref={sortable.setNodeRef}
            style={style}
            {...sortable.attributes}
            {...sortable.listeners}
        >
            <Indicator
                color={color}
                processing
                label={index + 1}
                position="bottom-center"
                size={20}
                disabled={!isReordering}
            >
                {children}
            </Indicator>
        </div>
    );
}

export function AnalyticsGrid({
    analytics,
    color,
    isConfiguring,
    onReorder,
    onRemove,
    onEntryClick,
}: AnalyticsGridProps) {
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

        onReorder(newOrder.map((x) => x.id));
    };

    const renderCard = (analytic: AnalyticDto) => {
        switch (analytic.resultType) {
            case AnalyticResultTypeEnum.SingleValue:
                return (
                    <SingleValueCardMemo
                        analytic={analytic as SingleValueAnalyticDto}
                        color={color}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
                        onEntryClick={onEntryClick}
                    />
                );
            case AnalyticResultTypeEnum.LineChart:
                return (
                    <LineChartCardMemo
                        analytic={analytic as LineChartAnalyticDto}
                        color={color}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
                    />
                );
            case AnalyticResultTypeEnum.ScatterChart:
                return (
                    <ScatterChartCardMemo
                        analytic={analytic as ScatterChartAnalyticDto}
                        color={color}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
                    />
                );
            case AnalyticResultTypeEnum.Calendar:
                return (
                    <CalendarChartCardMemo
                        analytic={analytic as CalendarAnalyticDto}
                        color={color}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
                        onEntryClick={onEntryClick}
                    />
                );
            case AnalyticResultTypeEnum.Donut:
                return (
                    <DonutChartCardMemo
                        analytic={analytic as DonutChartAnaylticDto}
                        color={color}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
                    />
                );
            case AnalyticResultTypeEnum.BarChart:
                return (
                    <BarChartCardMemo
                        analytic={analytic as BarChartAnalyticDto}
                        color={color}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
                    />
                );
            case AnalyticResultTypeEnum.Composed:
                return (
                    <ComposedChartCardMemo
                        analytic={analytic as ComposedChartAnalyticDto}
                        color={color}
                        isConfiguring={isConfiguring}
                        onRemove={onRemove}
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
                modifiers={[restrictToFirstScrollableAncestor]}
                collisionDetection={closestToPointer}
            >
                <SortableContext
                    items={orderedAnalytics.map((a) => a.id)}
                    strategy={rectSortingStrategy}
                >
                    <ResponsiveMasonry
                        columnsCountBreakPoints={{
                            350: 1,
                            640: 2,
                            1024: 3,
                            1536: 4,
                        }}
                    >
                        <Masonry gutter="16px">
                            {orderedAnalytics.map((analytic, index) => (
                                <SortableCardWrapper
                                    key={analytic.id}
                                    id={analytic.id}
                                    isReordering={isConfiguring}
                                    index={index}
                                    color={color}
                                >
                                    {renderCard(analytic)}
                                </SortableCardWrapper>
                            ))}
                        </Masonry>
                    </ResponsiveMasonry>
                </SortableContext>
            </DndContext>
        </Stack>
    );
}
