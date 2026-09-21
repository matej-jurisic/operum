import { Alert, Anchor, Checkbox, Group, Modal, PasswordInput, Stack, Text, TextInput } from "@mantine/core";
import { PASSWORD_MIN_LENGTH, validatePassword, validatePasswordConfirmation } from "../utils/passwordRules";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { authController } from "../api/authenticationController";
import { AuthForm } from "../components/AuthForm";
import { GoogleButton } from "../components/GoogleButton";
import { isGoogleAuthEnabled } from "../config/googleAuth";
import useAuth from "../hooks/useAuth";
import { LoginRequestDto } from "../types/requests/LoginDto";
import { RegisterDto } from "../types/requests/RegisterDto";

interface Props {
    onClose: () => void;
    initialTab?: "login" | "register";
}

export default function AuthDialog(props: Props) {
    const auth = useAuth();

    const navigate = useNavigate();

    const loginForm = useForm<LoginRequestDto>({
        initialValues: {
            credentials: "",
            password: "",
        },
    });

    const registerForm = useForm<RegisterDto & { agreedToTerms: boolean }>({
        initialValues: {
            email: "",
            userName: "",
            password: "",
            confirmPassword: "",
            agreedToTerms: false,
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
            password: validatePassword,
            confirmPassword: (value, values) =>
                validatePasswordConfirmation(value, values.password),
            agreedToTerms: (value) =>
                !value ? "You must agree to the Terms and Privacy Policy" : null,
        },
    });

    const onSuccess = () => {
        navigate("/dashboard");
    };

    const onLogin = async (values: LoginRequestDto) => {
        const user = await authController.login(values);
        if (user.isSuccess) {
            auth.setUserData(user.data);
            props.onClose();
            onSuccess();
        }
    };

    const [selectedTab, setSelectedTab] = useState(props.initialTab ?? "login");
    const [registeredNotice, setRegisteredNotice] = useState<string>();

    const onRegister = async (values: RegisterDto & { agreedToTerms: boolean }) => {
        const { agreedToTerms: _, ...dto } = values;
        const res = await authController.register(dto);
        if (res.isSuccess) {
            // The message says whether to confirm the email first or log in right away
            setRegisteredNotice(res.messages[0]);
            loginForm.setFieldValue("credentials", dto.userName);
            setSelectedTab("login");
        }
    };

    return (
        <>
            <Modal opened onClose={props.onClose} centered>
                <Stack gap={"xs"} align="stretch" px={20}>
                    {selectedTab === "login" ? (
                        <AuthForm<LoginRequestDto>
                            mode="login"
                            form={loginForm}
                            onSubmit={onLogin}
                            onSwitchMode={() => setSelectedTab("register")}
                        >
                            {registeredNotice && (
                                <Alert color="teal" variant="light">
                                    {registeredNotice}
                                </Alert>
                            )}
                            <TextInput
                                label="Username or email"
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
                        <AuthForm<RegisterDto & { agreedToTerms: boolean }>
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
                                description={`At least ${PASSWORD_MIN_LENGTH} characters`}
                                {...registerForm.getInputProps("password")}
                            />
                            <PasswordInput
                                required
                                label="Confirm password"
                                {...registerForm.getInputProps("confirmPassword")}
                            />
                            <Checkbox
                                mt="xs"
                                {...registerForm.getInputProps("agreedToTerms", { type: "checkbox" })}
                                label={
                                    <Text size="xs">
                                        I agree to the{" "}
                                        <Anchor component={Link} to="/terms" size="xs" target="_blank">
                                            Terms of Service
                                        </Anchor>{" "}
                                        and{" "}
                                        <Anchor component={Link} to="/privacy" size="xs" target="_blank">
                                            Privacy Policy
                                        </Anchor>
                                    </Text>
                                }
                            />
                        </AuthForm>
                    )}
                    {isGoogleAuthEnabled && (
                        <Group justify="center">
                            <GoogleButton
                                onSuccess={() => {
                                    props.onClose();
                                    onSuccess();
                                }}
                            />
                        </Group>
                    )}
                </Stack>
            </Modal>
        </>
    );
}
