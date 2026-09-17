const flag = import.meta.env.VITE_REACT_INTEGRATIONS_ENABLED;

/** Backend has its own matching flag (Features__Integrations) and 404s without it. */
export const areIntegrationsEnabled = flag?.trim().toLowerCase() === "true";
