/**
 * Resolves a dot-notated path (e.g. "queries.0.sorts") against a nested
 * object/array, the same path format used by Mantine's useForm for nested
 * fields and list items.
 */
export function getPathValue<T = unknown>(
    source: unknown,
    path: string,
): T | undefined {
    return path
        .split(".")
        .reduce<unknown>(
            (value, key) =>
                value === undefined || value === null
                    ? undefined
                    : (value as Record<string, unknown>)[key],
            source,
        ) as T | undefined;
}
