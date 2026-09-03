import { CodeDto } from "../types/AnalyticConfigDto";

/** Mirrors AnalyticPurposes on the backend. Only the purposes the frontend reasons about
    by name are listed. */
export enum AnalyticPurposeEnum {
    Match = "Match",
}

/** True for a calculation that pairs two trackers on a shared match field (the scatter
    chart's Correlation): it reads from two sources, one per axis, rather than one. The
    Match purpose is what distinguishes it from an ordinary single-tracker code. */
export const codeSpansTrackers = (code: CodeDto): boolean =>
    code.purposes.some((p) => p.name === AnalyticPurposeEnum.Match);
