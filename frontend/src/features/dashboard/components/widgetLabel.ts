import { IconType } from "react-icons";
import { MdOutlineHorizontalRule } from "react-icons/md";
import {
    TbAdjustmentsHorizontal,
    TbChartHistogram,
    TbHeading,
    TbNote,
    TbPlus,
    TbTable,
} from "react-icons/tb";
import { DashboardWidgetDto, WidgetTypes } from "../types/DashboardDto";

/** A short, human-readable identifier for a widget, for lists where its own card isn't
    shown (the group picker, a group's member menu). */
export function widgetLabel(widget: DashboardWidgetDto): string {
    switch (widget.type) {
        case WidgetTypes.Analytic:
            return widget.analytic?.name || "Untitled chart";
        case WidgetTypes.Entries:
            return widget.entriesWidget?.trackerName ?? "Entries";
        case WidgetTypes.Header:
            return "Header";
        case WidgetTypes.Divider:
            return "Divider";
        case WidgetTypes.Note:
            return "Note";
        case WidgetTypes.Filter:
            return "Filter";
        case WidgetTypes.QuickAdd:
            return "Quick-add button";
        default:
            return "Widget";
    }
}

export function widgetIcon(widget: DashboardWidgetDto): IconType {
    switch (widget.type) {
        case WidgetTypes.Entries:
            return TbTable;
        case WidgetTypes.Header:
            return TbHeading;
        case WidgetTypes.Divider:
            return MdOutlineHorizontalRule;
        case WidgetTypes.Note:
            return TbNote;
        case WidgetTypes.Filter:
            return TbAdjustmentsHorizontal;
        case WidgetTypes.QuickAdd:
            return TbPlus;
        default:
            return TbChartHistogram;
    }
}
