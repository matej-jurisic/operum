import {
    Box,
    Button,
    Group,
    Input,
    Paper,
    PasswordInput,
    Stack,
    Tabs,
    Text,
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
                        <Paper p="xl" shadow="md" w={350}>
                            <form onSubmit={loginForm.onSubmit(onLogin)}>
                                <Stack>
                                    <Title order={3}>Welcome to Operum</Title>
                                    <Input
                                        placeholder="Username or Email"
                                        {...loginForm.getInputProps(
                                            "credentials"
                                        )}
                                    />
                                    <PasswordInput
                                        placeholder="Password"
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
                        <Paper p="xl" shadow="md" w={350}>
                            <form onSubmit={registerForm.onSubmit(onRegister)}>
                                <Stack>
                                    <Title order={3}>Welcome to Operum</Title>
                                    <Input
                                        placeholder="Email"
                                        {...registerForm.getInputProps("email")}
                                    />
                                    <Input
                                        placeholder="Username"
                                        {...registerForm.getInputProps(
                                            "userName"
                                        )}
                                    />
                                    <PasswordInput
                                        placeholder="Password"
                                        {...registerForm.getInputProps(
                                            "password"
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
