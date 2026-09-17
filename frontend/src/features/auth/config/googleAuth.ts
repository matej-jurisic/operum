const clientId = import.meta.env.VITE_REACT_GOOGLE_CLIENT;

/** Hidden entirely when no OAuth client id was supplied at build time; backend rejects it too. */
export const isGoogleAuthEnabled = Boolean(clientId?.trim());

export const googleClientId = clientId ?? "";
