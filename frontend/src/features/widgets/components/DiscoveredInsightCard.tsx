import { ActionIcon, Group, Loader, Text, ThemeIcon, UnstyledButton, useMantineTheme } from "@mantine/core";
import { FiX } from "react-icons/fi";
import { TbChartScatter } from "react-icons/tb";
import { CorrelationInsightDto } from "../types/CorrelationInsightDto";

interface Props {
    insight: CorrelationInsightDto;
    color: string;
    adding?: boolean;
    dismissing?: boolean;
    onAdd: () => void;
    onDismiss: () => void;
}

export function DiscoveredInsightCard({
    insight,
    color,
    adding,
    dismissing,
    onAdd,
    onDismiss,
}: Props) {
    const theme = useMantineTheme();
    const busy = !!adding || !!dismissing;

    return (
        <UnstyledButton
            onClick={onAdd}
            disabled={busy}
            px="sm"
            py="xs"
            style={{ borderRadius: theme.radius.sm, width: "100%" }}
        >
            <Group wrap="nowrap" gap="sm">
                <ThemeIcon size={34} radius="md" variant="light" color={color}>
                    <TbChartScatter size={18} />
                </ThemeIcon>
                <div style={{ flex: 1, minWidth: 0 }}>
                    <Text fw={500} lineClamp={1}>
                        {insight.valueFieldAName} vs {insight.valueFieldBName}
                    </Text>
                    <Text size="xs" c="dimmed" lineClamp={1}>
                        {insight.trackerAName} · {insight.trackerBName} · r ={" "}
                        {insight.coefficient.toFixed(2)} · {insight.sampleSize} shared days
                    </Text>
                </div>
                {adding ? (
                    <Loader size="xs" />
                ) : (
                    <ActionIcon
                        variant="subtle"
                        color="gray"
                        aria-label="Dismiss"
                        loading={dismissing}
                        disabled={busy}
                        onClick={(event) => {
                            event.stopPropagation();
                            onDismiss();
                        }}
                    >
                        <FiX size={16} />
                    </ActionIcon>
                )}
            </Group>
        </UnstyledButton>
    );
}
