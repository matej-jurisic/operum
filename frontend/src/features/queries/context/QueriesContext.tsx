import React, { createContext, useCallback, useContext, useState } from "react";
import { useTracker } from "../../trackers/context/TrackerContext";
import { queriesController } from "../api/queriesController";
import { QueryDto } from "../types/QueryDto";
import { CreateQueryDto } from "../types/requests/CreateQueryDto";
import { UpdateQueryDto } from "../types/requests/UpdateQueryDto";

type QueriesContextType = {
    queries: QueryDto[];
    refreshQueries: () => Promise<void>;
    refreshQueriesIfDirty: () => Promise<void>;
    markQueriesDirty: () => void;
    // API methods - internal use only
    _createQuery: (query: CreateQueryDto) => Promise<QueryDto>;
    _updateQuery: (queryId: string, query: UpdateQueryDto) => Promise<QueryDto>;
    _deleteQuery: (queryId: string) => Promise<void>;
};

const QueriesContext = createContext<QueriesContextType | undefined>(undefined);

export const QueriesProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const { tracker } = useTracker();
    const [queries, setQueries] = useState<QueryDto[]>([]);
    const [queriesDirty, setQueriesDirty] = useState(true);

    const refreshQueries = useCallback(async () => {
        const response = await queriesController.getQueryList(tracker.id);
        setQueries(response.data);
        setQueriesDirty(false);
    }, [tracker.id]);

    const refreshQueriesIfDirty = useCallback(async () => {
        if (queriesDirty) await refreshQueries();
    }, [queriesDirty, refreshQueries]);

    const markQueriesDirty = useCallback(() => setQueriesDirty(true), []);

    const _createQuery = async (queryData: CreateQueryDto) => {
        const response = await queriesController.createQuery(
            tracker.id,
            queryData,
        );
        await refreshQueries();
        return response.data;
    };

    const _updateQuery = async (queryId: string, queryData: UpdateQueryDto) => {
        const response = await queriesController.updateQuery(
            tracker.id,
            queryId,
            queryData,
        );
        await refreshQueries();
        return response.data;
    };

    const _deleteQuery = async (queryId: string) => {
        await queriesController.deleteQuery(tracker.id, queryId);
        await refreshQueries();
    };

    return (
        <QueriesContext.Provider
            value={{
                queries,
                refreshQueries,
                refreshQueriesIfDirty,
                markQueriesDirty,
                _createQuery,
                _updateQuery,
                _deleteQuery,
            }}
        >
            {children}
        </QueriesContext.Provider>
    );
};

export const useQueries = () => {
    const ctx = useContext(QueriesContext);
    if (!ctx) throw new Error("useQueries must be used within QueriesProvider");
    return ctx;
};
