import {
    ActionIcon,
    Badge,
    Card,
    Group,
    Paper,
    Stack,
    Text,
    Title,
    Tooltip,
    useMantineTheme,
} from "@mantine/core";
import dayjs from "dayjs";
import { FiPlus, FiRefreshCw } from "react-icons/fi";
import {
    MdDelete,
    MdEdit,
    MdHistory,
    MdLinkOff,
    MdVpnKey,
    MdWarning,
} from "react-icons/md";
import {
    IntegrationDto,
    IntegrationTargetDto,
    ProviderDto,
} from "../types/IntegrationDto";

interface IntegrationCardProps {
    integration: IntegrationDto;
    provider?: ProviderDto;
    syncingTargetId: string | null;
    syncingIntegration: boolean;
    onAddTarget: () => void;
    onEditTarget: (target: IntegrationTargetDto) => void;
    onDeleteTarget: (target: IntegrationTargetDto) => void;
    onSyncNow: (target: IntegrationTargetDto) => void;
    onResync: (target: IntegrationTargetDto) => void;
    onSyncAll: () => void;
    onChangeSecret: (target: IntegrationTargetDto) => void;
    onDisconnect: () => void;
}

export default function IntegrationCard({
    integration,
    provider,
    syncingTargetId,
    syncingIntegration,
    onAddTarget,
    onEditTarget,
    onDeleteTarget,
    onSyncNow,
    onResync,
    onSyncAll,
    onChangeSecret,
    onDisconnect,
}: IntegrationCardProps) {
    const theme = useMantineTheme();
    const name = provider?.displayName ?? integration.provider;

    // One button that pulls every tracker fed by this connection, fetching each kind of
    // data once rather than once per tracker. Only meaningful when something is pulled.
    const hasPullTarget = integration.targets.some((t) => t.mode === "Pull");

    const details =
        [
            integration.externalAccountId,
            integration.baseUrl,
            integration.maskedCredential || null,
        ]
            .filter(Boolean)
            .join(" · ") || "Connected";

    return (
        <Card p="md" radius="md" withBorder>
            <Stack gap="md">
                <Group align="flex-start" justify="space-between" wrap="nowrap">
                    <Stack gap="xs" flex={1} miw={0}>
                        <Group gap="xs" wrap="wrap">
                            <Title
                                order={4}
                                lineClamp={1}
                                className="wrapped-text"
                            >
                                {name}
                            </Title>
                            {!integration.isEnabled && (
                                <Badge color="gray" variant="light" size="sm">
                                    Paused
                                </Badge>
                            )}
                        </Group>
                        <Text
                            c="dimmed"
                            size="sm"
                            lineClamp={2}
                            className="wrapped-text"
                        >
                            {details}
                        </Text>
                    </Stack>

                    <Group
                        gap="xs"
                        wrap="nowrap"
                        align="flex-start"
                        style={{ flexShrink: 0 }}
                    >
                        {hasPullTarget && (
                            <Tooltip
                                label="Check every import for new data"
                                withArrow
                            >
                                <ActionIcon
                                    variant="outline"
                                    color={theme.primaryColor}
                                    size="lg"
                                    loading={syncingIntegration}
                                    onClick={onSyncAll}
                                    aria-label={`Sync everything from ${name} now`}
                                >
                                    <FiRefreshCw size={16} />
                                </ActionIcon>
                            </Tooltip>
                        )}
                        <Tooltip label="Import into a tracker" withArrow>
                            <ActionIcon
                                variant="outline"
                                color={theme.primaryColor}
                                size="lg"
                                onClick={onAddTarget}
                                aria-label={`Import from ${name} into a tracker`}
                            >
                                <FiPlus size={16} />
                            </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Disconnect" withArrow>
                            <ActionIcon
                                variant="outline"
                                color="red"
                                size="lg"
                                onClick={onDisconnect}
                                aria-label={`Disconnect ${name}`}
                            >
                                <MdLinkOff size={16} />
                            </ActionIcon>
                        </Tooltip>
                    </Group>
                </Group>

                {integration.targets.length === 0 ? (
                    <Text size="sm" c="dimmed" className="wrapped-text">
                        Nothing is being imported yet. Choose a tracker and pick
                        which values fill which fields.
                    </Text>
                ) : (
                    <Stack gap="xs">
                        {integration.targets.map((target) => (
                            <TargetRow
                                key={target.id}
                                target={target}
                                provider={provider}
                                syncing={
                                    syncingTargetId === target.id ||
                                    (syncingIntegration &&
                                        target.mode === "Pull")
                                }
                                onEdit={() => onEditTarget(target)}
                                onDelete={() => onDeleteTarget(target)}
                                onSyncNow={() => onSyncNow(target)}
                                onResync={() => onResync(target)}
                                onChangeSecret={() => onChangeSecret(target)}
                            />
                        ))}
                    </Stack>
                )}
            </Stack>
        </Card>
    );
}

