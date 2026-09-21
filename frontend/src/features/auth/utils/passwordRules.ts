export const PASSWORD_MIN_LENGTH = 10;
export const PASSWORD_MAX_LENGTH = 128;

/** Length only; the server also rejects common passwords and ones matching the username or email. */
export function validatePassword(value: string): string | null {
    if (value.length < PASSWORD_MIN_LENGTH)
        return `Password must be at least ${PASSWORD_MIN_LENGTH} characters long`;
    if (value.length > PASSWORD_MAX_LENGTH)
        return `Password must be at most ${PASSWORD_MAX_LENGTH} characters long`;
    return null;
}

export function validatePasswordConfirmation(
    value: string | undefined,
    password: string | undefined,
): string | null {
    return value === password ? null : "Passwords must match";
}
