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
    description: string;
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
    description: string,
    startAnchor: DateAnchor,
    endAnchor: DateAnchor,
    offset: number,
): FilterTemplate {
    return {
        id,
        name,
        description,
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
    description: string,
    prefix: LookbackPrefix,
    n: number,
): FilterTemplate {
    return {
        id,
        name,
        description,
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

export const filterTemplates: FilterTemplate[] = [
    {
        id: "today",
        name: "Today",
        description: "Show entries from today",
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
        description: "Show entries from yesterday",
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
        "Show entries from the past week",
        LookbackPrefixes.LastNDays,
        7,
    ),
    lookbackTemplate(
        "last_30_days",
        "Last 30 Days",
        "Show entries from the past 30 days",
        LookbackPrefixes.LastNDays,
        30,
    ),
    periodTemplate(
        "current_week",
        "Current Week",
        "Show entries from the current week",
        DateAnchors.StartOfWeek,
        DateAnchors.EndOfWeek,
        0,
    ),
    periodTemplate(
        "last_week",
        "Last Week",
        "Show entries from the previous week only",
        DateAnchors.StartOfWeek,
        DateAnchors.EndOfWeek,
        -1,
    ),
    periodTemplate(
        "current_month",
        "Current Month",
        "Show entries from the current month",
        DateAnchors.StartOfMonth,
        DateAnchors.EndOfMonth,
        0,
    ),
    periodTemplate(
        "last_month",
        "Last Month",
        "Show entries from the previous month only",
        DateAnchors.StartOfMonth,
        DateAnchors.EndOfMonth,
        -1,
    ),
    periodTemplate(
        "current_year",
        "Current Year",
        "Show entries from the current year",
        DateAnchors.StartOfYear,
        DateAnchors.EndOfYear,
        0,
    ),
    periodTemplate(
        "last_year",
        "Last Year",
        "Show entries from the previous year only",
        DateAnchors.StartOfYear,
        DateAnchors.EndOfYear,
        -1,
    ),
    {
        id: "positive_values",
        name: "Positive Values",
        description: "Show entries with values above zero",
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
        description: "Show entries with values below zero",
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
        description: "Show entries where value is true",
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
        description: "Show entries where value is false",
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
        description: "Show entries that are not empty",
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
        description: "Show entries that are empty",
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