function TargetRow({
    target,
    provider,
    syncing,
    onEdit,
    onDelete,
    onSyncNow,
    onResync,
    onChangeSecret,
}: {
    target: IntegrationTargetDto;
    provider?: ProviderDto;
    syncing: boolean;
    onEdit: () => void;
    onDelete: () => void;
    onSyncNow: () => void;
    onResync: () => void;
    onChangeSecret: () => void;
}) {
    const theme = useMantineTheme();
    const isPush = target.mode === "Push";
    // A Firefly target is created before its secret exists: the user has to make the webhook
    // in Firefly first, then paste the secret it generates. Until then nothing is received.
    const needsSecret = isPush && target.hasWebhookSecret === false;
    const secretTooltip = provider?.providerSuppliesSecret
        ? needsSecret
            ? "Add webhook secret"
            : "Update webhook secret"
        : "New webhook secret";

    return (
        <Paper withBorder radius="sm" p="sm">
            <Group justify="space-between" align="flex-start" wrap="nowrap">
                <Stack gap={4} flex={1} miw={0}>
                    <Group gap="xs" wrap="wrap">
                        <Text
                            size="sm"
                            fw={500}
                            lineClamp={1}
                            className="wrapped-text"
                        >
                            {target.trackerName}
                        </Text>
                        {!target.isEnabled && (
                            <Badge size="sm" color="gray" variant="light">
                                Paused
                            </Badge>
                        )}
                    </Group>

                    <Text
                        size="xs"
                        c="dimmed"
                        lineClamp={2}
                        className="wrapped-text"
                    >
                        {summarise(target)}
                    </Text>

                    {target.lastSyncStatus === "Error" &&
                        target.lastSyncError && (
                            <Group gap={4} wrap="nowrap" align="flex-start">
                                <MdWarning
                                    size={14}
                                    style={{
                                        marginTop: 2,
                                        flexShrink: 0,
                                        color: "var(--mantine-color-red-6)",
                                    }}
                                />
                                <Text
                                    size="xs"
                                    c="red"
                                    lineClamp={2}
                                    className="wrapped-text"
                                >
                                    {target.lastSyncError}
                                </Text>
                            </Group>
                        )}

                    {needsSecret && (
                        <Group gap={4} wrap="nowrap" align="flex-start">
                            <MdVpnKey
                                size={14}
                                style={{
                                    marginTop: 2,
                                    flexShrink: 0,
                                    color: "var(--mantine-color-yellow-6)",
                                }}
                            />
                            <Text
                                size="xs"
                                c="dimmed"
                                lineClamp={2}
                                className="wrapped-text"
                            >
                                Add the webhook secret from{" "}
                                {provider?.displayName ?? "the provider"} to
                                start receiving data.
                            </Text>
                        </Group>
                    )}
                </Stack>

                <Group
                    gap="xs"
                    wrap="nowrap"
                    align="flex-start"
                    style={{ flexShrink: 0 }}
                >
                    {/* A push target has nothing to poll, so the slot its sync button
                        would take holds the one action only it has. */}
                    {isPush ? (
                        <Tooltip label={secretTooltip} withArrow>
                            <ActionIcon
                                variant={needsSecret ? "filled" : "outline"}
                                color="yellow"
                                size="lg"
                                onClick={onChangeSecret}
                                aria-label={`${secretTooltip} for ${target.trackerName}`}
                            >
                                <MdVpnKey size={16} />
                            </ActionIcon>
                        </Tooltip>
                    ) : (
                        <>
                            <Tooltip label="Check for new data now" withArrow>
                                <ActionIcon
                                    variant="outline"
                                    color={theme.primaryColor}
                                    size="lg"
                                    loading={syncing}
                                    onClick={onSyncNow}
                                    aria-label={`Sync ${target.trackerName} now`}
                                >
                                    <FiRefreshCw size={16} />
                                </ActionIcon>
                            </Tooltip>
                            {/* A normal sync only re-reads the last few days and skips
                                what has not changed upstream, so a mapping added later
                                leaves older entries blank. This re-imports the lot. */}
                            <Tooltip label="Re-import all data" withArrow>
                                <ActionIcon
                                    variant="outline"
                                    color={theme.primaryColor}
                                    size="lg"
                                    disabled={syncing}
                                    onClick={onResync}
                                    aria-label={`Re-import all data for ${target.trackerName}`}
                                >
                                    <MdHistory size={16} />
                                </ActionIcon>
                            </Tooltip>
                        </>
                    )}
                    <Tooltip label="Edit mapping" withArrow>
                        <ActionIcon
                            variant="outline"
                            color="green"
                            size="lg"
                            onClick={onEdit}
                            aria-label={`Edit mapping for ${target.trackerName}`}
                        >
                            <MdEdit size={16} />
                        </ActionIcon>
                    </Tooltip>
                    <Tooltip label="Stop importing" withArrow>
                        <ActionIcon
                            variant="outline"
                            color="red"
                            size="lg"
                            onClick={onDelete}
                            aria-label={`Stop importing into ${target.trackerName}`}
                        >
                            <MdDelete size={16} />
                        </ActionIcon>
                    </Tooltip>
                </Group>
            </Group>
        </Paper>
    );
}

