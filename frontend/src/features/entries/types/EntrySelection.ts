/** Either explicit entryIds, or everything the view matches minus excludedEntryIds. */
export type EntrySelection = {
    entryIds: string[];
    selectAllMatching: boolean;
    viewId: string | null;
    excludedEntryIds: string[];
};
