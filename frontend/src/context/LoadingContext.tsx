import React, { createContext, useContext, useEffect, useState } from "react";

type LoadingContextType = {
    loading: boolean;
    setLoading: (value: boolean) => void;
};

const LoadingContext = createContext<LoadingContextType | undefined>(undefined);

export const LoadingProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        registerLoadingSetter(setLoading);
    }, []);

    return (
        <LoadingContext.Provider value={{ loading, setLoading }}>
            {children}
        </LoadingContext.Provider>
    );
};

export const useLoading = () => {
    const context = useContext(LoadingContext);
    if (!context) {
        throw new Error("useLoading must be used within a LoadingProvider");
    }
    return context;
};

let setLoadingExternal: (value: boolean) => void;

export const registerLoadingSetter = (setter: (value: boolean) => void) => {
    setLoadingExternal = setter;
};

export const setGlobalLoading = (value: boolean) => {
    if (setLoadingExternal) {
        setLoadingExternal(value);
    }
};
