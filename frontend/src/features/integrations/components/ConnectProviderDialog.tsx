import {
    Alert,
    Button,
    Modal,
    PasswordInput,
    Radio,
    Stack,
    Text,
    TextInput,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useEffect, useState } from "react";
import { MdInfoOutline } from "react-icons/md";
import { ProviderDto } from "../types/IntegrationDto";
import { ConnectIntegrationDto } from "../types/requests/SaveIntegrationTargetDto";

interface ConnectProviderDialogProps {
    opened: boolean;
    onClose: () => void;
    providers: ProviderDto[];
    onConnect: (dto: ConnectIntegrationDto) => Promise<boolean>;
}

export default function ConnectProviderDialog({
    opened,
    onClose,
    providers,
    onConnect,
}: ConnectProviderDialogProps) {
    const theme = useMantineTheme();
    const isMobile = useMediaQuery("(max-width: 48em)");

    const [providerKey, setProviderKey] = useState(providers[0]?.key ?? "");
    const [credential, setCredential] = useState("");
    const [baseUrl, setBaseUrl] = useState("");
    const [connecting, setConnecting] = useState(false);

    useEffect(() => {
        if (!opened) return;
        setProviderKey(providers[0]?.key ?? "");
        setCredential("");
        setBaseUrl("");
    }, [opened, providers]);

    const provider = providers.find((p) => p.key === providerKey);

    // A push-only provider has nothing to call, so there is no credential to verify: the
    // first signed delivery is what proves the connection.
    const needsCredential = !!provider?.supportsPull;

    const canConnect =
        !!provider &&
        !connecting &&
        (!needsCredential || credential.trim().length > 0) &&
        (!provider.requiresBaseUrl || baseUrl.trim().length > 0);

    const connect = async () => {
        if (!provider) return;
        setConnecting(true);
        const ok = await onConnect({
            provider: provider.key,
            credential: needsCredential ? credential.trim() : undefined,
            baseUrl: provider.requiresBaseUrl ? baseUrl.trim() : undefined,
        });
        setConnecting(false);
        if (ok) onClose();
    };

    return (
        <Modal
            opened={opened}
            onClose={onClose}
            title="Connect an integration"
            centered
            fullScreen={isMobile}
        >
            <Stack gap="lg">
                <Stack gap="md">
                    <Radio.Group
                        label="Service"
                        value={providerKey}
                        onChange={setProviderKey}
                    >
                        <Stack gap="xs" mt="xs">
                            {providers.map((p) => (
                                <Radio
                                    key={p.key}
                                    value={p.key}
                                    color={theme.primaryColor}
                                    label={p.displayName}
                                    description={
                                        p.supportsPull
                                            ? "Operum checks for new data on a schedule."
                                            : "Your instance sends data to Operum as it changes."
                                    }
                                />
                            ))}
                        </Stack>
                    </Radio.Group>

                    {provider?.requiresBaseUrl && (
                        <TextInput
                            label="Instance address"
                            placeholder="https://firefly.example.com"
                            description="Must be https, and reachable from the internet."
                            value={baseUrl}
                            onChange={(event) =>
                                setBaseUrl(event.currentTarget.value)
                            }
                        />
                    )}

                    {needsCredential ? (
                        <PasswordInput
                            label="API key"
                            description={
                                provider?.key === "intervals.icu"
                                    ? "In intervals.icu, go to Settings → Developer Settings to generate one. The key is checked before it is saved, and stored encrypted — it is never shown again."
                                    : "The key is checked before it is saved, and stored encrypted. It is never shown again."
                            }
                            value={credential}
                            onChange={(event) =>
                                setCredential(event.currentTarget.value)
                            }
                        />
                    ) : (
                        <Alert
                            icon={<MdInfoOutline size={18} />}
                            variant="light"
                            color="blue"
                        >
                            <Text size="sm" className="wrapped-text">
                                No key needed. After connecting you will get a
                                URL and a secret to paste into{" "}
                                {provider?.displayName}, which then sends its
                                data here.
                            </Text>
                        </Alert>
                    )}
                </Stack>

                <Button
                    color={theme.primaryColor}
                    size="md"
                    onClick={connect}
                    disabled={!canConnect}
                    loading={connecting}
                >
                    Connect
                </Button>
            </Stack>
        </Modal>
    );
}
