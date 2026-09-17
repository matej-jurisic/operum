const flag = import.meta.env.VITE_REACT_NOTIFICATIONS_ENABLED;

/** Backend has its own matching flag (Features__Notifications). */
export const areNotificationsEnabled = flag?.trim().toLowerCase() === "true";
