import { FieldTypes, OperatorTypes } from "./DataTypes";

export const calculatedFieldTypes = [
    { value: "number", label: "Number" },
    { value: "bool", label: "Bool" },
    { value: "timespan", label: "Timespan" },
];

export const fieldTypes = [
    { value: "string", label: "String" },
    { value: "number", label: "Number" },
    { value: "bool", label: "Bool" },
    { value: "date", label: "Date" },
    { value: "timespan", label: "Timespan" },
    { value: "datetime", label: "Datetime" },
];

export const operatorTypes = [
    { value: "Equals", label: "Equals" },
    { value: "Not Equals", label: "Not Equals" },
    { value: "Greater Than", label: "Greater Than" },
    { value: "Greater Than Or Equal", label: "Greater or Equal" },
    { value: "Less Than", label: "Less Than" },
    { value: "Less Than Or Equal", label: "Less or Equal" },
    { value: "Contains", label: "Contains" },
    { value: "Starts With", label: "Starts With" },
    { value: "Ends With", label: "Ends With" },
];

const TEXT_OPERATORS = [
    OperatorTypes.Equals,
    OperatorTypes.NotEquals,
    OperatorTypes.Contains,
    OperatorTypes.StartsWith,
    OperatorTypes.EndsWith,
];

const ORDERED_OPERATORS = [
    OperatorTypes.Equals,
    OperatorTypes.NotEquals,
    OperatorTypes.GreaterThan,
    OperatorTypes.GreaterThanOrEqual,
    OperatorTypes.LessThan,
    OperatorTypes.LessThanOrEqual,
];

const EQUALITY_OPERATORS = [OperatorTypes.Equals, OperatorTypes.NotEquals];

/**
 * Which operators a field of this type accepts, mirroring the server's own rule:
 * text operators are string-only and ordering operators are rejected on strings.
 */
export const operatorsForFieldType = (type: string | undefined) => {
    const allowed =
        type === FieldTypes.String
            ? TEXT_OPERATORS
            : type === FieldTypes.Bool
              ? EQUALITY_OPERATORS
              : type === undefined
                ? null
                : ORDERED_OPERATORS;

    if (allowed === null) return operatorTypes;
    return operatorTypes.filter((o) => (allowed as string[]).includes(o.value));
};
