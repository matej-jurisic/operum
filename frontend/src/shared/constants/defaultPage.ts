/** Mirrors server-side User.DefaultPage so the catch-all route can resolve synchronously before login/`me` responds. */
export const DEFAULT_PAGE_KEY = "operum.defaultPage";

/** Where a bare "/" (or any unknown route) sends a signed-in user. */
export const FALLBACK_PAGE = "/dashboard";

export function readDefaultPage(): string {
    const stored = localStorage.getItem(DEFAULT_PAGE_KEY);
    if (stored && (stored.startsWith("/dashboard") || stored.startsWith("/trackers"))) {
        return stored;
    }
    return FALLBACK_PAGE;
}

export function writeDefaultPage(page: string | null | undefined) {
    if (page) localStorage.setItem(DEFAULT_PAGE_KEY, page);
    else localStorage.removeItem(DEFAULT_PAGE_KEY);
}
