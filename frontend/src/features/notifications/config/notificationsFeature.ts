const flag = import.meta.env.VITE_REACT_NOTIFICATIONS_ENABLED;

/**
 * Notifications are still in development: unless the build explicitly opts in, the tab,
 * its route and the push subscription flow are hidden. The backend has its own flag
 * (Features__Notifications), so nothing has to be asked of it at runtime.
 */
export const areNotificationsEnabled = flag?.trim().toLowerCase() === "true";
