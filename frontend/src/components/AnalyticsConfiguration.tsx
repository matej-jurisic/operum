import { Badge, Button, Card, Group, Stack, Text, Title } from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { AnalyticDto } from "../model/AnalyticDto";
import ConfirmationDialog from "./ConfirmationDialog";
import { CreateAnalyticDialog } from "./CreateAnalyticDialog";

enum OpenDialogType {
    CreateAnalytic,
    DeleteAnalytic,
}

export default function AdminPanel() {
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
            <Stack gap="lg">
                <Group>
                    <Button
                        leftSection={<FiPlus size={18} />}
                        onClick={() =>
                            setOpenDialogType(OpenDialogType.CreateAnalytic)
                        }
                    >
                        Create Analytic
                    </Button>
                </Group>
                {analytics.map((a) => (
                    <Card key={a.id} p="md" radius="md" withBorder>
                        <Group justify="space-between" align="center">
                            <Stack gap={4} maw="90%">
                                <Title className="truncated-text" order={4}>
                                    {a.name}
                                </Title>
                                <Text className="truncated-text">
                                    {a.description}
                                </Text>
                            </Stack>
                            <Group
                                gap="xs"
                                justify="flex-end"
                                style={{ flexGrow: 1 }}
                            >
                                {a.analyticRequiredDataTypes.map((r) => (
                                    <Badge
                                        variant={
                                            r.purpose === "main"
                                                ? "light"
                                                : "outline"
                                        }
                                        key={r.id}
                                    >
                                        {r.type}
                                    </Badge>
                                ))}
                                <Button
                                    variant="outline"
                                    color="red"
                                    onClick={() => {
                                        setSelectedAnalytic(a);
                                        setOpenDialogType(
                                            OpenDialogType.DeleteAnalytic
                                        );
                                    }}
                                >
                                    <MdDelete size={18} />
                                </Button>
                            </Group>
                        </Group>
                    </Card>
                ))}
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
                    message={`Are you sure you want to delete the analytic ${selectedAnalytic?.name}?`}
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
