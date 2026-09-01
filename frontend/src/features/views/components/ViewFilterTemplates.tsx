import { CiClock2 } from "react-icons/ci";
import { FiCalendar, FiPlus, FiTrendingUp } from "react-icons/fi";
import { FieldTypes, OperatorTypes } from "../../../shared/constants/DataTypes";
import {
    DateAnchor,
    DateAnchors,
    LookbackPrefix,
    LookbackPrefixes,
    serializeAnchorToken,
    serializeLookbackToken,
} from "../../../shared/constants/dynamicDateTokens";

// Filter template definitions
export interface FilterTemplate {
    id: string;
    name: string;
    icon: React.ReactNode;
    fieldTypes: string[]; // Field types this template works with
    filters: Array<{
        operator: string;
        value?: any;
    }>;
}

const DATE_FIELD_TYPES = [FieldTypes.Date, FieldTypes.DateTime];

/**
 * Date templates emit dynamic date tokens rather than concrete dates: a template is applied once
 * but the view it builds is read forever, so "Current Month" has to mean whichever month it is
 * when the view runs, not the month it was created in.
 *
 * A bounded period takes two filters, a lower and an upper bound, which the caller ANDs together.
 */
function periodTemplate(
    id: string,
    name: string,
    startAnchor: DateAnchor,
    endAnchor: DateAnchor,
    offset: number,
): FilterTemplate {
    return {
        id,
        name,
        icon: <FiCalendar size={16} />,
        fieldTypes: DATE_FIELD_TYPES,
        filters: [
            {
                operator: OperatorTypes.GreaterThanOrEqual,
                value: serializeAnchorToken(startAnchor, offset),
            },
            {
                operator: OperatorTypes.LessThanOrEqual,
                value: serializeAnchorToken(endAnchor, offset),
            },
        ],
    };
}

function lookbackTemplate(
    id: string,
    name: string,
    prefix: LookbackPrefix,
    n: number,
): FilterTemplate {
    return {
        id,
        name,
        icon: <CiClock2 size={16} />,
        fieldTypes: DATE_FIELD_TYPES,
        filters: [
            {
                operator: OperatorTypes.GreaterThanOrEqual,
                value: serializeLookbackToken(prefix, n),
            },
        ],
    };
}

/** A lower + upper bound with no values filled in — the caller (or the board, for a
 *  parameter widget) supplies the ends. Lets someone build a customizable range without
 *  starting from a preset period like "Current Month". */
function blankRangeTemplate(
    id: string,
    name: string,
    fieldTypes: string[],
    icon: React.ReactNode,
): FilterTemplate {
    return {
        id,
        name,
        icon,
        fieldTypes,
        filters: [
            { operator: OperatorTypes.GreaterThanOrEqual },
            { operator: OperatorTypes.LessThanOrEqual },
        ],
    };
}

export const filterTemplates: FilterTemplate[] = [
    blankRangeTemplate(
        "date_range",
        "Date range",
        DATE_FIELD_TYPES,
        <FiCalendar size={16} />,
    ),
    {
        id: "today",
        name: "Today",
        icon: <FiCalendar size={16} />,
        fieldTypes: DATE_FIELD_TYPES,
        filters: [
            {
                operator: OperatorTypes.Equals,
                value: DateAnchors.Today,
            },
        ],
    },
    {
        id: "yesterday",
        name: "Yesterday",
        icon: <FiCalendar size={16} />,
        fieldTypes: DATE_FIELD_TYPES,
        filters: [
            {
                operator: OperatorTypes.Equals,
                value: serializeAnchorToken(DateAnchors.Today, -1),
            },
        ],
    },
    lookbackTemplate(
        "last_7_days",
        "Last 7 Days",
        LookbackPrefixes.LastNDays,
        7,
    ),
    lookbackTemplate(
        "last_30_days",
        "Last 30 Days",
        LookbackPrefixes.LastNDays,
        30,
    ),
    periodTemplate(
        "current_week",
        "Current Week",
        DateAnchors.StartOfWeek,
        DateAnchors.EndOfWeek,
        0,
    ),
    periodTemplate(
        "last_week",
        "Last Week",
        DateAnchors.StartOfWeek,
        DateAnchors.EndOfWeek,
        -1,
    ),
    periodTemplate(
        "current_month",
        "Current Month",
        DateAnchors.StartOfMonth,
        DateAnchors.EndOfMonth,
        0,
    ),
    periodTemplate(
        "last_month",
        "Last Month",
        DateAnchors.StartOfMonth,
        DateAnchors.EndOfMonth,
        -1,
    ),
    periodTemplate(
        "current_year",
        "Current Year",
        DateAnchors.StartOfYear,
        DateAnchors.EndOfYear,
        0,
    ),
    periodTemplate(
        "last_year",
        "Last Year",
        DateAnchors.StartOfYear,
        DateAnchors.EndOfYear,
        -1,
    ),
    blankRangeTemplate(
        "number_range",
        "Number range",
        [FieldTypes.Number],
        <FiTrendingUp size={16} />,
    ),
    {
        id: "positive_values",
        name: "Positive Values",
        icon: <FiTrendingUp size={16} />,
        fieldTypes: [FieldTypes.Number],
        filters: [
            {
                operator: OperatorTypes.GreaterThan,
                value: 0,
            },
        ],
    },
    {
        id: "negative_values",
        name: "Negative Values",
        icon: <FiTrendingUp size={16} />,
        fieldTypes: [FieldTypes.Number],
        filters: [
            {
                operator: OperatorTypes.LessThan,
                value: 0,
            },
        ],
    },
    {
        id: "is_true",
        name: "Is True",
        icon: <FiPlus size={16} />,
        fieldTypes: [FieldTypes.Bool],
        filters: [
            {
                operator: OperatorTypes.Equals,
                value: true,
            },
        ],
    },
    {
        id: "is_false",
        name: "Is False",
        icon: <FiPlus size={16} />,
        fieldTypes: [FieldTypes.Bool],
        filters: [
            {
                operator: OperatorTypes.Equals,
                value: false,
            },
        ],
    },
    {
        id: "has_value",
        name: "Has Value",
        icon: <FiPlus size={16} />,
        fieldTypes: [
            FieldTypes.String,
            FieldTypes.Number,
            FieldTypes.Date,
            FieldTypes.DateTime,
            FieldTypes.TimeSpan,
        ],
        filters: [
            {
                operator: OperatorTypes.NotEquals,
            },
        ],
    },
    {
        id: "is_empty",
        name: "Is Empty",
        icon: <FiPlus size={16} />,
        fieldTypes: [
            FieldTypes.String,
            FieldTypes.Number,
            FieldTypes.Date,
            FieldTypes.DateTime,
            FieldTypes.TimeSpan,
        ],
        filters: [
            {
                operator: OperatorTypes.Equals,
            },
        ],
    },
];
