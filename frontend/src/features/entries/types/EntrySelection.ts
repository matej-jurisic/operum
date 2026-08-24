/**
 * The set of entries a bulk action should run on: either the listed ids, or everything the
 * given views match minus the exclusions, which is how selecting past the current page works.
 */
export type EntrySelection = {
    entryIds: string[];
    selectAllMatching: boolean;
    viewIds: string[];
    excludedEntryIds: string[];
};
