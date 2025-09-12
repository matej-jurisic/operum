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
    const timeoutRef = useRef<number | null>(null);

    const setLoadingWithDelay = (value: boolean) => {
        if (timeoutRef.current) {
            clearTimeout(timeoutRef.current);
        }

        if (value) {
            // delay before showing loader
            timeoutRef.current = window.setTimeout(() => {
                setLoading(true);
            }, 100); // show after 200ms
        } else {
            // debounce before hiding loader
            timeoutRef.current = window.setTimeout(() => {
                setLoading(false);
            }, 150); // hide after 150ms
        }
    };

    useEffect(() => {
        registerLoadingSetter(setLoadingWithDelay);
        return () => {
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }
        };
    }, []);

    return (
        <LoadingContext.Provider
            value={{ loading, setLoading: setLoadingWithDelay }}
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
