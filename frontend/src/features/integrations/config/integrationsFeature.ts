const flag = import.meta.env.VITE_REACT_INTEGRATIONS_ENABLED;

/**
 * Integrations are still in development: unless the build explicitly opts in, the page and
 * its route are hidden. The backend has its own flag (Features__Integrations) and answers
 * 404 without it, so nothing has to be asked of it at runtime.
 */
export const areIntegrationsEnabled = flag?.trim().toLowerCase() === "true";
