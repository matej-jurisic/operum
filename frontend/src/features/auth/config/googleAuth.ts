const clientId = import.meta.env.VITE_REACT_GOOGLE_CLIENT;

/**
 * Google sign-in is optional: when no OAuth client id was supplied at build time
 * the feature is hidden from the UI entirely (the backend rejects it as well).
 */
export const isGoogleAuthEnabled = Boolean(clientId?.trim());

export const googleClientId = clientId ?? "";
