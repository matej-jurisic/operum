import { FiCalendar, FiClock, FiPlus, FiTrendingUp } from "react-icons/fi";
import { FieldTypes, OperatorTypes } from "./DataTypes";

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
        valueGenerator?: () => any; // Function to generate dynamic values
    }>;
}

export const filterTemplates: FilterTemplate[] = [
    {
        id: "current_month",
        name: "Current Month",
        description: "Show entries from the current month",
        icon: <FiCalendar size={16} />,
        fieldTypes: [FieldTypes.Date, FieldTypes.DateTime],
        filters: [
            {
                operator: OperatorTypes.GreaterThanOrEqual,
                valueGenerator: () => {
                    const now = new Date();
                    return new Date(now.getFullYear(), now.getMonth(), 1);
                },
            },
            {
                operator: OperatorTypes.LessThanOrEqual,
                valueGenerator: () => {
                    const now = new Date();
                    return new Date(now.getFullYear(), now.getMonth() + 1, 0);
                },
            },
        ],
    },
    {
        id: "last_7_days",
        name: "Last 7 Days",
        description: "Show entries from the past week",
        icon: <FiClock size={16} />,
        fieldTypes: [FieldTypes.Date, FieldTypes.DateTime],
        filters: [
            {
                operator: OperatorTypes.GreaterThanOrEqual,
                valueGenerator: () => {
                    const date = new Date();
                    date.setDate(date.getDate() - 7);
                    return date;
                },
            },
        ],
    },
    {
        id: "last_30_days",
        name: "Last 30 Days",
        description: "Show entries from the past month",
        icon: <FiClock size={16} />,
        fieldTypes: [FieldTypes.Date, FieldTypes.DateTime],
        filters: [
            {
                operator: OperatorTypes.GreaterThanOrEqual,
                valueGenerator: () => {
                    const date = new Date();
                    date.setDate(date.getDate() - 30);
                    return date;
                },
            },
        ],
    },
    {
        id: "today",
        name: "Today",
        description: "Show entries from today",
        icon: <FiCalendar size={16} />,
        fieldTypes: [FieldTypes.Date, FieldTypes.DateTime],
        filters: [
            {
                operator: OperatorTypes.Equals,
                valueGenerator: () => {
                    const today = new Date();
                    today.setHours(0, 0, 0, 0);
                    return today;
                },
            },
        ],
    },
    {
        id: "high_values",
        name: "High Values",
        description: "Show entries with values above threshold",
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
        id: "positive_values",
        name: "Positive Values",
        description: "Show entries with positive values",
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
        description: "Show entries with negative values",
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
