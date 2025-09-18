import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Collapse,
    Group,
    Paper,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiChevronDown, FiChevronRight, FiPlus } from "react-icons/fi";
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

// Group analytics by name
function groupAnalyticsByName(
    analytics: AnalyticDto[]
): Record<string, AnalyticDto[]> {
    return analytics.reduce((groups, analytic) => {
        const name = analytic.name;
        if (!groups[name]) {
            groups[name] = [];
        }
        groups[name].push(analytic);
        return groups;
    }, {} as Record<string, AnalyticDto[]>);
}

export default function AnalyticsConfiguration() {
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const [selectedAnalytic, setSelectedAnalytic] = useState<AnalyticDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
        new Set()
    );

    const GetAnalytics = async () => {
        const response = await api.get("/analytics");
        setAnalytics(response.data.data);
    };

    const DeleteAnalytic = async (analyticId: string) => {
        await api.delete(`/analytics/${analyticId}`);
    };

    const toggleGroup = (groupName: string) => {
        const newExpandedGroups = new Set(expandedGroups);
        if (newExpandedGroups.has(groupName)) {
            newExpandedGroups.delete(groupName);
        } else {
            newExpandedGroups.add(groupName);
        }
        setExpandedGroups(newExpandedGroups);
    };

    useEffect(() => {
        GetAnalytics();
    }, []);

    const groupedAnalytics = groupAnalyticsByName(analytics);

    return (
        <>
            <Stack gap="md">
                <Group justify="space-between" w="100%">
                    <Button
                        color="indigo"
                        variant="outline"
                        leftSection={<FiPlus size={18} />}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateAnalytic)
                        }
                    >
                        Create
                    </Button>
                </Group>

                {Object.keys(groupedAnalytics).length > 0 ? (
                    Object.entries(groupedAnalytics).map(
                        ([groupName, groupAnalytics]) => (
                            <Card key={groupName} radius="md" withBorder>
                                <Stack gap="sm">
                                    {/* Group Header - Clickable */}
                                    <Group
                                        wrap="nowrap"
                                        justify="space-between"
                                        style={{ cursor: "pointer" }}
                                        onClick={() => toggleGroup(groupName)}
                                    >
                                        <Group
                                            gap="xs"
                                            align="center"
                                            w={"100%"}
                                        >
                                            <ActionIcon
                                                variant="transparent"
                                                size="sm"
                                                color="gray"
                                            >
                                                {expandedGroups.has(
                                                    groupName
                                                ) ? (
                                                    <FiChevronDown size={16} />
                                                ) : (
                                                    <FiChevronRight size={16} />
                                                )}
                                            </ActionIcon>
                                            <Group
                                                flex={1}
                                                justify="space-between"
                                            >
                                                <Title order={4}>
                                                    {groupName}
                                                </Title>
                                                <Badge
                                                    variant="transparent"
                                                    size="md"
                                                >
                                                    {groupAnalytics.length}{" "}
                                                    variant
                                                    {groupAnalytics.length !== 1
                                                        ? "s"
                                                        : ""}
                                                </Badge>
                                            </Group>
                                        </Group>
                                    </Group>

                                    {/* Collapsible Content */}
                                    <Collapse
                                        in={expandedGroups.has(groupName)}
                                    >
                                        <Stack gap="sm" pl={"xl"}>
                                            {groupAnalytics.map((analytic) => (
                                                <Card
                                                    key={analytic.id}
                                                    radius="sm"
                                                    withBorder
                                                >
                                                    <Stack gap={"sm"}>
                                                        <Group
                                                            justify="space-between"
                                                            wrap="nowrap"
                                                        >
                                                            <Text
                                                                flex={1}
                                                                fw={500}
                                                                className="wrapped-text"
                                                            >
                                                                {analytic.code}
                                                            </Text>
                                                            <Badge
                                                                variant="outline"
                                                                color={
                                                                    analytic.analyticTypeId ===
                                                                    AnalyticType.Public
                                                                        ? "green"
                                                                        : "yellow"
                                                                }
                                                            >
                                                                {
                                                                    analytic.analyticTypeName
                                                                }
                                                            </Badge>
                                                        </Group>
                                                        <Text
                                                            c="dimmed"
                                                            size="sm"
                                                            lineClamp={2}
                                                            className="wrapped-text"
                                                        >
                                                            {analytic.description ||
                                                                "No description"}
                                                        </Text>
                                                        <Group
                                                            justify="space-between"
                                                            w={"100%"}
                                                        >
                                                            <Stack gap="xs">
                                                                {analytic.analyticRequiredDataTypes.map(
                                                                    (r) => (
                                                                        <Badge
                                                                            key={
                                                                                r.id
                                                                            }
                                                                            color={
                                                                                isFieldType(
                                                                                    r.type
                                                                                )
                                                                                    ? DataTypeColor[
                                                                                          r
                                                                                              .type
                                                                                      ]
                                                                                    : "gray"
                                                                            }
                                                                            variant="outline"
                                                                        >
                                                                            {
                                                                                r.purpose
                                                                            }{" "}
                                                                            (
                                                                            {
                                                                                r.type
                                                                            }
                                                                            )
                                                                        </Badge>
                                                                    )
                                                                )}
                                                            </Stack>
                                                            <Group
                                                                gap="sm"
                                                                wrap="nowrap"
                                                                justify="flex-end"
                                                                flex={1}
                                                            >
                                                                <ActionIcon
                                                                    variant="outline"
                                                                    color="red"
                                                                    size={"lg"}
                                                                    onClick={(
                                                                        e
                                                                    ) => {
                                                                        e.stopPropagation(); // Prevent group toggle
                                                                        setSelectedAnalytic(
                                                                            analytic
                                                                        );
                                                                        setOpenDialogType(
                                                                            OpenDialogType.DeleteAnalytic
                                                                        );
                                                                    }}
                                                                    aria-label={`Delete analytic ${analytic.name} - ${analytic.code}`}
                                                                >
                                                                    <MdDelete
                                                                        size={
                                                                            16
                                                                        }
                                                                    />
                                                                </ActionIcon>
                                                            </Group>
                                                        </Group>
                                                    </Stack>
                                                </Card>
                                            ))}
                                        </Stack>
                                    </Collapse>
                                </Stack>
                            </Card>
                        )
                    )
                ) : (
                    <Paper withBorder p="xl" radius="md">
                        <Stack gap="md" align="center">
                            <Text size="lg" fw={500} c="dimmed">
                                No Analytics Available
                            </Text>
                            <Text ta="center" c="dimmed">
                                Analytics will appear here when you create them.
                            </Text>
                        </Stack>
                    </Paper>
                )}
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
