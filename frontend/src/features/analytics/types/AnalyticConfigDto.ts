export interface PurposeDto {
    name: string;
    allowedDataTypes: string[];
}

export interface CodeDto {
    code: string;
    name: string;
    purposes: PurposeDto[];
}

export interface ResultTypeDto {
    name: string;
    codes: CodeDto[];
}

export interface AnalyticConfigDto {
    resultTypes: ResultTypeDto[];
}
