import { CiSearch } from "react-icons/ci";
import {
    FiChevronsLeft,
    FiChevronsRight,
    FiMoreHorizontal,
    FiSlash,
    FiTarget,
    FiType,
} from "react-icons/fi";
import { FieldTypes, OperatorTypes } from "../../../shared/constants/DataTypes";

/**
 * Clause templates for a *filter widget* — deliberately separate from a view's
 * {@link filterTemplates}. A filter widget's clauses only ever collect a data type and an
 * operator; their values are typed on the board, never baked in here. So a view template
 * like "Current Month" or "Positive Values" — which is mostly a canned *value* — carries
 * nothing useful once its value is stripped.
 *
 * A template here is purely a shortcut for a common operator shape: a two-ended range, a
 * single bound, a text search.
 */
export interface FilterWidgetClauseTemplate {
    id: string;
    name: string;
    /** One short line shown under the name in the picker. */
    description: string;
    icon: React.ReactNode;
    /** Data types the shape makes sense for; the picker's type list offers their union. */
    fieldTypes: string[];
    /** One entry per clause the template adds, in order. */
    clauses: Array<{ operator: string }>;
}

const DATE_TYPES = [FieldTypes.Date, FieldTypes.DateTime];
/** Types with a meaningful ordering — everything a range or a bound applies to. */
const ORDERED_TYPES = [...DATE_TYPES, FieldTypes.Number, FieldTypes.TimeSpan];
const ALL_TYPES = [
    FieldTypes.String,
    FieldTypes.Number,
    FieldTypes.Bool,
    ...DATE_TYPES,
    FieldTypes.TimeSpan,
];

export const filterWidgetClauseTemplates: FilterWidgetClauseTemplate[] = [
    {
        id: "range",
        name: "Range",
        description: "Two board inputs: a lower and an upper bound, matched together.",
        icon: <FiMoreHorizontal size={16} />,
        fieldTypes: ORDERED_TYPES,
        clauses: [
            { operator: OperatorTypes.GreaterThanOrEqual },
            { operator: OperatorTypes.LessThanOrEqual },
        ],
    },
    {
        id: "minimum",
        name: "Minimum",
        description: "One board input that keeps rows at or above the value.",
        icon: <FiChevronsRight size={16} />,
        fieldTypes: ORDERED_TYPES,
        clauses: [{ operator: OperatorTypes.GreaterThanOrEqual }],
    },
    {
        id: "maximum",
        name: "Maximum",
        description: "One board input that keeps rows at or below the value.",
        icon: <FiChevronsLeft size={16} />,
        fieldTypes: ORDERED_TYPES,
        clauses: [{ operator: OperatorTypes.LessThanOrEqual }],
    },
    {
        id: "exact",
        name: "Exact match",
        description: "One board input that keeps rows whose field equals the value.",
        icon: <FiTarget size={16} />,
        fieldTypes: ALL_TYPES,
        clauses: [{ operator: OperatorTypes.Equals }],
    },
    {
        id: "exclude",
        name: "Exclude",
        description: "One board input that drops rows whose field equals the value.",
        icon: <FiSlash size={16} />,
        fieldTypes: ALL_TYPES,
        clauses: [{ operator: OperatorTypes.NotEquals }],
    },
    {
        id: "text_search",
        name: "Text search",
        description: "One board input that keeps rows whose text contains it.",
        icon: <CiSearch size={16} />,
        fieldTypes: [FieldTypes.String],
        clauses: [{ operator: OperatorTypes.Contains }],
    },
    {
        id: "starts_with",
        name: "Starts with",
        description: "One board input that keeps rows whose text starts with it.",
        icon: <FiType size={16} />,
        fieldTypes: [FieldTypes.String],
        clauses: [{ operator: OperatorTypes.StartsWith }],
    },
];
