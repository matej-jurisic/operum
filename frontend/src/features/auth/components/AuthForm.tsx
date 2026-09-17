import { Button, Group, Stack, Text, Title } from "@mantine/core";
import { useForm } from "@mantine/form";

// Mirrors @mantine/form's own useForm<Values extends Record<string, any>> constraint.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
interface AuthFormProps<T extends Record<string, any>> {
    mode: "login" | "register";
    form: ReturnType<typeof useForm<T>>;
    onSubmit: (values: T) => void;
    children: React.ReactNode;
    onSwitchMode: () => void;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function AuthForm<T extends Record<string, any>>({
    mode,
    form,
    onSubmit,
    children,
    onSwitchMode,
}: AuthFormProps<T>) {
    const title = mode === "login" ? "Login" : "Register";
    const description =
        mode === "login"
            ? "Please log in to continue."
            : "Please register to continue.";
    const switchText =
        mode === "login"
            ? "Don't have an account?"
            : "Already have an account?";

    return (
        <>
            <Title order={3} ta="center" variant="gradient">
                {title}
            </Title>
            <Text ta="center" size="sm" c="dimmed">
                {description}
            </Text>

            <form onSubmit={form.onSubmit(onSubmit)}>
                <Stack gap={5}>
                    {children}
                    <Group>
                        <Button type="submit" fullWidth mt={"sm"}>
                            {title}
                        </Button>
                    </Group>
                </Stack>
            </form>

            <Group gap={10} align="center" wrap="nowrap" justify="center">
                <Text>{switchText} </Text>
                <Button variant="transparent" onClick={onSwitchMode} px={0}>
                    {mode === "login" ? "Register" : "Login"}
                </Button>
            </Group>
        </>
    );
}
