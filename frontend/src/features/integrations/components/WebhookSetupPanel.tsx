import {
    ActionIcon,
    Alert,
    Button,
    Code,
    CopyButton,
    Group,
    List,
    Modal,
    Stack,
    Text,
    Tooltip,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { MdCheck, MdContentCopy, MdWarning } from "react-icons/md";
import { IntegrationTargetDto, ProviderDto } from "../types/IntegrationDto";

interface WebhookSetupPanelProps {
    opened: boolean;
    onClose: () => void;
    provider: ProviderDto;
    target: IntegrationTargetDto;
}

/**
 * Shown once, right after a push target is created or its secret rotated. The secret is
 * stored encrypted and is never returned again, so this is the only chance to copy it --
 * which the panel has to say plainly rather than assume.
 */
export default function WebhookSetupPanel({
    opened,
    onClose,
    provider,
    target,
}: WebhookSetupPanelProps) {
    const theme = useMantineTheme();
    const isMobile = useMediaQuery("(max-width: 48em)");

    return (
        <Modal
            opened={opened}
            onClose={onClose}
            title={`Finish setting up ${provider.displayName}`}
            size="lg"
            centered
            fullScreen={isMobile}
        >
            <Stack gap="lg">
                <Alert
                    icon={<MdWarning size={18} />}
                    color="orange"
                    variant="light"
                    title="Copy the secret now"
                >
                    <Text size="sm" className="wrapped-text">
                        It is stored encrypted and cannot be shown again. If you
                        lose it, issue a new one with the key button on the
                        import.
                    </Text>
                </Alert>

                <Stack gap="md">
                    <CopyableValue
                        label="Webhook URL"
                        value={target.webhookUrl ?? ""}
                    />
                    <CopyableValue
                        label="Secret"
                        value={target.webhookSecret ?? ""}
                    />
                </Stack>

                <Stack gap={4}>
                    <Text fw={500} size="md">
                        In {provider.displayName}
                    </Text>
                    <List size="sm" spacing={4} c="dimmed">
                        <List.Item>
                            Go to <b>Automation → Webhooks</b> and create a new
                            webhook.
                        </List.Item>
                        <List.Item>
                            Set <b>Trigger</b> to fire after a transaction is
                            created, updated and destroyed.
                        </List.Item>
                        <List.Item>
                            Set <b>Response</b> to <Code>TRANSACTIONS</Code>, so
                            the full detail is sent.
                        </List.Item>
                        <List.Item>
                            Paste the URL and secret above, then save.
                        </List.Item>
                    </List>
                </Stack>

                <Text size="xs" c="dimmed" className="wrapped-text">
                    Your {provider.displayName} instance calls Operum, so it
                    does not need to be reachable from the internet. Only
                    transactions from the moment you connect are imported —
                    there is no history to backfill.
                </Text>

                <Button color={theme.primaryColor} size="md" onClick={onClose}>
                    Done
                </Button>
            </Stack>
        </Modal>
    );
}

function CopyableValue({ label, value }: { label: string; value: string }) {
    return (
        <Stack gap={4}>
            <Text fw={500} size="sm">
                {label}
            </Text>
            <Group gap="xs" wrap="nowrap" align="center">
                {/* miw={0} lets the code block shrink instead of pushing the copy
                    button off the edge on a long URL. */}
                <Code
                    block
                    style={{
                        flex: 1,
                        minWidth: 0,
                        overflowX: "auto",
                        whiteSpace: "nowrap",
                    }}
                >
                    {value}
                </Code>
                <CopyButton value={value} timeout={2000}>
                    {({ copied, copy }) => (
                        <Tooltip
                            label={copied ? "Copied" : `Copy ${label}`}
                            withArrow
                        >
                            <ActionIcon
                                variant="outline"
                                color={copied ? "teal" : "green"}
                                size="lg"
                                onClick={copy}
                                aria-label={`Copy ${label}`}
                                style={{ flexShrink: 0 }}
                            >
                                {copied ? (
                                    <MdCheck size={16} />
                                ) : (
                                    <MdContentCopy size={16} />
                                )}
                            </ActionIcon>
                        </Tooltip>
                    )}
                </CopyButton>
            </Group>
        </Stack>
    );
}
