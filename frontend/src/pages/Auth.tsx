import {
    Box,
    Button,
    Group,
    Paper,
    PasswordInput,
    Stack,
    Tabs,
    Text,
    TextInput,
    Title,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import useAuth from "../hooks/useAuth";
import { LoginRequestDto } from "../model/requests/LoginRequestDto";
import { RegisterRequestDto } from "../model/requests/RegisterRequestDto";

export default function Auth() {
    const auth = useAuth();

    const loginForm = useForm<LoginRequestDto>({
        initialValues: {
            credentials: "",
            password: "",
        },
    });

    const registerForm = useForm<RegisterRequestDto>({
        initialValues: {
            email: "",
            userName: "",
            password: "",
            confirmPassword: "",
        },
        validate: {
            email: (value) => {
                if (!/\S+@\S+\.\S+/.test(value)) {
                    return "Invalid email";
                }
                return null;
            },
            userName: (value) => {
                if (value.length < 3) {
                    return "Username must be at least 3 characters long";
                }
                if (value.length > 20) {
                    return "Username must be at most 20 characters long";
                }
                return null;
            },
            password: (value) => {
                if (value.length < 6) {
                    return "Password must be at least 6 characters long";
                }
                if (!/[A-Z]/.test(value)) {
                    return "Password must include an uppercase letter";
                }
                if (!/[a-z]/.test(value)) {
                    return "Password must include a lowercase letter";
                }
                if (!/\d/.test(value)) {
                    return "Password must include a number";
                }
                if (!/[^\w\d\s]/.test(value)) {
                    return "Password must include a special character";
                }
                return null;
            },
            confirmPassword: (value, values) => {
                if (value !== values.password) {
                    return "Passwords must match";
                }
                return null;
            },
        },
    });

    const onLogin = (values: LoginRequestDto) => {
        auth.login(values);
    };

    const onRegister = (values: RegisterRequestDto) => {
        auth.register(values);
    };

    const [selectedTab, setSelectedTab] = useState("login");

    return (
        <>
            <Box
                style={{
                    display: "flex",
                    justifyContent: "center",
                    position: "absolute",
                    top: "50%",
                    left: "50%",
                    transform: "translate(-50%, -50%)",
                    width: "100%",
                }}
            >
                <Tabs
                    defaultValue={"login"}
                    variant="outline"
                    value={selectedTab}
                    onChange={(v) => {
                        if (v) setSelectedTab(v);
                    }}
                >
                    <Tabs.Panel value="login">
                        <Paper p="xl" shadow="xl" w={350}>
                            <form onSubmit={loginForm.onSubmit(onLogin)}>
                                <Stack>
                                    <Title
                                        order={3}
                                        ta="center"
                                        variant="gradient"
                                    >
                                        Welcome to Operum
                                    </Title>
                                    <Text ta="center" size="sm" c="dimmed">
                                        Please log in to continue.
                                    </Text>
                                    <TextInput
                                        label="Username or Email"
                                        {...loginForm.getInputProps(
                                            "credentials"
                                        )}
                                    />
                                    <PasswordInput
                                        label="Password"
                                        {...loginForm.getInputProps("password")}
                                    />
                                    <Group>
                                        <Button type="submit" fullWidth>
                                            Login
                                        </Button>
                                    </Group>
                                    <Text>
                                        Don't have an account?{" "}
                                        <Button
                                            variant="transparent"
                                            onClick={() =>
                                                setSelectedTab("register")
                                            }
                                        >
                                            Register
                                        </Button>
                                    </Text>
                                </Stack>
                            </form>
                        </Paper>
                    </Tabs.Panel>
                    <Tabs.Panel value="register">
                        <Paper p="xl" shadow="xl" w={350}>
                            <form onSubmit={registerForm.onSubmit(onRegister)}>
                                <Stack gap="sm">
                                    <Title
                                        order={3}
                                        ta="center"
                                        variant="gradient"
                                    >
                                        Welcome to Operum
                                    </Title>
                                    <Text ta="center" size="sm" c="dimmed">
                                        Please register to continue.
                                    </Text>
                                    <TextInput
                                        label="Email"
                                        {...registerForm.getInputProps("email")}
                                    />
                                    <TextInput
                                        label="Username"
                                        {...registerForm.getInputProps(
                                            "userName"
                                        )}
                                    />
                                    <PasswordInput
                                        label="Password"
                                        {...registerForm.getInputProps(
                                            "password"
                                        )}
                                    />
                                    <PasswordInput
                                        label="Confirm Password"
                                        {...registerForm.getInputProps(
                                            "confirmPassword"
                                        )}
                                    />
                                    <Group>
                                        <Button type="submit" fullWidth>
                                            Register
                                        </Button>
                                    </Group>
                                    <Text>
                                        Already have an account?{" "}
                                        <Button
                                            variant="transparent"
                                            onClick={() =>
                                                setSelectedTab("login")
                                            }
                                        >
                                            Login
                                        </Button>
                                    </Text>
                                </Stack>
                            </form>
                        </Paper>
                    </Tabs.Panel>
                </Tabs>
            </Box>
        </>
    );
}
