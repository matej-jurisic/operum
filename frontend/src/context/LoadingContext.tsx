import React, {
    createContext,
    useContext,
    useEffect,
    useRef,
    useState,
} from "react";

type LoadingContextType = {
    loading: boolean;
    setLoading: (value: boolean) => void;
};

const LoadingContext = createContext<LoadingContextType | undefined>(undefined);

export const LoadingProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const [loading, setLoading] = useState(false);
    const timeoutRef = useRef<number>(null);

    const setLoadingWithDebounce = (value: boolean) => {
        if (timeoutRef.current) {
            clearTimeout(timeoutRef.current);
        }

        if (value) {
            setLoading(true);
        } else {
            // Delay hiding loading by 150ms to prevent flicker
            timeoutRef.current = setTimeout(() => {
                setLoading(false);
            }, 150);
        }
    };

    useEffect(() => {
        registerLoadingSetter(setLoadingWithDebounce);
    }, []);

    return (
        <LoadingContext.Provider
            value={{ loading, setLoading: setLoadingWithDebounce }}
        >
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
