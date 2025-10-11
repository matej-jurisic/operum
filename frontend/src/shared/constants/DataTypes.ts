export const OperatorTypes = {
    Equals: "Equals",
    NotEquals: "Not Equals",
    GreaterThan: "Greater Than",
    GreaterThanOrEqual: "Greater Than Or Equal",
    LessThan: "Less Than",
    LessThanOrEqual: "Less Than Or Equal",
    Contains: "Contains",
    StartsWith: "Starts With",
    EndsWith: "Ends With",
} as const;
export type OperatorType = keyof typeof OperatorTypes;

export const FieldTypes = {
    String: "string",
    Number: "number",
    Bool: "bool",
    Date: "date",
    DateTime: "datetime",
    TimeSpan: "timespan",
} as const;
export type FieldType = (typeof FieldTypes)[keyof typeof FieldTypes];

export const DataTypeColor: Record<FieldType, string> = {
    string: "red",
    number: "blue",
    bool: "green",
    date: "orange",
    datetime: "yellow",
    timespan: "yellow",
};
