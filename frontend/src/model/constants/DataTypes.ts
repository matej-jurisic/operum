export const OperatorTypes = {
    Equals: "Equals",
    NotEquals: "NotEquals",
    GreaterThan: "GreaterThan",
    GreaterThanOrEqual: "GreaterThanOrEqual",
    LessThan: "LessThan",
    LessThanOrEqual: "LessThanOrEqual",
    Contains: "Contains",
    StartsWith: "StartsWith",
    EndsWith: "EndsWith",
    IsEmpty: "IsEmpty",
    IsNotEmpty: "IsNotEmpty",
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
export type FieldType = keyof typeof FieldTypes;

export const AllowedOperatorsForFieldType: Record<FieldType, OperatorType[]> = {
    String: [
        "Equals",
        "NotEquals",
        "Contains",
        "StartsWith",
        "EndsWith",
        "IsEmpty",
        "IsNotEmpty",
    ],
    Number: [
        "Equals",
        "NotEquals",
        "GreaterThan",
        "GreaterThanOrEqual",
        "LessThan",
        "LessThanOrEqual",
    ],
    Bool: ["Equals", "NotEquals"],
    Date: [
        "Equals",
        "NotEquals",
        "GreaterThan",
        "GreaterThanOrEqual",
        "LessThan",
        "LessThanOrEqual",
    ],
    DateTime: [
        "Equals",
        "NotEquals",
        "GreaterThan",
        "GreaterThanOrEqual",
        "LessThan",
        "LessThanOrEqual",
    ],
    TimeSpan: [
        "Equals",
        "NotEquals",
        "GreaterThan",
        "GreaterThanOrEqual",
        "LessThan",
        "LessThanOrEqual",
    ],
};

export function isValidOperatorForFieldType(
    operator: OperatorType,
    fieldType: FieldType
): boolean {
    return AllowedOperatorsForFieldType[fieldType].includes(operator);
}
