import {
    Box,
    Group,
    Paper,
    PasswordInput,
    Stack,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { notifications } from "@mantine/notifications";
import { CredentialResponse } from "@react-oauth/google";
import { useState } from "react";
import { AuthForm } from "../components/AuthForm";
import { GoogleButton } from "../components/GoogleButton";
import useAuth from "../hooks/useAuth";
import { LoginRequestDto } from "../types/requests/LoginDto";
import { RegisterDto } from "../types/requests/RegisterDto";

export default function Auth() {
    const auth = useAuth();

    const loginForm = useForm<LoginRequestDto>({
        initialValues: {
            credentials: "",
            password: "",
        },
    });

    const registerForm = useForm<RegisterDto>({
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

    const onRegister = (values: RegisterDto) => {
        auth.register(values);
    };

    const handleGoogleSuccess = (credentialResponse: CredentialResponse) => {
        if (credentialResponse.credential) {
            auth.loginWithGoogle(credentialResponse.credential);
        }
    };

    const handleGoogleError = () => {
        notifications.show({
            message: "Google Login Failed",
            color: "red",
            withBorder: true,
        });
    };

    const [selectedTab, setSelectedTab] = useState("login");

    return (
        <>
            <Stack w="100%" h="100%" justify="center" align="center">
                <Paper withBorder p={"xl"} maw={"100%"}>
                    <Stack gap={"xs"} align="stretch">
                        {selectedTab === "login" ? (
                            <AuthForm<LoginRequestDto>
                                mode="login"
                                form={loginForm}
                                onSubmit={onLogin}
                                onSwitchMode={() => setSelectedTab("register")}
                            >
                                <TextInput
                                    label="Username or Email"
                                    required
                                    {...loginForm.getInputProps("credentials")}
                                />
                                <PasswordInput
                                    required
                                    label="Password"
                                    {...loginForm.getInputProps("password")}
                                />
                            </AuthForm>
                        ) : (
                            <AuthForm<RegisterDto>
                                mode="register"
                                form={registerForm}
                                onSubmit={onRegister}
                                onSwitchMode={() => setSelectedTab("login")}
                            >
                                <TextInput
                                    required
                                    label="Email"
                                    {...registerForm.getInputProps("email")}
                                />
                                <TextInput
                                    required
                                    label="Username"
                                    {...registerForm.getInputProps("userName")}
                                />
                                <PasswordInput
                                    required
                                    label="Password"
                                    {...registerForm.getInputProps("password")}
                                />
                                <PasswordInput
                                    required
                                    label="Confirm Password"
                                    {...registerForm.getInputProps(
                                        "confirmPassword"
                                    )}
                                />
                            </AuthForm>
                        )}
                        <Group justify="center">
                            <Box className="google-login-wrapper">
                                <GoogleButton />
                            </Box>
                        </Group>
                    </Stack>
                </Paper>
            </Stack>
        </>
    );
}
