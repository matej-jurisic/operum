export const QueryKinds = {
    Filter: "filter",
    Sort: "sort",
} as const;

export type QueryKind = (typeof QueryKinds)[keyof typeof QueryKinds];

export const QueryKindColor: Record<QueryKind, string> = {
    filter: "blue",
    sort: "teal",
};

export const QueryKindLabel: Record<QueryKind, string> = {
    filter: "Filter",
    sort: "Sort",
};
