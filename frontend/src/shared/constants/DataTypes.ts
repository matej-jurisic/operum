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

// Data types that filter and sort identically, so a clause authored for one may run against
// a field of the other. Today that's only date/datetime -- both are stored and compared as a
// point in time; "date" just hides the clock.
const INTERCHANGEABLE_TYPE_GROUPS: string[][] = [[FieldTypes.Date, FieldTypes.DateTime]];

export const fieldTypesCompatible = (a: string, b: string): boolean =>
    a === b ||
    INTERCHANGEABLE_TYPE_GROUPS.some(
        (group) => group.includes(a) && group.includes(b),
    );

export const DataTypeColor: Record<FieldType, string> = {
    string: "red",
    number: "blue",
    bool: "green",
    date: "orange",
    datetime: "yellow",
    timespan: "yellow",
};
