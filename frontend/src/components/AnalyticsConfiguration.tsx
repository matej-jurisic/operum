import {
    Accordion,
    ActionIcon,
    Badge,
    Button,
    Card,
    Divider,
    Group,
    Paper,
    ScrollArea,
    Stack,
    Text,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useEffect, useMemo, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { AnalyticDto } from "../model/AnalyticDto";
import { DataTypeColor, FieldType } from "../model/constants/DataTypes";
import { AnalyticType } from "../model/enums/AnalyticTypeEnum";
import ConfirmationDialog from "./ConfirmationDialog";
import { CreateAnalyticDialog } from "./CreateAnalyticDialog";

enum OpenDialogType {
    CreateAnalytic,
    DeleteAnalytic,
}

function isFieldType(value: string): value is FieldType {
    return value in DataTypeColor;
}

interface AnalyticCardProps {
    analytic: AnalyticDto;
    onDelete: (analytic: AnalyticDto) => void;
}

function AnalyticCard({ analytic, onDelete }: AnalyticCardProps) {
    return (
        <>
            <Divider />
            <Card radius="sm" p="md">
                <Group align="flex-start" wrap="nowrap" gap="md">
                    {/* Left section: Main content */}
                    <Stack gap="sm" style={{ flex: 1, minWidth: 0 }}>
                        <Group gap="xs" wrap="wrap">
                            <Text fw={500} className="wrapped-text">
                                {analytic.code}
                            </Text>
                            <Badge variant="outline" color="blue" size="sm">
                                {analytic.resultType}
                            </Badge>
                            <Badge
                                variant="outline"
                                size="sm"
                                color={
                                    analytic.analyticTypeId ===
                                    AnalyticType.Public
                                        ? "green"
                                        : "yellow"
                                }
                            >
                                {analytic.analyticTypeName}
                            </Badge>
                        </Group>

                        <Text
                            c="dimmed"
                            size="sm"
                            lineClamp={2}
                            className="wrapped-text"
                        >
                            {analytic.description || "No description"}
                        </Text>

                        <Group gap="xs" wrap="wrap">
                            {analytic.analyticRequiredDataTypes.map((r) => (
                                <Badge
                                    key={r.id}
                                    color={
                                        isFieldType(r.type)
                                            ? DataTypeColor[r.type]
                                            : "gray"
                                    }
                                    variant="outline"
                                    size="sm"
                                >
                                    {r.purpose} ({r.type})
                                </Badge>
                            ))}
                        </Group>
                    </Stack>

                    {/* Right section: Actions */}
                    <ActionIcon
                        variant="outline"
                        color="red"
                        size="lg"
                        onClick={(e) => {
                            e.stopPropagation();
                            onDelete(analytic);
                        }}
                        aria-label={`Delete analytic ${analytic.name} - ${analytic.code}`}
                    >
                        <MdDelete size={16} />
                    </ActionIcon>
                </Group>
            </Card>
        </>
    );
}

export default function AnalyticsConfiguration() {
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const [selectedAnalytic, setSelectedAnalytic] = useState<AnalyticDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    const GetAnalytics = async () => {
        const response = await api.get("/analytics/admin-analytics");
        setAnalytics(response.data.data);
    };

    const DeleteAnalytic = async (analyticId: string) => {
        await api.delete(`/analytics/${analyticId}`);
    };

    const handleDeleteClick = (analytic: AnalyticDto) => {
        setSelectedAnalytic(analytic);
        setOpenDialogType(OpenDialogType.DeleteAnalytic);
    };

    useEffect(() => {
        GetAnalytics();
    }, []);

    const groupedAnalytics = useMemo(() => {
        return analytics.reduce((acc, analytic) => {
            if (!acc[analytic.name]) {
                acc[analytic.name] = [];
            }
            acc[analytic.name].push(analytic);
            return acc;
        }, {} as Record<string, AnalyticDto[]>);
    }, [analytics]);

    const theme = useMantineTheme();

    return (
        <>
            <Stack gap="md" h={"100%"}>
                <Group justify="space-between" w="100%">
                    <Button
                        color={theme.primaryColor}
                        variant="outline"
                        leftSection={<FiPlus size={18} />}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateAnalytic)
                        }
                    >
                        Create
                    </Button>
                </Group>

                <ScrollArea flex={1}>
                    <Stack>
                        {Object.keys(groupedAnalytics).length > 0 ? (
                            <Accordion
                                variant="contained"
                                chevronPosition="left"
                            >
                                {Object.entries(groupedAnalytics).map(
                                    ([groupName, groupAnalytics]) => (
                                        <Accordion.Item
                                            key={groupName}
                                            value={groupName}
                                        >
                                            <Accordion.Control>
                                                <Group justify="space-between">
                                                    <Title order={4}>
                                                        {groupName}
                                                    </Title>
                                                    <Badge
                                                        variant="light"
                                                        size="md"
                                                    >
                                                        {groupAnalytics.length}{" "}
                                                        variant
                                                        {groupAnalytics.length !==
                                                        1
                                                            ? "s"
                                                            : ""}
                                                    </Badge>
                                                </Group>
                                            </Accordion.Control>
                                            <Accordion.Panel>
                                                <Stack gap="md">
                                                    {groupAnalytics.map(
                                                        (analytic) => (
                                                            <AnalyticCard
                                                                key={
                                                                    analytic.id
                                                                }
                                                                analytic={
                                                                    analytic
                                                                }
                                                                onDelete={
                                                                    handleDeleteClick
                                                                }
                                                            />
                                                        )
                                                    )}
                                                </Stack>
                                            </Accordion.Panel>
                                        </Accordion.Item>
                                    )
                                )}
                            </Accordion>
                        ) : (
                            <Paper withBorder p="xl" radius="md">
                                <Stack gap="md" align="center">
                                    <Text size="lg" fw={500} c="dimmed">
                                        No Analytics Available
                                    </Text>
                                    <Text ta="center" c="dimmed">
                                        Analytics will appear here when you
                                        create them.
                                    </Text>
                                </Stack>
                            </Paper>
                        )}
                    </Stack>
                </ScrollArea>
            </Stack>

            {openDialogType === OpenDialogType.CreateAnalytic && (
                <CreateAnalyticDialog
                    onFieldAdded={() => {
                        GetAnalytics();
                        setOpenDialogType(undefined);
                    }}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
            {openDialogType === OpenDialogType.DeleteAnalytic && (
                <ConfirmationDialog
                    isOpen
                    title="Delete Analytic"
                    severity="warning"
                    message={`Are you sure you want to delete the analytic "${selectedAnalytic?.name}" with code "${selectedAnalytic?.code}"?`}
                    onConfirm={async () => {
                        if (selectedAnalytic) {
                            await DeleteAnalytic(selectedAnalytic.id);
                            GetAnalytics();
                        }
                        setOpenDialogType(undefined);
                    }}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
        </>
    );
}
