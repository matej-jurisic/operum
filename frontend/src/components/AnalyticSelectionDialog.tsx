import {
    Accordion,
    Badge,
    Box,
    Button,
    Divider,
    Group,
    Modal,
    Stack,
    Text,
} from "@mantine/core";
import { useEffect, useState } from "react";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import {
    AnalyticConfigDto,
    CodeDto,
    ResultTypeDto,
} from "../model/AnalyticDto";
import { DataTypeColor, FieldType } from "../model/constants/DataTypes";
import TrackerAnalyticFormDialog from "./TrackerAnalyticFormDialog";

interface Props {
    onClose: () => void;
}

function isFieldType(value: string): value is FieldType {
    return value in DataTypeColor;
}

export default function AnalyticSelectionDialog({ onClose }: Props) {
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [selectedAnalytic, setSelectedAnalytic] = useState<{
        code: CodeDto;
        resultType: ResultTypeDto;
    }>();
    const [loading, setLoading] = useState(true);
    const { tracker } = useTracker();

    const fetchConfig = async () => {
        try {
            setLoading(true);
            const response = await api.get("/analytics");
            setConfig(response.data.data as AnalyticConfigDto);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchConfig();
    }, []);

    return (
        <>
            <Modal
                opened={!loading}
                onClose={onClose}
                title="Select an Analytic"
                centered
            >
                <Accordion variant="contained">
                    {config?.resultTypes.map((resultType) => (
                        <Accordion.Item
                            key={resultType.name}
                            value={resultType.name}
                        >
                            <Accordion.Control>
                                <Group
                                    justify="space-between"
                                    pr="sm"
                                    wrap="nowrap"
                                >
                                    <Text fw={500}>{resultType.name}</Text>
                                    <Badge
                                        color={tracker.color}
                                        variant="outline"
                                    >
                                        {resultType.codes.length}
                                    </Badge>
                                </Group>
                            </Accordion.Control>
                            <Accordion.Panel>
                                <Stack gap="xs">
                                    {resultType.codes.map((code) => (
                                        <Box key={code.name}>
                                            <Divider />
                                            <Group
                                                justify="space-between"
                                                wrap="nowrap"
                                                py="xs"
                                            >
                                                <Stack
                                                    gap="xs"
                                                    style={{
                                                        flex: 1,
                                                        minWidth: 0,
                                                    }}
                                                >
                                                    <Text size="sm" fw={500}>
                                                        {code.name}
                                                    </Text>
                                                    {code.purposes.map(
                                                        (purpose) => (
                                                            <Box
                                                                key={
                                                                    purpose.name
                                                                }
                                                            >
                                                                <Text
                                                                    size="xs"
                                                                    c="dimmed"
                                                                    mb={4}
                                                                >
                                                                    {
                                                                        purpose.name
                                                                    }
                                                                </Text>
                                                                <Group
                                                                    gap={4}
                                                                    wrap="wrap"
                                                                >
                                                                    {purpose.allowedDataTypes.map(
                                                                        (
                                                                            type
                                                                        ) => (
                                                                            <Badge
                                                                                key={
                                                                                    type
                                                                                }
                                                                                variant="outline"
                                                                                size="xs"
                                                                                color={
                                                                                    isFieldType(
                                                                                        type
                                                                                    )
                                                                                        ? DataTypeColor[
                                                                                              type
                                                                                          ]
                                                                                        : "gray"
                                                                                }
                                                                            >
                                                                                {
                                                                                    type
                                                                                }
                                                                            </Badge>
                                                                        )
                                                                    )}
                                                                </Group>
                                                            </Box>
                                                        )
                                                    )}
                                                </Stack>
                                                <Button
                                                    size="xs"
                                                    color={tracker.color}
                                                    variant="outline"
                                                    onClick={() =>
                                                        setSelectedAnalytic({
                                                            code,
                                                            resultType,
                                                        })
                                                    }
                                                >
                                                    Select
                                                </Button>
                                            </Group>
                                        </Box>
                                    ))}
                                </Stack>
                            </Accordion.Panel>
                        </Accordion.Item>
                    ))}
                </Accordion>
            </Modal>

            {selectedAnalytic && (
                <TrackerAnalyticFormDialog
                    onClose={() => setSelectedAnalytic(undefined)}
                    selectedAnalytic={selectedAnalytic}
                />
            )}
        </>
    );
}
