const isApple =
    typeof navigator !== "undefined" &&
    /Mac|iPhone|iPad/.test(navigator.platform);

/** The primary modifier key as the user's keyboard labels it. */
export const MOD_KEY = isApple ? "⌘" : "Ctrl";
