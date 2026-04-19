const operatorSymbols: Record<string, string> = {
    "Equals": "=",
    "Not Equals": "≠",
    "Greater Than": ">",
    "Less Than": "<",
    "Greater Than Or Equal": "≥",
    "Less Than Or Equal": "≤",
};

export const formatOperator = (operator: string): string =>
    operatorSymbols[operator] ?? operator;
