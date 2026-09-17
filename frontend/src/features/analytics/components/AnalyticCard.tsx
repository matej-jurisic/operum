import React from "react";
import { AnalyticResultTypeEnum } from "../enums/AnalyticResultTypeEnum";
import {
    AnalyticDto,
    BarChartAnalyticDto,
    CalendarAnalyticDto,
    ComposedChartAnalyticDto,
    DonutChartAnaylticDto,
    GoalAnalyticDto,
    LineChartAnalyticDto,
    ScatterChartAnalyticDto,
    SingleValueAnalyticDto,
} from "../types/AnalyticDto";
import { BarChartCard } from "./BarChartCard";
import { CalendarCard } from "./CalendarCard";
import { ComposedChartCard } from "./ComposedChartCard";
import { DonutChartCard } from "./DonutChartCard";
import { GoalCard } from "./GoalCard";
import { LineChartCard } from "./LineChartCard";
import { ScatterChartCard } from "./ScatterChartCard";
import { SingleValueCard } from "./SingleValueCard";

export const SingleValueCardMemo = React.memo(SingleValueCard);
export const GoalCardMemo = React.memo(GoalCard);
export const LineChartCardMemo = React.memo(LineChartCard);
export const ScatterChartCardMemo = React.memo(ScatterChartCard);
export const CalendarChartCardMemo = React.memo(CalendarCard);
export const DonutChartCardMemo = React.memo(DonutChartCard);
export const BarChartCardMemo = React.memo(BarChartCard);
export const ComposedChartCardMemo = React.memo(ComposedChartCard);

interface AnalyticCardProps {
    analytic: AnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    /** Stretch to fill the container instead of rendering at a fixed height. */
    fillHeight?: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    onEntryClick?: (entryId: string) => void;
}

/** Shared by the tracker masonry and the dashboard grid. */
export function AnalyticCard({
    analytic,
    color,
    isConfiguring,
    fillHeight,
    onRemove,
    onEdit,
    onEntryClick,
}: AnalyticCardProps) {
    switch (analytic.resultType) {
        case AnalyticResultTypeEnum.SingleValue:
            return (
                <SingleValueCardMemo
                    analytic={analytic as SingleValueAnalyticDto}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight={fillHeight}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    onEntryClick={onEntryClick}
                />
            );
        case AnalyticResultTypeEnum.Goal:
            return (
                <GoalCardMemo
                    analytic={analytic as GoalAnalyticDto}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight={fillHeight}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        case AnalyticResultTypeEnum.LineChart:
            return (
                <LineChartCardMemo
                    analytic={analytic as LineChartAnalyticDto}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight={fillHeight}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        case AnalyticResultTypeEnum.ScatterChart:
            return (
                <ScatterChartCardMemo
                    analytic={analytic as ScatterChartAnalyticDto}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight={fillHeight}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        case AnalyticResultTypeEnum.Calendar:
            return (
                <CalendarChartCardMemo
                    analytic={analytic as CalendarAnalyticDto}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight={fillHeight}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    onEntryClick={onEntryClick}
                />
            );
        case AnalyticResultTypeEnum.Donut:
            return (
                <DonutChartCardMemo
                    analytic={analytic as DonutChartAnaylticDto}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight={fillHeight}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        case AnalyticResultTypeEnum.BarChart:
            return (
                <BarChartCardMemo
                    analytic={analytic as BarChartAnalyticDto}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight={fillHeight}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        case AnalyticResultTypeEnum.Composed:
            return (
                <ComposedChartCardMemo
                    analytic={analytic as ComposedChartAnalyticDto}
                    color={color}
                    isConfiguring={isConfiguring}
                    fillHeight={fillHeight}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />
            );
        default:
            return null;
    }
}
