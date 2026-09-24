import { Badge, Card, Group, Stack, Text, ThemeIcon, Title } from "@mantine/core";
import { createElement } from "react";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { formatRelativeTime } from "../../../shared/utils/formatters/TypeFormatter";
import { AdminTrackerDto } from "../types/AdminTrackerDto";

interface Props {
    tracker: AdminTrackerDto;
}

const count = (value: number, one: string, many: string) =>
    `${value} ${value === 1 ? one : many}`;

/**
 * One tracker in the admin list, drawn the way every other tracker card in the app is:
 * the colour accent on top, the tinted icon, then a dimmed meta line. It carries no
 * actions, the list being read-only, and spends that room on the owner instead.
 */
export default function AdminTrackerCard({ tracker }: Props) {
    const color = tracker.color ?? "indigo";

    return (
        <Card
            withBorder
            shadow="sm"
            padding="lg"
            radius="md"
            style={{ borderTop: `3px solid var(--mantine-color-${color}-5)` }}
        >
            <Group gap="md" align="center" wrap="nowrap">
                <ThemeIcon
                    size={44}
                    radius="md"
                    variant="light"
                    color={color}
                    style={{ flexShrink: 0 }}
                >
                    {createElement(resolveTrackerIcon(tracker.icon), {
                        size: 22,
                    })}
                </ThemeIcon>
                <Stack gap={4} flex={1} style={{ minWidth: 0 }}>
                    <Title
                        order={4}
                        lineClamp={1}
                        className="wrapped-text"
                        style={{ minWidth: 0 }}
                    >
                        {tracker.name}
                    </Title>
                    <Text
                        c="dimmed"
                        size="sm"
                        className="wrapped-text"
                        lineClamp={2}
                    >
                        {tracker.description || "No description"}
                    </Text>
                    <Text c="dimmed" size="xs">
                        {count(tracker.fieldCount, "field", "fields")} ·{" "}
                        {count(tracker.entryCount, "entry", "entries")} ·{" "}
                        {count(
                            tracker.collaboratorCount,
                            "collaborator",
                            "collaborators",
                        )}
                        {tracker.lastEntryAt &&
                            ` · updated ${formatRelativeTime(
                                tracker.lastEntryAt,
                            )}`}
                    </Text>
                    <Badge
                        variant="outline"
                        maw="100%"
                        style={{ alignSelf: "flex-start" }}
                    >
                        Owned by: {tracker.ownerName}
                    </Badge>
                </Stack>
            </Group>
        </Card>
    );
}
