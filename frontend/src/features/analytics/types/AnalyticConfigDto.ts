export interface PurposeDto {
    name: string;
    allowedDataTypes: string[];
}

export interface CodeDto {
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
