import { Accordion, Badge, Group, Stack, Text } from "@mantine/core";
import { useTracker } from "../../../trackers/context/TrackerContext";
import { CodeDto, ResultTypeDto } from "../../types/AnalyticConfigDto";
import { AnalyticCodeItem } from "./AnalyticCodeItem";

interface Props {
    resultType: ResultTypeDto;
    onSelectAnalytic: (code: CodeDto) => void;
}

export function AnalyticResultTypeItem({
    resultType,
    onSelectAnalytic,
}: Props) {
    const { tracker } = useTracker();

    return (
        <Accordion.Item value={resultType.name}>
            <Accordion.Control>
                <Group justify="space-between" pr="sm" wrap="nowrap">
                    <Text fw={500}>{resultType.name}</Text>
                    <Badge color={tracker.color} variant="outline">
                        {resultType.codes.length}
                    </Badge>
                </Group>
            </Accordion.Control>
            <Accordion.Panel>
                <Stack gap="xs">
                    {resultType.codes.map((code) => (
                        <AnalyticCodeItem
                            key={code.name}
                            code={code}
                            onSelect={() => onSelectAnalytic(code)}
                        />
                    ))}
                </Stack>
            </Accordion.Panel>
        </Accordion.Item>
    );
}