/**
 * What is imported and how it last went, as one dimmed line under the tracker name --
 * the same shape a notification's schedule and mode get. A pill is kept for state the
 * eye should catch on its own (paused, and the error message below), not for facts that
 * read perfectly well as a sentence.
 */
function summarise(target: IntegrationTargetDto) {
    const count = target.mappings.length;

    return [
        titleCase(target.resourceType),
        `${count} ${count === 1 ? "field" : "fields"} mapped`,
        status(target),
    ].join(" · ");
}

function status(target: IntegrationTargetDto) {
    const when = relative(target.lastSyncedAt);

    if (target.lastSyncStatus === "Error") {
        return when ? `failed ${when}` : "failed";
    }

    if (target.lastSyncStatus === "Never" || !when) {
        return target.mode === "Push"
            ? "waiting for first delivery"
            : "not synced yet";
    }

    return `updated ${when}`;
}

function titleCase(value: string) {
    return value.charAt(0).toUpperCase() + value.slice(1);
}

function relative(value?: string | null) {
    if (!value) return "";
    const then = dayjs(value);
    const minutes = dayjs().diff(then, "minute");

    if (minutes < 1) return "just now";
    if (minutes < 60) return `${minutes}m ago`;

    const hours = dayjs().diff(then, "hour");
    if (hours < 24) return `${hours}h ago`;

    const days = dayjs().diff(then, "day");
    return days < 30 ? `${days}d ago` : then.format("D MMM YYYY");
}
