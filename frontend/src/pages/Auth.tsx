import {
    Box,
    Button,
    Group,
    Input,
    Paper,
    PasswordInput,
    Stack,
    Title,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import api from "../api/api";
import useAuth from "../hooks/useAuth";
import { LoginRequestDto } from "../model/requests/LoginRequestDto";

export default function Auth() {
    const auth = useAuth();

    const form = useForm<LoginRequestDto>({
        initialValues: {
            credentials: "",
            password: "",
        },
    });

    const GetTrackers = async () => {
        await api.get("/trackers");
    };

    const onSubmit = (values: LoginRequestDto) => {
        auth.authenticate(values);
    };

    return (
        <Box
            style={{
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                minHeight: "100vh",
            }}
        >
            <Paper withBorder p="xl" shadow="md" w={350}>
                <Title order={2} ta="center" mb="lg">
                    Log in
                </Title>
                <form onSubmit={form.onSubmit(onSubmit)}>
                    <Stack>
                        <Input
                            placeholder="Username or Email"
                            {...form.getInputProps("credentials")}
                        />
                        <PasswordInput
                            placeholder="Password"
                            {...form.getInputProps("password")}
                        />
                        <Group>
                            <Button type="submit" fullWidth>
                                Authenticate
                            </Button>
                            <Button
                                fullWidth
                                variant="gradient"
                                onClick={() => auth.refresh()}
                            >
                                Refresh
                            </Button>
                            <Button fullWidth onClick={() => GetTrackers()}>
                                Find Out
                            </Button>
                        </Group>
                    </Stack>
                </form>
            </Paper>
        </Box>
    );
}
