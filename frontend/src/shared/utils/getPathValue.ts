/** Resolves a dot path (e.g. "queries.0.sorts"), matching Mantine useForm's nested-field path format. */
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
