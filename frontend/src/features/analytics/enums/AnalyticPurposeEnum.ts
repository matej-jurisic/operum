import { CodeDto, PurposeDto } from "../types/AnalyticConfigDto";

/** Mirrors AnalyticPurposes on the backend. Only the purposes the frontend reasons about
    by name are listed. */
export enum AnalyticPurposeEnum {
    Match = "Match",
    Display = "Display",
}

/** What an optional purpose's field select says under its label. */
export const purposeHint = (purpose: PurposeDto): string | undefined =>
    purpose.name === AnalyticPurposeEnum.Display
        ? "Shows this field's value from the entry holding the minimum or maximum, instead of the number."
        : undefined;

/** True for a two-tracker calculation (e.g. the scatter chart's Correlation), identified by the Match purpose. */
export const codeSpansTrackers = (code: CodeDto): boolean =>
    code.purposes.some((p) => p.name === AnalyticPurposeEnum.Match);
