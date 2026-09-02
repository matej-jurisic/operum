import {
    Button,
    Group,
    ScrollArea,
    Stack,
    Text,
    ThemeIcon,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import dayjs from "dayjs";
import { observer } from "mobx-react";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { TbPlug } from "react-icons/tb";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import EmptyState from "../../../shared/components/EmptyState";
import SidebarBurger from "../../../shared/components/navigation/SidebarBurger";
import navigationStore from "../../../shared/stores/NavigationStore";
import { integrationsController } from "../api/integrationsController";
import ConnectProviderDialog from "../components/ConnectProviderDialog";
import IntegrationCard from "../components/IntegrationCard";
import TargetFormDialog from "../components/TargetFormDialog";
import WebhookSetupPanel from "../components/WebhookSetupPanel";
import {
    IntegrationDto,
    IntegrationTargetDto,
    ProviderDto,
} from "../types/IntegrationDto";
import {
    ConnectIntegrationDto,
    SaveIntegrationTargetDto,
} from "../types/requests/SaveIntegrationTargetDto";

const IntegrationsPage = observer(function IntegrationsPage() {
    const theme = useMantineTheme();
    const isMobile = useMediaQuery("(max-width: 48em)");

    const [providers, setProviders] = useState<ProviderDto[]>([]);
    const [integrations, setIntegrations] = useState<IntegrationDto[]>([]);
    const [loaded, setLoaded] = useState(false);

    const [connectOpen, setConnectOpen] = useState(false);
    const [targetDialog, setTargetDialog] = useState<{
        integration: IntegrationDto;
        target?: IntegrationTargetDto;
    } | null>(null);
    const [webhookPanel, setWebhookPanel] = useState<{
        provider: ProviderDto;
        integrationId: string;
        target: IntegrationTargetDto;
    } | null>(null);
    const [confirming, setConfirming] = useState<{
        message: string;
        onConfirm: () => Promise<void>;
    } | null>(null);
    const [syncingTargetId, setSyncingTargetId] = useState<string | null>(null);
    const [syncingIntegrationId, setSyncingIntegrationId] = useState<
        string | null
    >(null);

    const load = async () => {
        const [providerResponse, integrationResponse] = await Promise.all([
            integrationsController.getProviders(),
            integrationsController.getIntegrations(),
        ]);
        setProviders(providerResponse.data ?? []);
        setIntegrations(integrationResponse.data ?? []);
        setLoaded(true);
    };

    useEffect(() => {
        load();
        // The mapping editor lists the user's trackers, which the sidebar has usually
        // already loaded -- but this page can be opened directly by URL.
        if (navigationStore.trackers.length === 0) {
            navigationStore.refreshTrackers();
        }
    }, []);

    const refresh = async () => {
        const response = await integrationsController.getIntegrations();
        setIntegrations(response.data ?? []);
    };

    const providerFor = (integration: IntegrationDto) =>
        providers.find((p) => p.key === integration.provider);

    const connect = async (dto: ConnectIntegrationDto) => {
        const response = await integrationsController.connect(dto);
        if (!response?.isSuccess) return false;
        await refresh();
        return true;
    };

    const saveTarget = async (dto: SaveIntegrationTargetDto) => {
        if (!targetDialog) return false;
        const { integration, target } = targetDialog;

        const response = target
            ? await integrationsController.updateTarget(
                  integration.id,
                  target.id,
                  dto,
              )
            : await integrationsController.createTarget(integration.id, dto);

        if (!response?.isSuccess) return false;

        await refresh();

        // A new push target still needs wiring to the provider: either its Operum-minted
        // secret comes back on this one response and has to be shown now, or the provider
        // mints the secret and the user needs the URL plus a field to paste it back into.
        const saved = response.data;
        const provider = providerFor(integration);
        if (!target && saved?.mode === "Push" && saved.webhookUrl && provider) {
            setWebhookPanel({
                provider,
                integrationId: integration.id,
                target: saved,
            });
        }

        return true;
    };

    const saveWebhookSecret = async (secret: string) => {
        if (!webhookPanel) return false;
        const response = await integrationsController.setWebhookSecret(
            webhookPanel.integrationId,
            webhookPanel.target.id,
            secret,
        );
        if (!response?.isSuccess) return false;
        await refresh();
        return true;
    };

    const syncNow = async (
        integration: IntegrationDto,
        target: IntegrationTargetDto,
    ) => {
        setSyncingTargetId(target.id);
        try {
            await integrationsController.syncNow(integration.id, target.id);
        } finally {
            setSyncingTargetId(null);
            // Refresh either way: a failed sync records its reason on the target, and that
            // is the most useful thing to show.
            await refresh();
        }
    };

    const resyncTarget = (
        integration: IntegrationDto,
        target: IntegrationTargetDto,
    ) => {
        setConfirming({
            message: `Re-import ${target.trackerName} from ${dayjs(target.backfillFrom).format("D MMM YYYY")}? Mapped fields on existing entries will be overwritten, including any manual edits.`,
            onConfirm: async () => {
                setSyncingTargetId(target.id);
                try {
                    await integrationsController.resyncTarget(
                        integration.id,
                        target.id,
                    );
                } finally {
                    setSyncingTargetId(null);
                    await refresh();
                }
            },
        });
    };

    const syncIntegration = async (integration: IntegrationDto) => {
        setSyncingIntegrationId(integration.id);
        try {
            await integrationsController.syncIntegration(integration.id);
        } finally {
            setSyncingIntegrationId(null);
            // Refresh either way: a failed sync records its reason on each target, and
            // that is the most useful thing to show.
            await refresh();
        }
    };

    const changeSecret = (
        integration: IntegrationDto,
        target: IntegrationTargetDto,
    ) => {
        const provider = providerFor(integration);
        if (!provider) return;

        // Firefly mints the secret itself, so there is nothing to "issue": open the panel
        // with the URL and a field to paste the provider's secret into.
        if (provider.providerSuppliesSecret) {
            setWebhookPanel({
                provider,
                integrationId: integration.id,
                target,
            });
            return;
        }

        setConfirming({
            message: `Issue a new secret for ${target.trackerName}? The old one stops working right away, and deliveries fail until you paste the new one into ${provider.displayName}.`,
            onConfirm: async () => {
                const response = await integrationsController.setWebhookSecret(
                    integration.id,
                    target.id,
                );
                if (response?.isSuccess && response.data) {
                    setWebhookPanel({
                        provider,
                        integrationId: integration.id,
                        target: response.data,
                    });
                }
                await refresh();
            },
        });
    };

    const deleteTarget = (
        integration: IntegrationDto,
        target: IntegrationTargetDto,
    ) => {
        setConfirming({
            message: `Stop importing into ${target.trackerName}? Entries already imported stay.`,
            onConfirm: async () => {
                await integrationsController.deleteTarget(
                    integration.id,
                    target.id,
                );
                await refresh();
            },
        });
    };

    const disconnect = (integration: IntegrationDto) => {
        const name = providerFor(integration)?.displayName ?? integration.provider;
        setConfirming({
            message: `Disconnect ${name}? All of its imports stop. Entries already imported stay.`,
            onConfirm: async () => {
                await integrationsController.disconnect(integration.id);
                await refresh();
            },
        });
    };

    return (
        <>
            <Stack h="100%" gap="md">
                <Group
                    align="center"
                    w="100%"
                    justify="space-between"
                    wrap="nowrap"
                >
                    <Group
                        gap="sm"
                        align="center"
                        wrap="nowrap"
                        style={{ minWidth: 0 }}
                    >
                        <SidebarBurger />
                        <Title c={theme.primaryColor} order={2}>
                            Integrations
                        </Title>
                    </Group>
                    {/* While nothing is connected the empty state carries the call to
                        action, so a second Connect button here would only crowd the row. */}
                    {integrations.length > 0 && (
                        <Button
                            variant="outline"
                            color={theme.primaryColor}
                            px={isMobile ? "xs" : undefined}
                            leftSection={
                                isMobile ? undefined : <FiPlus size={18} />
                            }
                            onClick={() => setConnectOpen(true)}
                            aria-label="Connect an integration"
                            style={{ flexShrink: 0 }}
                        >
                            {isMobile ? <FiPlus size={18} /> : "Connect"}
                        </Button>
                    )}
                </Group>

                {/* The global request loader already covers the first fetch; rendering
                    nothing here avoids a second, differently-styled spinner on top of it. */}
                <ScrollArea flex={1} mih={0}>
                    {!loaded ? null : providers.length === 0 ? (
                        <EmptyState
                            title="No integrations available"
                            hint="This deployment has no integration providers enabled."
                        />
                    ) : integrations.length === 0 ? (
                        <NothingConnected
                            providers={providers}
                            color={theme.primaryColor}
                            onConnect={() => setConnectOpen(true)}
                        />
                    ) : (
                        <Stack gap="md" pb="md">
                            {integrations.map((integration) => (
                                <IntegrationCard
                                    key={integration.id}
                                    integration={integration}
                                    provider={providerFor(integration)}
                                    syncingTargetId={syncingTargetId}
                                    syncingIntegration={
                                        syncingIntegrationId === integration.id
                                    }
                                    onAddTarget={() =>
                                        setTargetDialog({ integration })
                                    }
                                    onEditTarget={(target) =>
                                        setTargetDialog({ integration, target })
                                    }
                                    onDeleteTarget={(target) =>
                                        deleteTarget(integration, target)
                                    }
                                    onSyncNow={(target) =>
                                        syncNow(integration, target)
                                    }
                                    onResync={(target) =>
                                        resyncTarget(integration, target)
                                    }
                                    onSyncAll={() =>
                                        syncIntegration(integration)
                                    }
                                    onChangeSecret={(target) =>
                                        changeSecret(integration, target)
                                    }
                                    onDisconnect={() => disconnect(integration)}
                                />
                            ))}
                        </Stack>
                    )}
                </ScrollArea>
            </Stack>

            <ConnectProviderDialog
                opened={connectOpen}
                onClose={() => setConnectOpen(false)}
                providers={providers}
                onConnect={connect}
            />

            {targetDialog && providerFor(targetDialog.integration) && (
                <TargetFormDialog
                    opened
                    onClose={() => setTargetDialog(null)}
                    provider={providerFor(targetDialog.integration)!}
                    target={targetDialog.target}
                    onSave={saveTarget}
                />
            )}

            {webhookPanel && (
                <WebhookSetupPanel
                    opened
                    onClose={() => setWebhookPanel(null)}
                    provider={webhookPanel.provider}
                    target={webhookPanel.target}
                    onSaveSecret={saveWebhookSecret}
                />
            )}

            <ConfirmationDialog
                isOpen={!!confirming}
                onClose={() => setConfirming(null)}
                onConfirm={async () => {
                    const action = confirming;
                    setConfirming(null);
                    await action?.onConfirm();
                }}
                message={confirming?.message ?? ""}
                severity="warning"
            />
        </>
    );
});

/**
 * The page-level "nothing here yet" treatment -- the same icon, headline and single
 * call to action a board with no widgets gets, rather than the bordered card used for
 * an empty list inside a tab.
 */
function NothingConnected({
    providers,
    color,
    onConnect,
}: {
    providers: ProviderDto[];
    color: string;
    onConnect: () => void;
}) {
    return (
        <Stack align="center" gap="md" py={80} px="md">
            <ThemeIcon size={72} radius="xl" variant="light" color={color}>
                <TbPlug size={36} />
            </ThemeIcon>
            <Text fw={700} size="xl" ta="center">
                Bring data in automatically
            </Text>
            <Text c="dimmed" ta="center" maw={460}>
                Connect a service and map its values to tracker fields.
                Operum keeps them up to date automatically.
            </Text>
            <Text size="sm" c="dimmed" ta="center">
                Available: {providers.map((p) => p.displayName).join(", ")}
            </Text>
            <Button
                color={color}
                leftSection={<FiPlus size={16} />}
                onClick={onConnect}
            >
                Get Started
            </Button>
        </Stack>
    );
}

export default IntegrationsPage;
