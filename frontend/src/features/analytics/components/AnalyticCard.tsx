import React from "react";
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
import { ScatterChartCard } from "./ScatterChartCard";
import { SingleValueCard } from "./SingleValueCard";

export const SingleValueCardMemo = React.memo(SingleValueCard);
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
    onRename?: (analyticId: string) => void;
    onEntryClick?: (entryId: string) => void;
}

/**
 * The card a result type is drawn with. Shared by the masonry on a tracker page and by
 * the dashboard grid, which differ only in whether the card sizes itself or is sized by
 * the cell it was dropped into.
 */
export function AnalyticCard({
    analytic,
    color,
    isConfiguring,
    fillHeight,
    onRemove,
    onRename,
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
                    onRename={onRename}
                    onEntryClick={onEntryClick}
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
                    onRename={onRename}
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
                    onRename={onRename}
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
                    onRename={onRename}
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
                    onRename={onRename}
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
                    onRename={onRename}
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
                    onRename={onRename}
                />
            );
        default:
            return null;
    }
}
