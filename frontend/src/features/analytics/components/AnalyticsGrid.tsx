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
import { AnalyticDto } from "../types/AnalyticDto";
import { AnalyticCard } from "./AnalyticCard";
import { closestToPointer } from "./MasonryCollision";

interface AnalyticsGridProps {
    analytics: AnalyticDto[];
    color: string | undefined;
    isConfiguring: boolean;
    onReorder: (orderedIds: string[]) => void;
    onRemove?: (analyticId: string) => void;
    onRename?: (analyticId: string) => void;
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
    onRename,
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
                                    <AnalyticCard
                                        analytic={analytic}
                                        color={color}
                                        isConfiguring={isConfiguring}
                                        onRemove={onRemove}
                                        onRename={onRename}
                                        onEntryClick={onEntryClick}
                                    />
                                </SortableCardWrapper>
                            ))}
                        </Masonry>
                    </ResponsiveMasonry>
                </SortableContext>
            </DndContext>
        </Stack>
    );
}
