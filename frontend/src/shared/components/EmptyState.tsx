import { Paper, Stack, Text } from "@mantine/core";
import type { ReactNode } from "react";

interface Props {
    /** Headline, phrased as "No <things> yet". */
    title: string;
    /** One line saying what the thing is or how to make one. */
    hint?: string;
    /** Optional call to action rendered under the hint. */
    children?: ReactNode;
}

/** For in-list/tab empty results; page-level empty states (no trackers, no boards) use a richer treatment instead. */
export default function EmptyState(props: Props) {
    return (
        <Paper withBorder p="xl" radius="md">
            <Stack gap="md" align="center">
                <Text size="lg" fw={500} c="dimmed">
                    {props.title}
                </Text>
                {props.hint && (
                    <Text ta="center" c="dimmed">
                        {props.hint}
                    </Text>
                )}
                {props.children}
            </Stack>
        </Paper>
    );
}
