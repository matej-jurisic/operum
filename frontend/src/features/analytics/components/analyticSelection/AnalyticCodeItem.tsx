import { Box, Button, Divider, Group, Stack, Text } from "@mantine/core";
import { useTracker } from "../../../trackers/context/TrackerContext";
import { CodeDto } from "../../types/AnalyticConfigDto";
import { AnalyticPurposeList } from "./AnalyticPurposeList";

interface Props {
    code: CodeDto;
    onSelect: () => void;
}

export function AnalyticCodeItem({ code, onSelect }: Props) {
    const { tracker } = useTracker();

    return (
        <Box>
            <Divider />
            <Group justify="space-between" wrap="nowrap" py="xs">
                <Stack gap="xs" style={{ flex: 1, minWidth: 0 }}>
                    <Text size="sm" fw={500}>
                        {code.name}
                    </Text>
                    <AnalyticPurposeList purposes={code.purposes} />
                </Stack>
                <Button
                    size="xs"
                    color={tracker.color}
                    variant="outline"
                    onClick={onSelect}
                >
                    Select
                </Button>
            </Group>
        </Box>
    );
}
