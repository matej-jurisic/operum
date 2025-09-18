import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Group,
    Paper,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
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

export default function AnalyticsConfiguration() {
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const [selectedAnalytic, setSelectedAnalytic] = useState<AnalyticDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    const GetAnalytics = async () => {
        const response = await api.get("/analytics");
        setAnalytics(response.data.data);
    };

    const DeleteAnalytic = async (analyticId: string) => {
        await api.delete(`/analytics/${analyticId}`);
    };

    useEffect(() => {
        GetAnalytics();
    }, []);

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

                {analytics.length > 0 ? (
                    analytics.map((analytic) => (
                        <Card key={analytic.id} p="md" radius="md" withBorder>
                            <Group
                                wrap="nowrap"
                                justify="space-between"
                                align="flex-start"
                            >
                                <Stack
                                    gap="sm"
                                    style={{ minWidth: 0, flex: 1 }}
                                >
                                    <Group gap={"xs"}>
                                        <Title
                                            order={4}
                                            lineClamp={1}
                                            className="wrapped-text"
                                        >
                                            {analytic.name}
                                        </Title>
                                        <Text c={"dimmed"}>
                                            ({analytic.code})
                                        </Text>
                                    </Group>
                                    <Text
                                        c="dimmed"
                                        size="sm"
                                        lineClamp={3}
                                        className="wrapped-text"
                                    >
                                        {analytic.description ||
                                            "No description"}
                                    </Text>
                                    <Group gap="xs" wrap="wrap">
                                        {analytic.analyticRequiredDataTypes.map(
                                            (r) => (
                                                <Badge
                                                    key={r.id}
                                                    size="sm"
                                                    color={
                                                        isFieldType(r.type)
                                                            ? DataTypeColor[
                                                                  r.type
                                                              ]
                                                            : "gray"
                                                    }
                                                    variant="outline"
                                                >
                                                    {r.purpose} ({r.type})
                                                </Badge>
                                            )
                                        )}
                                    </Group>
                                </Stack>
                                <Group gap="md" wrap="nowrap">
                                    <Badge
                                        variant="outline"
                                        color={
                                            analytic.analyticTypeId ===
                                            AnalyticType.Public
                                                ? "green"
                                                : "yellow"
                                        }
                                    >
                                        {analytic.analyticTypeName}
                                    </Badge>
                                    <ActionIcon
                                        variant="outline"
                                        color="red"
                                        size="lg"
                                        onClick={() => {
                                            setSelectedAnalytic(analytic);
                                            setOpenDialogType(
                                                OpenDialogType.DeleteAnalytic
                                            );
                                        }}
                                        aria-label={`Delete analytic ${analytic.name}`}
                                    >
                                        <MdDelete size={16} />
                                    </ActionIcon>
                                </Group>
                            </Group>
                        </Card>
                    ))
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
                    message={`Are you sure you want to delete the analytic "${selectedAnalytic?.name}"?`}
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
