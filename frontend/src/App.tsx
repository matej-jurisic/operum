import {
    Box,
    Button,
    Flex,
    Grid,
    Group,
    Input,
    PasswordInput,
    Select,
    Stack,
} from "@mantine/core";
import { Dispatch, SetStateAction, useState } from "react";
import api from "./api/api";
import "./App.css";
import { EntryDto } from "./model/EntryDto";
import { FieldDto } from "./model/FieldDto";
import { SingleFieldNumericAnalyticsDto } from "./model/SingleFieldNumericAnalyticsDto";
import { TrackerDto } from "./model/TrackerDto";

interface LoginRequestDto {
    credentials: string;
    password: string;
}

const authenticate = async (loginRequest: LoginRequestDto) => {
    await api.post("/auth/login", loginRequest);
};

const GetTrackers = async (
    setTrackers: Dispatch<SetStateAction<TrackerDto[]>>
) => {
    const response = await api.get("/trackers");
    setTrackers(response.data.data);
};

const GetEntries = async (
    trackerId: string,
    setEntries: Dispatch<SetStateAction<EntryDto[]>>
) => {
    const response = await api.get(`/trackers/${trackerId}/entries`);
    setEntries(response.data.data);
};

function App() {
    const [loginRequest, setLoginRequest] = useState<LoginRequestDto>({
        credentials: "",
        password: "",
    });
    const [trackers, setTrackers] = useState<TrackerDto[]>([]);
    const [entries, setEntries] = useState<EntryDto[]>([]);
    const [analytics, setAnalytics] =
        useState<SingleFieldNumericAnalyticsDto>();

    const GetAnalytics = async (trackerId: string, fieldId: string) => {
        const response = await api.get(
            `/trackers/${trackerId}/fields/${fieldId}/analytics/numeric`
        );
        setAnalytics({ ...response.data.data });
    };

    const renderValue = (value: unknown | null) => {
        if (value === null) {
            return "Value not set.";
        }
        if (typeof value === "string") {
            return value;
        }
        if (typeof value === "number") {
            return value;
        }

        if (typeof value === "boolean") {
            return value.toString();
        }

        if (value instanceof Date) {
            return value.toLocaleString();
        }

        return "Unexpected data type";
    };

    return (
        <Flex direction="column" style={{ height: "100vh" }}>
            <Box style={{ padding: "20px 300px" }}>
                <Flex justify={"space-between"}>
                    <Box
                        style={{
                            width: "300px",
                            padding: "2rem",
                            border: "1px solid #ddd",
                            borderRadius: "8px",
                        }}
                    >
                        <Stack gap="lg">
                            <Input
                                type="text"
                                value={loginRequest.credentials}
                                onChange={(e) =>
                                    setLoginRequest((prev) => ({
                                        ...prev,
                                        credentials: e.target.value,
                                    }))
                                }
                                title="Credentials"
                                placeholder="Your credentials"
                            ></Input>
                            <PasswordInput
                                type="password"
                                value={loginRequest.password}
                                onChange={(e) =>
                                    setLoginRequest((prev) => ({
                                        ...prev,
                                        password: e.target.value,
                                    }))
                                }
                                title="Password"
                                placeholder="Your password"
                            ></PasswordInput>
                            <Button onClick={() => authenticate(loginRequest)}>
                                Authenticate
                            </Button>
                            <Button onClick={() => GetTrackers(setTrackers)}>
                                Get Trackers
                            </Button>
                        </Stack>
                    </Box>
                    <Box
                        style={{
                            marginTop: "20px",
                            padding: "2rem",
                            borderRadius: "8px",
                        }}
                    >
                        <Stack>
                            {trackers.map((x) => (
                                <Group>
                                    {x.name}
                                    <Button
                                        onClick={() =>
                                            GetEntries(x.id, setEntries)
                                        }
                                    >
                                        Get Entries
                                    </Button>
                                    {x.fields && (
                                        <Select
                                            data={x.fields
                                                .filter(
                                                    (x) => x.type === "number"
                                                )
                                                .map((x: FieldDto) => ({
                                                    label: x.name,
                                                    value: x.id,
                                                }))}
                                            onChange={(e) => {
                                                if (e) {
                                                    GetAnalytics(x.id, e);
                                                }
                                            }}
                                        />
                                    )}
                                </Group>
                            ))}
                        </Stack>
                    </Box>

                    <Grid gutter="md" style={{ width: "30%" }}>
                        <Grid.Col span={{ base: 12, sm: 6 }}>
                            <Input.Wrapper label="Count">
                                <Input
                                    readOnly
                                    value={analytics?.count ?? ""}
                                />
                            </Input.Wrapper>
                        </Grid.Col>
                        <Grid.Col span={{ base: 12, sm: 6 }}>
                            <Input.Wrapper label="Average">
                                <Input
                                    readOnly
                                    value={analytics?.average ?? ""}
                                />
                            </Input.Wrapper>
                        </Grid.Col>
                        <Grid.Col span={{ base: 12, sm: 6 }}>
                            <Input.Wrapper label="StdDev">
                                <Input
                                    readOnly
                                    value={analytics?.stdDev ?? ""}
                                />
                            </Input.Wrapper>
                        </Grid.Col>
                        <Grid.Col span={{ base: 12, sm: 6 }}>
                            <Input.Wrapper label="Min">
                                <Input readOnly value={analytics?.min ?? ""} />
                            </Input.Wrapper>
                        </Grid.Col>
                        <Grid.Col span={{ base: 12, sm: 6 }}>
                            <Input.Wrapper label="Max">
                                <Input readOnly value={analytics?.max ?? ""} />
                            </Input.Wrapper>
                        </Grid.Col>
                    </Grid>
                </Flex>
            </Box>

            {entries.length > 0 && (
                <Box
                    style={{
                        flexGrow: 1,
                        padding: "2rem",
                        border: "1px solid #ddd",
                        borderRadius: "8px",
                        overflowY: "auto",
                        margin: "0px 300px 20px 300px",
                    }}
                >
                    <Stack>
                        {entries.map((x) => (
                            <Group
                                style={{
                                    border: "1px solid #ddd",
                                    borderRadius: "8px",
                                    padding: "2rem",
                                    alignItems: "center",
                                    gap: "50px",
                                }}
                            >
                                {new Date(x.createdAt).toLocaleString()}
                                <Group style={{ gap: "20px" }}>
                                    {x.fieldValues.map((v) => (
                                        <Input.Wrapper label={v.fieldName}>
                                            <Input
                                                readOnly
                                                value={renderValue(v.value)}
                                            />
                                        </Input.Wrapper>
                                    ))}
                                </Group>
                            </Group>
                        ))}
                    </Stack>
                </Box>
            )}
        </Flex>
    );
}

export default App;
